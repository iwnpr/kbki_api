using Asp.Versioning;
using Cache_lib.Interfaces;
using CertManagement.Services.Interfaces;
using Confluent.Kafka;
//using Confluent.Kafka;
using Crypto_lib.Service;
using KafkaService_lib.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;
using QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;
using QBCH_api.QBCHProcessing.V3.StoreProcessingData.Event;
using QBCH_api.Services.Interfaces.V3;
using qbch_lib;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces.V3;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИно = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using АбонентИП = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентЮЛ = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;

namespace QBCH_api.Controllers;

[ApiVersion("3")]
[Route("v{version:apiVersion}")]
[ApiController]
public class QBCHIIIController(IMediator mediator,
        ICryptoService cryptoService,
        ILogger<QBCHIIIController> logger,
        IXmlServiceV3 xmlServiceV3,
        IKeyValueStorageService storageService,
        IValidationServiceV3 validationServiceV3,
        ITicketServiceV3 ticketServiceV3,
        IDlPutServiceV3 dlPutServiceV3,
        ICertManagementService certManagement,
        IKafkaService kafka,
        ApiV3ContractRules contractRules,
        IConfiguration config) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ILogger<QBCHIIIController> _logger = logger;
    private readonly IXmlServiceV3 _xmlServiceV3 = xmlServiceV3;
    private readonly IKeyValueStorageService _storageService = storageService;
    private readonly IValidationServiceV3 _validationServiceV3 = validationServiceV3;
    private readonly ITicketServiceV3 _ticketServiceV3 = ticketServiceV3;
    private readonly IDlPutServiceV3 _dlPutServiceV3 = dlPutServiceV3;
    private readonly ICertManagementService _certManagement = certManagement;
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly IConfiguration _config = config;
    private readonly IKafkaService _kafka = kafka;

    private readonly string? _kafkaTopic = config.GetValue<string>("KafkaService:Topic");
    private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");
    private readonly string? _ourBureauName = config.GetValue<string>("Bureau:Name");

    private const string DlPutAnswerV3ReadyField = "putanswer_v3_response_xml";
    /// <summary>
    /// Флаг Redis о наличии сохранённого ответа для /dlputanswer версии 3.
    /// </summary>
    private const string DlPutAnswerV3ExistsField = "putanswer_v3_exists";
    private const string ReadyAtUtcField = "ready_at_utc";
    private const string ReadyAtMskField = "ready_at_msk";
    private const string FirstPollAllowedAtUtcField = "first_poll_allowed_at_utc";
    private const string ResponseExpireAtUtcField = "response_expire_at_utc";
    private const string LastPollUtcField = "last_poll_utc";

    [HttpPost("dlrequest")]
    public async Task<IActionResult> DlRequest_v_3(ApiVersion apiVersion)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogDebug("Начало = {Action} v{Version} в {RequestTime}", nameof(DlRequest_v_3), apiVersion, requestTime);

        var transaction = await _mediator.Send(new CreateToValidateCommandV3(apiVersion, Request));
        _logger.LogDebug("{guid} Запрос: {dt}", transaction.Id, requestTime);

        if (transaction.ProcessingErrors.Count != 0)
        {
            var errorTicket = _ticketServiceV3.CreateResultV3Error(transaction.ProcessingErrors.First());
            var errorResult = _xmlServiceV3.SerializeAsByteV3(errorTicket);
            var signedResp = _cryptoService.SignMsg(errorResult);

            transaction.Complete(errorResult, signedResp);

            await _mediator.Publish(new QBCHProcessingCompleteV3(transaction));

            LogActionEnd(nameof(DlRequest_v_3), transaction.Id, StatusCodes.Status400BadRequest, actionStopwatch.Elapsed);

            return BadRequest(new MemoryStream(transaction.Response.SignedTicket!));
        }

        // Негарантированная запись уникального requestId в Redis
        try
        {
            //Достаём из transaction распарсенный запрос 'ЗапросСведений'.
            var request = transaction.GetRequest<ЗапросСведений>();

            var requestId = request?.ИдентификаторЗапроса;
            var requestDate = request?.ДатаЗапроса;
            var requestOgrn = request?.Абонент?.Item switch
            {
                АбонентЮЛ юрЛицо => юрЛицо.ОГРН,
                АбонентИП ип => ип.ОГРНИП,
                АбонентИно ино => ино.РегНомер,
                _ => null
            };

            // Если все значения валидные — пишет их в Redis
            if (!string.IsNullOrWhiteSpace(requestId) && !string.IsNullOrWhiteSpace(requestOgrn) && requestDate.HasValue)
                await _storageService.AddUniqueRequestId(RedisConstants.DlRequestV3Scope, requestId, requestOgrn, requestDate.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка записи уникального requestId в redis {guid}", transaction.Id);
        }

        transaction.TimeElapsedForValidation.Stop();
        _logger.LogDebug("{guid} Конец валидации: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

        // Основной processing и формирование HTTP-ответа
        try
        {
            // Запускает основной pipeline обработки
            var processingResult = await _mediator.Send(new QBCHProcessedStartV3(transaction, _contractRules.ImmediateResponseDeadlineMs, _ourBureauPSRN!));

            await _mediator.Publish(new QBCHProcessingCompleteV3(processingResult));

            //NOTE: Изменил логику на артемовскую и экранировал логирование
            if (processingResult.Status == QBCHProcessingStatus.Accepted)
            {
                LogActionEnd(nameof(DlRequest_v_3), transaction.Id, StatusCodes.Status202Accepted, actionStopwatch.Elapsed);
                return Accepted(new MemoryStream(processingResult.Response.SignedTicket!));
            }

            //NOTE: Сделал дополнительную обработку
            if (processingResult.Status == QBCHProcessingStatus.Failure)
            {
                _logger.LogError("Обработка вернула статус Failure, что было не предусмотрено системой.", nameof(DlRequest_v_3), apiVersion, requestTime);
            }

            Response.StatusCode = StatusCodes.Status200OK;
            LogActionEnd(nameof(DlRequest_v_3), transaction.Id, StatusCodes.Status200OK, actionStopwatch.Elapsed);
            return File(processingResult.Response.SignedResponse!, "application/octet-stream");
        }
        catch (Exception ex)
        {
            //NOTE: Вернул запись в Kafka
            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    ServiceName = transaction.ServiceName,
                    transaction.Id,
                    IpAddress = transaction.ClentRequest.IpAddress?.ToString(),
                    transaction.ClentRequest.Certificate?.Thumbprint,
                    transaction.RequestTime,
                    ResponseTime = transaction.ResponseTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                    ex.Message
                });

                // Попытка отправки в кафку                    
                if (!await _kafka.Produce(new Message<Null, string> { Value = message }, _kafkaTopic))
                    _logger.LogCritical("Потерян ключ:{key}", transaction.Id);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Критическая ошибка записи сообщения в kafka");
            }

            _logger.LogError(ex, "Ошибка формирования HTTP-ответа для идентификатора {Id}", transaction.Id);
            LogActionEnd(nameof(DlRequest_v_3), transaction.Id, StatusCodes.Status500InternalServerError, actionStopwatch.Elapsed);
            return StatusCode(500);
        }
    }

    [HttpGet("dlanswer")]
    public async Task<IActionResult> DlAnswer_v_3(string? id = null)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = RedisConstants.DlAnswerV3Scope;
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        AnswerErrorCode standartError;

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        _logger.LogDebug("Начало действия {Action} service={QbchService} transactionId={id} guid={Guid} в {RequestTime}", nameof(DlAnswer_v_3), serviceName, id, guid, requestTime);

        try
        {
            await _storageService.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _storageService.AddHash(serviceName, guid, "temp_guid", guid);
            await _storageService.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _storageService.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _storageService.AddHash(serviceName, guid, "ip_address", ipAddress);

            try
            {
                if (!string.Equals(Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
                {
                    standartError = AnswerErrorCode.Code1_WrongRequestMethod();
                    var methodError = await BuildV3ErrorResponseAsync(serviceName, guid, standartError.Code, standartError.Message, StatusCodes.Status400BadRequest);

                    return methodError.ActionResult;
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    standartError = AnswerErrorCode.Code3_EmptyRequiredParameters(nameof(id));
                    var statusCode = ResolveDlAnswerStatusCodeByErrorCode(standartError.Code);
                    var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, standartError.Code, standartError.Message, statusCode);

                    return errorResult.ActionResult;
                }

                await _storageService.AddHash(serviceName, guid, "response_guid", id);

                if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, RedisConstants.DlRequestV3Scope))
                {
                    standartError = AnswerErrorCode.Code22_AccessDenied();

                    var statusCode = ResolveDlAnswerStatusCodeByErrorCode(standartError.Code);
                    var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, standartError.Code, standartError.Message, statusCode);

                    return errorResult.ActionResult;
                }

                var isCertificateValid = _validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult);

                if (!isCertificateValid & certValidationResult is not null)
                {

                    var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(certValidationResult.ErrorCode, certValidationResult.Error));
                    responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                    signedResponse = _cryptoService.SignMsg(responseXml);

                    await _storageService.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                    await _storageService.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");

                    return BadRequest(new MemoryStream(signedResponse));
                }

                if (!await _storageService.KeyExists([RedisConstants.DlRequestV3Scope, id]))
                {
                    var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 16, "Указан некорректный идентификатор ответа", StatusCodes.Status400BadRequest);

                    return errorResult.ActionResult;
                }

                await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (_storageService.TryGetHash(RedisConstants.DlRequestV3Scope, id, "qbch_tasks_aggregate_xml", out responseXml))
                {
                    signedResponse = _cryptoService.SignMsg(responseXml);
                    return File(signedResponse, "application/octet-stream");
                }

                var error = AnswerErrorCode.Code12_ResponseIsIncomplete();
                var notReadyResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlAnswerStatusCodeByErrorCode(error.Code));

                responseXml = notReadyResult.ResponseXml;
                signedResponse = notReadyResult.SignedResponse;
                return notReadyResult.ActionResult;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка в /dlanswer v3");
                await _storageService.AddHash(serviceName, guid, "error_code", "500");
                await _storageService.AddHash(serviceName, guid, "error_message", ex.ToString());
                return StatusCode(500);
            }
            finally
            {
                if (signedResponse is not null)
                    await _storageService.AddHash(serviceName, guid, "response_signed_data", signedResponse);

                if (responseXml is not null)
                    await _storageService.AddHash(serviceName, guid, "response_xml", responseXml);

                if (!await _storageService.HashFieldExists(serviceName, guid, "validation_date_time"))
                    await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                await _storageService.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                LogActionEnd(nameof(DlAnswer_v_3), id, Response.StatusCode, actionStopwatch.Elapsed, guid);

                //NOTE: Вернул запись в Кафку
                await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" }, _kafkaTopic);
            }
        }
        catch (Exception ex)
        {
            var message = JsonSerializer.Serialize(
                   new
                   {
                       ServiceName = serviceName,
                       guid,
                       Id = id,
                       IpAddress = ipAddress?.ToString(),
                       certificate?.Thumbprint,
                       requestTime,
                       ResponseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                       ex.Message
                   });

            // Выгрузка в кафку
            await _kafka.Produce(new Message<Null, string> { Value = message }, _kafkaTopic);

            _logger.LogCritical(ex, "Возникла критическая ошибка");
            return StatusCode(500);
        }
    }

    [HttpPost("dlput")]
    public async Task<IActionResult> DlPut_v_3(ApiVersion apiVersion)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = RedisConstants.DlPutV3Scope;
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Начало действия {Action} service={ServiceName} guid={Guid} в {RequestTime}", nameof(DlPut_v_3), serviceName, guid, requestTime);
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            await _storageService.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _storageService.AddHash(serviceName, guid, "temp_guid", guid);
            await _storageService.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _storageService.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _storageService.AddHash(serviceName, guid, "ip_address", ipAddress);

            if (!string.Equals(Request.Method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            {
                var error = AnswerErrorCode.Code1_WrongRequestMethod();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            using var bodyStream = new MemoryStream();

            await Request.Body.CopyToAsync(bodyStream);
            var bodyBytes = bodyStream.ToArray();

            if (bodyBytes.Length == 0)
            {
                var error = AnswerErrorCode.Code2_EmptyRequestBody();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var isCertificateValid = _validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult);

            if (!isCertificateValid & certValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(certValidationResult.ErrorCode, certValidationResult.Error));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _storageService.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            var IsEncodingValid = _validationServiceV3.ValidateEncodingV3(bodyBytes, out var encodingValidationResult);

            if (!IsEncodingValid & encodingValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(encodingValidationResult.ErrorCode, encodingValidationResult.Error));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", encodingValidationResult?.ErrorCode.ToString() ?? "8");
                await _storageService.AddHash(serviceName, guid, "error_message", encodingValidationResult?.Error ?? "Неподдерживаемая кодировка, файл не в кодировке Utf-8");

                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                var error = AnswerErrorCode.Code22_AccessDenied();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            using var validationStream = new MemoryStream(bodyBytes);

            var isXmlValid = _validationServiceV3.ValidateXmlV3(validationStream, serviceName, out var xsdValidationResult);

            if (!isXmlValid && xsdValidationResult is not null)
            {
                var error = new AnswerErrorCode(xsdValidationResult.ErrorCode, xsdValidationResult.Error);

                var ticket = _ticketServiceV3.CreateResultV3Error(error);
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", error.Code.ToString());
                await _storageService.AddHash(serviceName, guid, "error_message", error.Message);

                return BadRequest(new MemoryStream(signedResponse));
            }

            var requestV3 = _xmlServiceV3.DeserializeV3<ПредставлениеСведений>(bodyBytes);

            if (requestV3 is null)
            {
                _logger.LogError("Не удалось десериализовать тело запроса dlput v3");

                var error = AnswerErrorCode.Code99_OtherError("Не удалось прочитать XML запроса");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            if (requestV3.БКИ is null || !string.Equals(requestV3.БКИ.ОГРН, _ourBureauPSRN, StringComparison.Ordinal))
            {
                var error = AnswerErrorCode.Code99_OtherError("ОГРН БКИ в запросе не соответствует ОГРН принимающего бюро");
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            if (!string.IsNullOrWhiteSpace(_ourBureauName) && !string.Equals(requestV3.БКИ.Value, _ourBureauName, StringComparison.OrdinalIgnoreCase))
            {
                var error = AnswerErrorCode.Code99_OtherError("Наименование БКИ в запросе не соответствует наименованию принимающего бюро");
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            var entitiesCount = requestV3.Сведения?.Length ?? 0;

            if (entitiesCount > _contractRules.MaxDlPutEntities)
            {
                var error = AnswerErrorCode.Code99_OtherError($"Превышен лимит количества элементов Сведения: {entitiesCount}");
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, $"Количество элементов Сведения превышает допустимый лимит {_contractRules.MaxDlPutEntities}", StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var isDateValid = _validationServiceV3.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult);

            if (!isDateValid & dateValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(dateValidationResult.ErrorCode, dateValidationResult.Error));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _storageService.AddHash(serviceName, guid, "error_code", dateValidationResult?.ErrorCode.ToString() ?? "23");
                await _storageService.AddHash(serviceName, guid, "error_message", dateValidationResult?.Error ?? "Дата запроса указана некорректно");

                return BadRequest(new MemoryStream(signedResponse));
            }

            var requestId = requestV3.ИдентификаторЗапроса;
            var requestOgrn = requestV3.БКИ?.ОГРН ?? string.Empty;

            var (isUniqueId, uniqueValidationResult) = await _validationServiceV3.IsUniqueRequestIdV3Async(requestId, serviceName, requestOgrn);

            if (!isUniqueId & uniqueValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(uniqueValidationResult.ErrorCode, uniqueValidationResult.ErrorMessage));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _storageService.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");

                return BadRequest(new MemoryStream(signedResponse));
            }

            var dlPutResult = await _dlPutServiceV3.ProcessAsync(requestV3);

            if (dlPutResult.IsAccepted)
            {
                await _storageService.AddUniqueRequestId(serviceName, requestId, requestOgrn, requestV3.ДатаЗапроса);

                if (dlPutResult.AcceptedTicket?.Item is QBCH.Lib.qcb_xml.v3_0.РезультатИдентификаторОтвета acceptedResponseId &&
                    !string.IsNullOrWhiteSpace(acceptedResponseId.ИдентификаторОтвета))
                {
                    var acceptedCreatedAtUtc = DateTimeOffset.UtcNow;
                    var readyTimeMs = acceptedResponseId.ВремяГотовностиSpecified
                        ? acceptedResponseId.ВремяГотовности
                        : _contractRules.MinAnswerPollingIntervalSeconds * 1000L;

                    var readyAtUtc = acceptedCreatedAtUtc.AddMilliseconds(Math.Max(1, readyTimeMs));
                    var firstPollAllowedAtUtc = acceptedCreatedAtUtc.AddSeconds(_contractRules.MinAnswerPollingIntervalSeconds);
                    var responseExpireAtUtc = acceptedCreatedAtUtc.AddHours(_contractRules.ResponseRetentionHours);

                    acceptedResponseId.ВремяГотовности = Math.Max(1, (long)(readyAtUtc - acceptedCreatedAtUtc).TotalMilliseconds);
                    acceptedResponseId.ВремяГотовностиSpecified = true;

                    responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.AcceptedTicket);
                    signedResponse = _cryptoService.SignMsg(responseXml);

                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, DlPutAnswerV3ExistsField, "1");
                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ReadyAtUtcField, readyAtUtc.ToString("O"));
                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ReadyAtMskField, readyAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("O"));
                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, FirstPollAllowedAtUtcField, firstPollAllowedAtUtc.ToString("O"));
                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ResponseExpireAtUtcField, responseExpireAtUtc.ToString("O"));
                    await _storageService.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, "response_guid", acceptedResponseId.ИдентификаторОтвета);
                    await _storageService.AddHash(serviceName, guid, "response_guid", acceptedResponseId.ИдентификаторОтвета);
                }
                else
                {
                    responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.AcceptedTicket);
                    signedResponse = _cryptoService.SignMsg(responseXml);
                }

                return Accepted(new MemoryStream(signedResponse));
            }

            responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.ReadyResult);
            signedResponse = _cryptoService.SignMsg(responseXml);

            await _storageService.AddUniqueRequestId(serviceName, requestId, requestOgrn, requestV3.ДатаЗапроса);
            await _storageService.AddHash(serviceName, dlPutResult.ReadyResult!.ИдентификаторОтвета, DlPutAnswerV3ReadyField, responseXml);
            await _storageService.AddHash(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, DlPutAnswerV3ExistsField, "1");
            await _storageService.AddHash(serviceName, guid, "response_guid", dlPutResult.ReadyResult.ИдентификаторОтвета);

            return File(signedResponse, "application/octet-stream");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Количество элементов Сведения", StringComparison.Ordinal))
        {
            _logger.LogError(ex, "Ошибка бизнес-валидации dlput v3");

            var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, $"Количество элементов Сведения превышает допустимый лимит {_contractRules.MaxDlPutEntities}", StatusCodes.Status400BadRequest);

            responseXml = errorResult.ResponseXml;
            signedResponse = errorResult.SignedResponse;
            return errorResult.ActionResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /dlput v3");
            await _storageService.AddHash(serviceName, guid, "error_code", "500");
            await _storageService.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _storageService.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _storageService.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _storageService.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _storageService.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            LogActionEnd(nameof(DlPut_v_3), guid, Response.StatusCode, actionStopwatch.Elapsed);
        }
    }

    [HttpGet("dlputanswer")]
    public async Task<IActionResult> DlPutAnswer_v_3(ApiVersion version, string? id = null)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = RedisConstants.DlPutAnswerV3Scope;
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Начало действия {Action} service={ServiceName} guid={Guid} в {RequestTime}", nameof(DlPutAnswer_v_3), serviceName, guid, requestTime);
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        AnswerErrorCode error = null;

        try
        {
            await _storageService.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _storageService.AddHash(serviceName, guid, "temp_guid", guid);
            await _storageService.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _storageService.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _storageService.AddHash(serviceName, guid, "ip_address", ipAddress);

            if (!string.Equals(Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
            {
                error = AnswerErrorCode.Code1_WrongRequestMethod();
                _logger.LogError(error.Message);
                var methodError = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = methodError.ResponseXml;
                signedResponse = methodError.SignedResponse;

                return methodError.ActionResult;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                error = AnswerErrorCode.Code3_EmptyRequiredParameters(nameof(id));
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlPutAnswerStatusCodeByErrorCode(error.Code));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            await _storageService.AddHash(serviceName, guid, "response_guid", id);

            var isCertificateValid = _validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult);

            if (!isCertificateValid & certValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(certValidationResult.ErrorCode, certValidationResult.ErrorMessage));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _storageService.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _storageService.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, RedisConstants.DlPutV3Scope))
            {
                error = AnswerErrorCode.Code22_AccessDenied();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlPutAnswerStatusCodeByErrorCode(22));
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            if (!await _storageService.KeyExists([RedisConstants.DlPutV3Scope, id]))
            {
                error = AnswerErrorCode.Code16_InvalidRequestId();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlPutAnswerStatusCodeByErrorCode(16));
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            if (!await _storageService.HashFieldExists(RedisConstants.DlPutV3Scope, id, DlPutAnswerV3ExistsField))
            {
                error = AnswerErrorCode.Code16_InvalidRequestId();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlPutAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var firstPollAllowedAtUtc = await GetFirstPollAllowedAtUtcAsync(RedisConstants.DlPutV3Scope, id);

            if (firstPollAllowedAtUtc.HasValue && nowUtc < firstPollAllowedAtUtc.Value)
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Первый опрос разрешён с {firstPollAllowedAtUtc}, текущее UTC={nowUtc}.", id, firstPollAllowedAtUtc.Value, nowUtc);
                await _storageService.AddHash(RedisConstants.DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _storageService.AddHash(RedisConstants.DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            if (_storageService.TryGetHashValue(RedisConstants.DlPutV3Scope, id, LastPollUtcField, out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Последний опрос={lastPollUtc}, текущий UTC={nowUtc}, min={interval} сек.", id, lastPollUtc, nowUtc, _contractRules.MinAnswerPollingIntervalSeconds);
                await _storageService.AddHash(RedisConstants.DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _storageService.AddHash(RedisConstants.DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            await _storageService.AddHash(RedisConstants.DlPutV3Scope, id, LastPollUtcField, nowUtc.ToString("O"));

            if (_storageService.TryGetHash(RedisConstants.DlPutV3Scope, id, DlPutAnswerV3ReadyField, out responseXml))
            {
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            error = AnswerErrorCode.Code12_ResponseIsIncomplete();
            var notReadyResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, ResolveDlPutAnswerStatusCodeByErrorCode(12));

            responseXml = notReadyResult.ResponseXml;
            signedResponse = notReadyResult.SignedResponse;

            return notReadyResult.ActionResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /dlputanswer v3");
            await _storageService.AddHash(serviceName, guid, "error_code", "500");
            await _storageService.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _storageService.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _storageService.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _storageService.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _storageService.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            LogActionEnd(nameof(DlPutAnswer_v_3), guid, Response.StatusCode, actionStopwatch.Elapsed);
        }
    }

    [HttpPost("certadd")]
    public async Task<IActionResult> CertAdd_v_3([FromForm] CertForm form)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = "certadd";
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Начало действия {Action} service={ServiceName} guid={Guid} в {RequestTime}", nameof(CertAdd_v_3), serviceName, guid, requestTime);
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        AnswerErrorCode error = null;

        await _storageService.AddHash(serviceName, guid, "request_date_time", requestTime);
        await _storageService.AddHash(serviceName, guid, "temp_guid", guid);
        await _storageService.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
        await _storageService.AddHash(serviceName, guid, "request_certificate_data", certificate?.RawData ?? Array.Empty<byte>());

        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _storageService.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            if (!string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                error = AnswerErrorCode.Code1_WrongRequestMethod();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                error = AnswerErrorCode.Code22_AccessDenied();
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var isCertificateValid = _validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult);

            if (!isCertificateValid & certValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(certValidationResult.ErrorCode, certValidationResult.ErrorMessage));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _storageService.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _storageService.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            var missingParameter = string.IsNullOrWhiteSpace(form.id)
                ? nameof(form.id)
                : form.sign is null || form.sign.Length == 0
                ? nameof(form.sign)
                : form.cert is null || form.cert.Length == 0
                ? nameof(form.cert)
                : null;

            if (!string.IsNullOrWhiteSpace(missingParameter))
            {
                error = AnswerErrorCode.Code3_EmptyRequiredParameters(missingParameter);
                _logger.LogError(error.Message);

                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            await _storageService.AddHash(serviceName, guid, "request_id", form.id);

            var signStream = new MemoryStream();
            var certStream = new MemoryStream();

            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certBytes = certStream.ToArray(); // DER
            var signBytes = signStream.ToArray();

            await _storageService.AddHash(serviceName, guid, "cert", certBytes);
            await _storageService.AddHash(serviceName, guid, "sign", signBytes);

            try
            {
                X509Certificate2 certFromForm = new(certBytes);
                await _storageService.AddHash(serviceName, guid, "cert_thumbprint", certFromForm.Thumbprint ?? "-");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{serviceName} не удалось считать thumbprint сертификата из формы", serviceName);
            }

            var isMsgValid = _cryptoService.ValidateMsg(certBytes, certificate, out var validateMsgResult, signBytes);

            if (!isMsgValid && validateMsgResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(validateMsgResult.ErrorCode, validateMsgResult.ErrorMessage));

                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _storageService.AddHash(serviceName, guid, "error_code", validateMsgResult.ErrorCode.ToString());
                await _storageService.AddHash(serviceName, guid, "error_message", validateMsgResult.ErrorMessage);
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (string.IsNullOrWhiteSpace(validateMsgResult.RequestOGRN))
            {
                error = AnswerErrorCode.Code99_OtherError("Не удалось определить ОГРН абонента по действующему сертификату");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var (isUniqueId, uniqueValidationResult) = await _validationServiceV3.IsUniqueRequestIdV3Async(form.id, serviceName, validateMsgResult.RequestOGRN);

            if (!isUniqueId & uniqueValidationResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(uniqueValidationResult.ErrorCode, uniqueValidationResult.ErrorMessage));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _storageService.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _storageService.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");
                return BadRequest(new MemoryStream(signedResponse));
            }

            await _storageService.AddUniqueRequestId(serviceName, form.id, validateMsgResult.RequestOGRN, DateTime.Now);

            if (await _validationServiceV3.IsCertExistsV3(certBytes))
            {
                error = AnswerErrorCode.Code99_OtherError("Такой сертификат уже существует.");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            if (!await _certManagement.AddCertificate(certBytes, validateMsgResult.RequestOGRN, guid))
            {
                error = AnswerErrorCode.Code99_OtherError("Не удалось добавить сертификат.");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var successTicket = _ticketServiceV3.CreateResultV3Success(form.id, DateTime.Today);
            responseXml = _xmlServiceV3.SerializeAsByteV3(successTicket);
            signedResponse = _cryptoService.SignMsg(responseXml);
            return File(signedResponse, "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /certadd v3");
            await _storageService.AddHash(serviceName, guid, "error_code", "500");
            await _storageService.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _storageService.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _storageService.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _storageService.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _storageService.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            LogActionEnd(nameof(CertAdd_v_3), guid, Response.StatusCode, actionStopwatch.Elapsed);
        }
    }

    [HttpPost("certrevoke")]
    public async Task<IActionResult> CertRevoke_v_3([FromForm] CertForm form)
    {
        byte[]? responseXml = null;
        byte[]? signedResponse = null;
        var guid = Guid.NewGuid().ToString();
        const string serviceName = "certrevoke";
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var actionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Начало действия {Action} service={ServiceName} guid={Guid} в {RequestTime}", nameof(CertRevoke_v_3), serviceName, guid, requestTime);

        await _storageService.AddHash(serviceName, guid, "request_date_time", requestTime);
        await _storageService.AddHash(serviceName, guid, "temp_guid", guid);

        var requestCertificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        await _storageService.AddHash(serviceName, guid, "request_certificate_thumbprint", requestCertificate?.Thumbprint ?? "-");
        await _storageService.AddHash(serviceName, guid, "request_certificate_data", requestCertificate?.RawData ?? Encoding.UTF8.GetBytes("-"));
        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _storageService.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            if (!await _validationServiceV3.ValidateRulesV3(requestCertificate?.Thumbprint, serviceName))
            {
                var error = AnswerErrorCode.Code22_AccessDenied();
                _logger.LogError(error.Message);
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (string.IsNullOrWhiteSpace(form.id))
            {
                var error = AnswerErrorCode.Code3_EmptyRequiredParameters(nameof(form.id));
                _logger.LogError(error.Message);
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.sign is null)
            {
                var error = AnswerErrorCode.Code3_EmptyRequiredParameters(nameof(form.sign));
                _logger.LogError(error.Message);
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.cert is null)
            {
                var error = AnswerErrorCode.Code3_EmptyRequiredParameters(nameof(form.cert));
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _storageService.AddHash(serviceName, guid, "request_guid", form.id);

            await using var certStream = new MemoryStream();
            await using var signStream = new MemoryStream();
            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certRaw = certStream.ToArray();
            var signRaw = signStream.ToArray();
            await _storageService.AddHash(serviceName, guid, "request_certificate_to_revoke", certRaw);
            await _storageService.AddHash(serviceName, guid, "request_sign", signRaw);

            X509Certificate2 certToRevoke;
            try
            {
                certToRevoke = new X509Certificate2(certRaw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось прочитать cert как DER");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 7, "Некорректный формат cert. Требуется DER", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _storageService.AddHash(serviceName, guid, "request_revoke_thumbprint", certToRevoke.Thumbprint ?? "-");

            var isMsgValid = _cryptoService.ValidateMsg(certRaw, requestCertificate, out var cryptoResult, signRaw);

            if (!isMsgValid & cryptoResult is not null)
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(cryptoResult.ErrorCode, cryptoResult.Error));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", cryptoResult.ErrorCode.ToString());
                await _storageService.AddHash(serviceName, guid, "error_message", cryptoResult.Error ?? "-");

                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.IsCertActiveV3(cryptoResult.SignThumbprint ?? string.Empty))
            {
                var error = AnswerErrorCode.Code99_OtherError("Сертификат подписи не является действующим");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            var (isUniqueRevoke, uniqueResult) = await _validationServiceV3.IsUniqueRequestIdV3Async(form.id, serviceName, cryptoResult.RequestOGRN ?? string.Empty);

            if (!isUniqueRevoke)
            {
                responseXml = _xmlServiceV3.SerializeAsByteV3(uniqueResult!.Ticket!);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _storageService.AddHash(serviceName, guid, "error_code", uniqueResult.ErrorCode.ToString());
                await _storageService.AddHash(serviceName, guid, "error_message", uniqueResult.Error ?? "-");
                return BadRequest(new MemoryStream(signedResponse));
            }

            await _storageService.AddUniqueRequestId(serviceName, form.id, cryptoResult.RequestOGRN!, DateTime.Now);

            if (!await _validationServiceV3.IsCertExistsV3(certRaw))
            {
                var error = AnswerErrorCode.Code99_OtherError("Сертификат не найден");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            var activeCertsCount = await _validationServiceV3.GetActiveCertificatesCountV3(certRaw);
            if (activeCertsCount <= 1)
            {
                var error = AnswerErrorCode.Code99_OtherError("Отзыв последнего действующего сертификата абонента запрещен Порядком 3.0");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            if (!await _validationServiceV3.SetCertificateInactiveV3(certRaw))
            {
                var error = AnswerErrorCode.Code99_OtherError("Не удалось отозвать сертификат");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, error.Code, error.Message, StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;

                return errorResult.ActionResult;
            }

            var successTicket = _ticketServiceV3.CreateResultV3Success(form.id, DateTime.Now);
            responseXml = _xmlServiceV3.SerializeAsByteV3(successTicket);
            signedResponse = _cryptoService.SignMsg(responseXml);
            return File(signedResponse, "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /certrevoke v3");
            await _storageService.AddHash(serviceName, guid, "error_code", "500");
            await _storageService.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (responseXml is not null)
                await _storageService.AddHash(serviceName, guid, "response_xml", responseXml);
            if (signedResponse is not null)
                await _storageService.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (!await _storageService.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _storageService.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _storageService.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        }
    }

    private sealed class V3ErrorResponseBuildResult
    {
        public byte[] ResponseXml { get; init; } = Array.Empty<byte>();
        public byte[] SignedResponse { get; init; } = Array.Empty<byte>();
        public IActionResult ActionResult { get; init; } = default!;
    }

    private async Task<V3ErrorResponseBuildResult> BuildV3ErrorResponseAsync(string serviceName, string guid, int code, string message, int statusCode)
    {
        _logger.LogError("Ошибка обработки v3 service={QbchService} guid={Guid} code={QbchErrorCode} status={StatusCode}: {QbchErrorMessage}", serviceName, guid, code, statusCode, message);
        await _storageService.AddHash(serviceName, guid, "error_code", code.ToString());
        await _storageService.AddHash(serviceName, guid, "error_message", message);

        var ticket = _ticketServiceV3.CreateResultV3Error(new AnswerErrorCode(code, message));
        var responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
        var signedResponse = _cryptoService.SignMsg(responseXml);

        Response.StatusCode = statusCode;

        return new V3ErrorResponseBuildResult
        {
            ResponseXml = responseXml,
            SignedResponse = signedResponse,
            ActionResult = File(signedResponse, "application/octet-stream")
        };
    }

    private static int ResolveDlAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    private static int ResolveDlPutAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    private async Task<DateTimeOffset?> GetFirstPollAllowedAtUtcAsync(string scope, string responseId)
    {
        if (_storageService.TryGetHashValue(scope, responseId, FirstPollAllowedAtUtcField, out var firstPollRaw) &&
            DateTimeOffset.TryParse(firstPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var firstPollAllowedAtUtc))
        {
            return firstPollAllowedAtUtc;
        }

        if (_storageService.TryGetHashValue(scope, responseId, ReadyAtUtcField, out var readyAtRaw) &&
            DateTimeOffset.TryParse(readyAtRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var readyAtUtc))
        {
            await _storageService.AddHash(scope, responseId, FirstPollAllowedAtUtcField, readyAtUtc.ToString("O"));
            return readyAtUtc;
        }

        return null;
    }

    private void LogActionEnd(string action, object transactionId, int statusCode, TimeSpan elapsed, string? guid = null)
    {
        if (guid is null)
        {
            _logger.LogInformation("Выполнен запрос {Action} transactionId = {transactionId} status={StatusCode} elapsed={ElapsedMs}ms", action, transactionId, statusCode, elapsed.TotalMilliseconds);
            return;
        }

        _logger.LogInformation(
            "Выполнен запрос {Action} transactionId = {transactionId} status={StatusCode} guid={Guid} elapsed={ElapsedMs}ms", action, transactionId, statusCode, guid, elapsed.TotalMilliseconds);
    }
}
