using Asp.Versioning;
using Cache_lib.Interfaces;
using CertManagement.Services.Interfaces;
using Crypto_lib.Service;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;
using QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;
using QBCH_api.QBCHProcessing.V3.StoreProcessingData.Event;
using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces.V3;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;
namespace QBCH_api.Controllers;

[ApiVersion("3.0")]
[Route("v{version:apiVersion}")]
[ApiController]
public class QBCHIIIController(
        IMediator mediator,
        ICryptoService cryptoService,
        ILogger<QBCHIIIController> logger,
        IXmlServiceV3 xmlServiceV3,
        ICacheService redisСache,
        IValidationServiceV3 validationServiceV3,
        ITicketServiceV3 ticketServiceV3,
        IDlPutServiceV3 dlPutServiceV3,
        ICertManagementService certManagement,
        ApiV3ContractRules contractRules,
        IConfiguration config) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ILogger<QBCHIIIController> _logger = logger;
    private readonly IXmlServiceV3 _xmlServiceV3 = xmlServiceV3;
    private readonly ICacheService _redisCache = redisСache;
    private readonly IValidationServiceV3 _validationServiceV3 = validationServiceV3;
    private readonly ITicketServiceV3 _ticketServiceV3 = ticketServiceV3;
    private readonly IDlPutServiceV3 _dlPutServiceV3 = dlPutServiceV3;
    private readonly ICertManagementService _certManagement = certManagement;
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly IConfiguration _config = config;
    private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");
    private const string DlPutAnswerV3ReadyField = "putanswer_v3_response_xml";
    private const string DlPutAnswerV3ExistsField = "putanswer_v3_exists";
    private const string DlRequestV3Scope = "dlrequest:v3";
    private const string DlPutV3Scope = "dlput:v3";
    private const string DlAnswerV3Scope = "dlanswer:v3";
    private const string DlPutAnswerV3Scope = "dlputanswer:v3";
    private const string ReadyAtUtcField = "ready_at_utc";
    private const string ReadyAtMskField = "ready_at_msk";
    private const string FirstPollAllowedAtUtcField = "first_poll_allowed_at_utc";
    private const string ResponseExpireAtUtcField = "response_expire_at_utc";
    private const string LastPollUtcField = "last_poll_utc";


    [HttpPost("dlrequest")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlRequest_v_3(ApiVersion apiVersion)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

        var transaction = await _mediator.Send(new CreateToValidateCommandV3(apiVersion, Request));
        _logger.LogDebug("{guid} Request: {dt}", transaction.Id, requestTime);

        if (transaction.ProcessingErrors.Count != 0)
        {
            var errorTicket = _ticketServiceV3.CreateResultV3Error(transaction.ProcessingErrors.First());
            var errorResult = _xmlServiceV3.SerializeAsByteV3(errorTicket);
            var signedResp = _cryptoService.SignMsg(errorResult);

            transaction.Complete(errorResult, signedResp);

            await _mediator.Publish(new QBCHProcessingCompleteV3(transaction));

            return BadRequest(new MemoryStream(transaction.Response.SignedTicket!));
        }

        try
        {
            var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();
            var requestId = requestV3?.ИдентификаторЗапроса;
            var requestDate = requestV3?.ДатаЗапроса;
            var requestOgrn = requestV3?.Абонент?.Item switch
            {
                АбонентИЮЛV3 юрЛицо => юрЛицо.ОГРН,
                АбонентИПV3 ип => ип.ОГРНИП,
                АбонентИноV3 ино => ино.РегНомер,
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(requestId) &&
                !string.IsNullOrWhiteSpace(requestOgrn) &&
                requestDate.HasValue)
            {
                await _redisCache.AddUniqueRequestId(DlRequestV3Scope, requestId, requestOgrn, requestDate.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи уникальнго requestId в redis");
        }

        transaction.TimeElapsedForValidation.Stop();
        _logger.LogDebug("{guid} Validation time elapsed: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

        try
        {
            var processingResult = await _mediator
                .Send(new QBCHProcessedStartV3(
                    transaction,
                    _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs"),
                    _ourBureauPSRN ?? string.Empty));

            Response.OnCompleted(async () => await _mediator.Publish(new QBCHProcessingCompleteV3(processingResult)));

            if (processingResult.Response.SignedTicket is not null && processingResult.Response.TicketXML is not null)
            {
                var statusCode = DetermineTicketStatusCode(processingResult.Response.TicketXML);
                Response.StatusCode = statusCode;
                return File(processingResult.Response.SignedTicket, "application/octet-stream");
            }

            Response.StatusCode = StatusCodes.Status200OK;
            return File(processingResult.Response.SignedResponse!, "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка");
            return StatusCode(500);
        }
    }

    [HttpGet("dlanswer")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlAnswer_v_3(string? id = null)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = DlAnswerV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Запрос не содержит обязательных параметров: id",
                     ResolveDlAnswerStatusCodeByErrorCode(3));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "response_guid", id);

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, DlRequestV3Scope))
            {
                _logger.LogError("Запрос не доступен для абонента");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    22,
                    "Запрос не доступен для абонента",
                    ResolveDlAnswerStatusCodeByErrorCode(22));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _redisCache.KeyExists([DlRequestV3Scope, id]))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.TrySetKeyExpiration(DlRequestV3Scope, id, _contractRules.ResponseRetentionMinutes);

            var nowUtc = DateTimeOffset.UtcNow;
            var firstPollAllowedAtUtc = await GetFirstPollAllowedAtUtcAsync(DlRequestV3Scope, id);

            if (firstPollAllowedAtUtc.HasValue && nowUtc < firstPollAllowedAtUtc.Value)
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlanswer v3 id={id}. Первый опрос разрешён с {firstPollAllowedAtUtc}, текущее UTC={nowUtc}.", id, firstPollAllowedAtUtc.Value, nowUtc);

                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
                await _redisCache.ListSet([serviceName, "polling_violations", id], $"{nowUtc:O}|{ipAddress ?? "-"}|min_interval={minIntervalSec}s");
                await _redisCache.TrySetKeyExpiration(serviceName, $"polling_violations:{id}", _contractRules.ResponseRetentionMinutes);

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    12,
                    "Ответ не готов",
                    ResolveDlAnswerStatusCodeByErrorCode(12));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (_redisCache.TryGetHashValue(DlRequestV3Scope, id, LastPollUtcField, out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlanswer v3 id={id}. Последний опрос={lastPollUtc}, текущий UTC={nowUtc}, min={interval} сек.", id, lastPollUtc, nowUtc, minIntervalSec);
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            await _redisCache.AddHash(DlRequestV3Scope, id, LastPollUtcField, nowUtc.ToString("O"));
            await _redisCache.TrySetKeyExpiration(DlRequestV3Scope, id, _contractRules.ResponseRetentionMinutes);

            await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            if (_redisCache.TryGetHash(DlRequestV3Scope, id, "qbch_tasks_aggregate_xml", out responseXml))
            {
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            var notReadyResult = await BuildV3ErrorResponseAsync(
                serviceName,
                guid,
                12,
                "Ответ не готов",
                ResolveDlAnswerStatusCodeByErrorCode(12));

            responseXml = notReadyResult.ResponseXml;
            signedResponse = notReadyResult.SignedResponse;
            return notReadyResult.ActionResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /dlanswer v3");
            await _redisCache.AddHash(serviceName, guid, "error_code", "500");
            await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    [HttpPost("dlput")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlPut_v_3(ApiVersion apiVersion)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = DlPutV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            if (!string.Equals(Request.Method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Метод передачи запроса не соответствует ожидаемому");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    1,
                    "Метод передачи запроса не соответствует ожидаемому",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            using var bodyStream = new MemoryStream();
            await Request.Body.CopyToAsync(bodyStream);
            var bodyBytes = bodyStream.ToArray();

            if (bodyBytes.Length == 0)
            {
                _logger.LogError("Пустое тело запроса");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    2,
                    "Тело запроса отсутствует",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!_validationServiceV3.ValidateEncodingV3(bodyBytes, out var encodingValidationResult))
            {
                var ticket = encodingValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(8, "Неподдерживаемая кодировка, файл не в кодировке Utf-8"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", encodingValidationResult?.ErrorCode.ToString() ?? "8");
                await _redisCache.AddHash(serviceName, guid, "error_message", encodingValidationResult?.Error ?? "Неподдерживаемая кодировка, файл не в кодировке Utf-8");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    22,
                    "Запрос не доступен для абонента",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            using var validationStream = new MemoryStream(bodyBytes);
            if (!_validationServiceV3.ValidateXmlV3(validationStream, serviceName, out var xsdValidationResult))
            {
                var ticket = xsdValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(9, "Запрос не соответствует схеме"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", xsdValidationResult?.ErrorCode.ToString() ?? "9");
                await _redisCache.AddHash(serviceName, guid, "error_message", xsdValidationResult?.Error ?? "Запрос не соответствует схеме");
                return BadRequest(new MemoryStream(signedResponse));
            }

            var requestV3 = _xmlServiceV3.DeserializeV3<ПредставлениеСведенийV3>(bodyBytes);
            if (requestV3 is null)
            {
                _logger.LogError("Не удалось десериализовать тело запроса dlput v3");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    9,
                    "Не удалось прочитать XML запроса",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var entitiesCount = requestV3.Сведения?.Length ?? 0;

            if (entitiesCount > 1000)
            {
                _logger.LogError("Превышен лимит количества элементов Сведения: {count}", entitiesCount);

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Количество элементов Сведения превышает допустимый лимит 1000",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult))
            {
                var ticket = dateValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(23, "Дата запроса указана некорректно"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", dateValidationResult?.ErrorCode.ToString() ?? "23");
                await _redisCache.AddHash(serviceName, guid, "error_message", dateValidationResult?.Error ?? "Дата запроса указана некорректно");
                return BadRequest(new MemoryStream(signedResponse));
            }

            var requestId = requestV3.ИдентификаторЗапроса;
            var requestOgrn = requestV3.БКИ?.ОГРН ?? string.Empty;

            if (!_validationServiceV3.IsUniqueRequestIdV3(requestId, serviceName, requestOgrn, out var uniqueValidationResult))
            {
                var ticket = uniqueValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(11, "Идентификатор запроса не уникален"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");
                return BadRequest(new MemoryStream(signedResponse));
            }

            var dlPutResult = _dlPutServiceV3.Process(requestV3);
            if (dlPutResult.IsAccepted)
            {
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

                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, DlPutAnswerV3ExistsField, "1");
                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ReadyAtUtcField, readyAtUtc.ToString("O"));
                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ReadyAtMskField, readyAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("O"));
                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, FirstPollAllowedAtUtcField, firstPollAllowedAtUtc.ToString("O"));
                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, ResponseExpireAtUtcField, responseExpireAtUtc.ToString("O"));
                    await _redisCache.AddHash(serviceName, acceptedResponseId.ИдентификаторОтвета, "response_guid", acceptedResponseId.ИдентификаторОтвета);
                    await _redisCache.TrySetKeyExpiration(serviceName, acceptedResponseId.ИдентификаторОтвета, _contractRules.ResponseRetentionMinutes);
                    await _redisCache.AddHash(serviceName, guid, "response_guid", acceptedResponseId.ИдентификаторОтвета);
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

            await _redisCache.AddUniqueRequestId(serviceName, requestId, requestOgrn, requestV3.ДатаЗапроса);
            await _redisCache.AddHash(serviceName, dlPutResult.ReadyResult!.ИдентификаторОтвета, DlPutAnswerV3ReadyField, responseXml);
            await _redisCache.AddHash(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, DlPutAnswerV3ExistsField, "1");
            await _redisCache.TrySetKeyExpiration(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, _contractRules.ResponseRetentionMinutes);
            await _redisCache.AddHash(serviceName, guid, "response_guid", dlPutResult.ReadyResult.ИдентификаторОтвета);

            return File(signedResponse, "application/octet-stream");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Количество элементов Сведения", StringComparison.Ordinal))
        {
            _logger.LogError(ex, "Ошибка бизнес-валидации dlput v3");

            var errorResult = await BuildV3ErrorResponseAsync(
                serviceName,
                guid,
                3,
                "Количество элементов Сведения превышает допустимый лимит 1000",
                StatusCodes.Status400BadRequest);

            responseXml = errorResult.ResponseXml;
            signedResponse = errorResult.SignedResponse;
            return errorResult.ActionResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /dlput v3");
            await _redisCache.AddHash(serviceName, guid, "error_code", "500");
            await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    [HttpGet("dlputanswer")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlPutAnswer_v_3(ApiVersion version, string? id = null)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = DlPutAnswerV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Запрос не содержит обязательных параметров: id",
                    ResolveDlPutAnswerStatusCodeByErrorCode(3));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "response_guid", id);

            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, "dlput"))
            {
                _logger.LogError("Запрос не доступен для абонента");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    22,
                    "Запрос не доступен для абонента",
                    ResolveDlPutAnswerStatusCodeByErrorCode(22));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!await _redisCache.KeyExists([DlPutV3Scope, id]))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlPutAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!await _redisCache.HashFieldExists(DlPutV3Scope, id, DlPutAnswerV3ExistsField))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlPutAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.TrySetKeyExpiration(DlPutV3Scope, id, _contractRules.ResponseRetentionMinutes);
            var nowUtc = DateTimeOffset.UtcNow;
            var firstPollAllowedAtUtc = await GetFirstPollAllowedAtUtcAsync(DlPutV3Scope, id);

            if (firstPollAllowedAtUtc.HasValue && nowUtc < firstPollAllowedAtUtc.Value)
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Первый опрос разрешён с {firstPollAllowedAtUtc}, текущее UTC={nowUtc}.", id, firstPollAllowedAtUtc.Value, nowUtc);
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            if (_redisCache.TryGetHashValue(DlPutV3Scope, id, LastPollUtcField, out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Последний опрос={lastPollUtc}, текущий UTC={nowUtc}, min={interval} сек.", id, lastPollUtc, nowUtc, _contractRules.MinAnswerPollingIntervalSeconds);
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            await _redisCache.AddHash(DlPutV3Scope, id, LastPollUtcField, nowUtc.ToString("O"));
            await _redisCache.TrySetKeyExpiration(DlPutV3Scope, id, _contractRules.ResponseRetentionMinutes);

            if (_redisCache.TryGetHash(DlPutV3Scope, id, DlPutAnswerV3ReadyField, out responseXml))
            {
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            var notReadyResult = await BuildV3ErrorResponseAsync(
                serviceName,
                guid,
                12,
                "Ответ не готов",
                ResolveDlPutAnswerStatusCodeByErrorCode(12));

            responseXml = notReadyResult.ResponseXml;
            signedResponse = notReadyResult.SignedResponse;
            return notReadyResult.ActionResult;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка в /dlputanswer v3");
            await _redisCache.AddHash(serviceName, guid, "error_code", "500");
            await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    [HttpPost("certadd")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> CertAdd_v_3([FromForm] CertForm form)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = "certadd";
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
        await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
        await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
        await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.RawData ?? Array.Empty<byte>());

        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            if (!string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Метод передачи запроса не соответствует ожидаемому");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    1,
                    "Метод передачи запроса не соответствует ожидаемому",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    22,
                    "Запрос не доступен для абонента",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (string.IsNullOrWhiteSpace(form.id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Запрос не содержит обязательных параметров: id",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.sign is null)
            {
                _logger.LogError("Запрос не содержит обязательных параметров: sign");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Запрос не содержит обязательных параметров: sign",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.cert is null)
            {
                _logger.LogError("Запрос не содержит обязательных параметров: cert");

                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    3,
                    "Запрос не содержит обязательных параметров: cert",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "request_id", form.id);

            var signStream = new MemoryStream();
            var certStream = new MemoryStream();

            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certBytes = certStream.ToArray(); // DER
            var signBytes = signStream.ToArray();

            await _redisCache.AddHash(serviceName, guid, "cert", certBytes);
            await _redisCache.AddHash(serviceName, guid, "sign", signBytes);

            try
            {
                X509Certificate2 certFromForm = new(certBytes);
                await _redisCache.AddHash(serviceName, guid, "cert_thumbprint", certFromForm.Thumbprint ?? "-");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{serviceName} не удалось считать thumbprint сертификата из формы", serviceName);
            }

            if (!_cryptoService.ValidateMsg(certBytes, certificate, out var validateMsgResult, signBytes))
            {
                var errorCode = validateMsgResult.ErrorCode == 0 ? 99 : validateMsgResult.ErrorCode;
                var errorMessage = string.IsNullOrWhiteSpace(validateMsgResult.Error) ? "Ошибка проверки подписи сертификата" : validateMsgResult.Error;
                var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(errorCode, errorMessage));

                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", errorCode.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", errorMessage);
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (string.IsNullOrWhiteSpace(validateMsgResult.RequestOGRN))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    99,
                    "Не удалось определить ОГРН абонента по действующему сертификату",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.IsUniqueRequestIdV3(form.id, serviceName, validateMsgResult.RequestOGRN, out var uniqueValidationResult))
            {
                var ticket = uniqueValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(11, "Идентификатор запроса не уникален"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");
                return BadRequest(new MemoryStream(signedResponse));
            }

            await _redisCache.AddUniqueRequestId(serviceName, form.id, validateMsgResult.RequestOGRN, DateTime.Now);

            if (await _validationServiceV3.IsCertExistsV3(certBytes))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    99,
                    "Такой сертификат уже существует.",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            if (!await _certManagement.AddCertificate(certBytes, validateMsgResult.RequestOGRN, guid))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    99,
                    "Не удалось добавить сертификат.",
                    StatusCodes.Status400BadRequest);

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
            await _redisCache.AddHash(serviceName, guid, "error_code", "500");
            await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    [HttpPost("certrevoke")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> CertRevoke_v_3([FromForm] CertForm form)
    {
        byte[]? responseXml = null;
        byte[]? signedResponse = null;
        var guid = Guid.NewGuid().ToString();
        const string serviceName = "certrevoke";

        await _redisCache.AddHash(serviceName, guid, "request_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);

        var requestCertificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", requestCertificate?.Thumbprint ?? "-");
        await _redisCache.AddHash(serviceName, guid, "request_certificate_data", requestCertificate?.RawData ?? Encoding.UTF8.GetBytes("-"));
        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            if (!await _validationServiceV3.ValidateRulesV3(requestCertificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 22, "Запрос не доступен для абонента", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (string.IsNullOrWhiteSpace(form.id))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: id", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.sign is null)
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: sign", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (form.cert is null)
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: cert", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "request_guid", form.id);

            await using var certStream = new MemoryStream();
            await using var signStream = new MemoryStream();
            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certRaw = certStream.ToArray();
            var signRaw = signStream.ToArray();
            await _redisCache.AddHash(serviceName, guid, "request_certificate_to_revoke", certRaw);
            await _redisCache.AddHash(serviceName, guid, "request_sign", signRaw);

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

            await _redisCache.AddHash(serviceName, guid, "request_revoke_thumbprint", certToRevoke.Thumbprint ?? "-");

            if (!_cryptoService.ValidateMsg(certRaw, requestCertificate, out var cryptoResult, signRaw))
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(cryptoResult.ErrorCode, cryptoResult.Error ?? "УЭП некорректна"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", cryptoResult.ErrorCode.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", cryptoResult.Error ?? "-");
                return BadRequest(new MemoryStream(signedResponse));
            }

            if (!await _validationServiceV3.IsCertActiveV3(cryptoResult.SignThumbprint ?? string.Empty))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Сертификат подписи не является действующим", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!_validationServiceV3.IsUniqueRequestIdV3(form.id, serviceName, cryptoResult.RequestOGRN ?? string.Empty, out var uniqueResult))
            {
                responseXml = _xmlServiceV3.SerializeAsByteV3(uniqueResult!.TicketV3!);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueResult.ErrorCode.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueResult.Error ?? "-");
                return BadRequest(new MemoryStream(signedResponse));
            }

            await _redisCache.AddUniqueRequestId(serviceName, form.id, cryptoResult.RequestOGRN!, DateTime.Now);

            if (!await _validationServiceV3.IsCertExistsV3(certRaw))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Сертификат не найден", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var activeCertsCount = await _validationServiceV3.GetActiveCertificatesCountV3(certRaw);
            if (activeCertsCount <= 1)
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName,
                    guid,
                    99,
                    "Отзыв последнего действующего сертификата абонента запрещен Порядком 3.0",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            if (!await _validationServiceV3.SetCertificateInactiveV3(certRaw))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Не удалось отозвать сертификат", StatusCodes.Status400BadRequest);
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
            await _redisCache.AddHash(serviceName, guid, "error_code", "500");
            await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
            return StatusCode(500);
        }
        finally
        {
            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    private sealed class V3ErrorResponseBuildResult
    {
        public byte[] ResponseXml { get; init; } = Array.Empty<byte>();
        public byte[] SignedResponse { get; init; } = Array.Empty<byte>();
        public IActionResult ActionResult { get; init; } = default!;
    }

    private async Task<V3ErrorResponseBuildResult> BuildV3ErrorResponseAsync(
    string serviceName,
    string guid,
    int code,
    string message,
    int statusCode)
    {
        await _redisCache.AddHash(serviceName, guid, "error_code", code.ToString());
        await _redisCache.AddHash(serviceName, guid, "error_message", message);

        var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(code, message));
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

    private int DetermineTicketStatusCode(byte[] ticketXml)
    {
        var ticket = _xmlServiceV3.DeserializeV3<Результат>(ticketXml);
        return ticket?.Item switch
        {
            ТипОшибка => StatusCodes.Status400BadRequest,
            РезультатИдентификаторОтвета => StatusCodes.Status202Accepted,
            _ => StatusCodes.Status200OK
        };
    }

    private static int ResolveDlAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    private static int ResolveDlPutAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    private async Task<DateTimeOffset?> GetFirstPollAllowedAtUtcAsync(string scope, string responseId)
    {
        if (_redisCache.TryGetHashValue(scope, responseId, FirstPollAllowedAtUtcField, out var firstPollRaw) &&
            DateTimeOffset.TryParse(firstPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var firstPollAllowedAtUtc))
        {
            return firstPollAllowedAtUtc;
        }

        if (_redisCache.TryGetHashValue(scope, responseId, ReadyAtUtcField, out var readyAtRaw) &&
            DateTimeOffset.TryParse(readyAtRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var readyAtUtc))
        {
            await _redisCache.AddHash(scope, responseId, FirstPollAllowedAtUtcField, readyAtUtc.ToString("O"));
            return readyAtUtc;
        }

        return null;
    }
}