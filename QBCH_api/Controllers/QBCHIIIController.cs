using Asp.Versioning;
using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH_api.QBCHProcessing.V2.StoreProcessingData.Event;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;
using QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;
using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.core;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.domain.aggregate;
using QBCH_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
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
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly IConfiguration _config = config;
    private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");


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

            await _mediator.Publish(new QBCHProcessingComplete(transaction));

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
                var uniqueScope = $"{transaction.ServiceName}:v{apiVersion}";
                await _redisCache.AddUniqueRequestId(uniqueScope, requestId, requestOgrn, requestDate.Value);
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

            Response.OnCompleted(async () => await _mediator.Publish(new QBCHProcessingComplete(processingResult)));

            return processingResult.Status switch
            {
                QBCHProcessingStatus.Accepted =>
                    Accepted(new MemoryStream(processingResult.Response.SignedTicket!)),
                _ => File(processingResult.Response.SignedResponse!, "application/octet-stream")
            };
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
        var serviceName = "dlanswer";
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        async Task<IActionResult> BuildV3ErrorResponse(int code, string message, bool accepted = false)
        {
            await _redisCache.AddHash(serviceName, guid, "error_code", code.ToString());
            await _redisCache.AddHash(serviceName, guid, "error_message", message);

            var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(code, message));
            responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
            signedResponse = _cryptoService.SignMsg(responseXml);

            return accepted
                ? Accepted(new MemoryStream(signedResponse))
                : BadRequest(new MemoryStream(signedResponse));
        }

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
                return await BuildV3ErrorResponse(3, "Запрос не содержит обязательных параметров: id");
            }

            await _redisCache.AddHash(serviceName, guid, "response_guid", id);

            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, "dlrequest"))
            {
                _logger.LogError("Запрос не доступен для абонента");
                return await BuildV3ErrorResponse(22, "Запрос не доступен для абонента");
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

            if (!await _redisCache.KeyExists(["dlrequest", id]))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");
                return await BuildV3ErrorResponse(16, "Указан некорректный идентификатор ответа");
            }

            await _redisCache.TrySetKeyExpiration("dlrequest", id, _contractRules.ResponseRetentionMinutes);

            var nowUtc = DateTimeOffset.UtcNow;
            var pollingKey = $"{id}:polling";

            if (_redisCache.TryGetHashValue(serviceName, pollingKey, "last_poll_utc", out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlanswer v3 id={id}. Интервал меньше {interval} сек.", id, minIntervalSec);

                await _redisCache.AddHash(serviceName, pollingKey, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(serviceName, pollingKey, "polling_violation_ip", ipAddress ?? "-");
                await _redisCache.ListSet([serviceName, "polling_violations", id], $"{nowUtc:O}|{ipAddress ?? "-"}|min_interval={minIntervalSec}s");
                await _redisCache.TrySetKeyExpiration(serviceName, pollingKey, _contractRules.ResponseRetentionMinutes);
                await _redisCache.TrySetKeyExpiration(serviceName, $"polling_violations:{id}", _contractRules.ResponseRetentionMinutes);

                return await BuildV3ErrorResponse(12, "Ответ не готов", accepted: true);
            }

            await _redisCache.AddHash(serviceName, pollingKey, "last_poll_utc", nowUtc.ToString("O"));
            await _redisCache.TrySetKeyExpiration(serviceName, pollingKey, _contractRules.ResponseRetentionMinutes);

            await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            if (_redisCache.TryGetHash("dlrequest", id, "qbch_tasks_aggregate_xml", out responseXml))
            {
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            return await BuildV3ErrorResponse(12, "Ответ не готов", accepted: true);
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
        const string serviceName = "dlput";
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        async Task<IActionResult> BuildV3ErrorResponse(int code, string message)
        {
            await _redisCache.AddHash(serviceName, guid, "error_code", code.ToString());
            await _redisCache.AddHash(serviceName, guid, "error_message", message);

            var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(code, message));
            responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
            signedResponse = _cryptoService.SignMsg(responseXml);
            return BadRequest(new MemoryStream(signedResponse));
        }

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
                return await BuildV3ErrorResponse(1, "Метод передачи запроса не соответствует ожидаемому");
            }

            using var bodyStream = new MemoryStream();
            await Request.Body.CopyToAsync(bodyStream);
            var bodyBytes = bodyStream.ToArray();

            if (bodyBytes.Length == 0)
            {
                _logger.LogError("Пустое тело запроса");
                return await BuildV3ErrorResponse(2, "Тело запроса отсутствует");
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
                return await BuildV3ErrorResponse(22, "Запрос не доступен для абонента");
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
                return await BuildV3ErrorResponse(9, "Не удалось прочитать XML запроса");
            }

            var entitiesCount = requestV3.Сведения?.Length ?? 0;
            if (entitiesCount > 1000)
            {
                _logger.LogError("Превышен лимит количества элементов Сведения: {count}", entitiesCount);
                return await BuildV3ErrorResponse(3, "Количество элементов Сведения превышает допустимый лимит 1000");
            }

            var requestId = requestV3.ИдентификаторЗапроса;
            var requestOgrn = requestV3.БКИ?.ОГРН ?? string.Empty;
            if (!_validationServiceV3.IsUniqueRequestIdV3(requestId, $"{serviceName}:v{apiVersion}", requestOgrn, out var uniqueValidationResult))
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
                responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.AcceptedTicket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                return Accepted(new MemoryStream(signedResponse));
            }

            responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.ReadyResult);
            signedResponse = _cryptoService.SignMsg(responseXml);

            await _redisCache.AddUniqueRequestId($"{serviceName}:v{apiVersion}", requestId, requestOgrn, requestV3.ДатаЗапроса);
            await _redisCache.AddHash(serviceName, dlPutResult.ReadyResult!.ИдентификаторОтвета, "response", responseXml);
            await _redisCache.TrySetKeyExpiration(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, _contractRules.ResponseRetentionMinutes);
            await _redisCache.AddHash(serviceName, guid, "response_guid", dlPutResult.ReadyResult.ИдентификаторОтвета);

            return File(signedResponse, "application/octet-stream");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Количество элементов Сведения", StringComparison.Ordinal))
        {
            _logger.LogError(ex, "Ошибка бизнес-валидации dlput v3");
            return await BuildV3ErrorResponse(3, "Количество элементов Сведения превышает допустимый лимит 1000");
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
    public Task<IActionResult> DlPutAnswer_v_3(ApiVersion version, string? id = null)
    {
        throw new System.NotImplementedException();
    }

    [HttpPost("certadd")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> CertAdd_v_3([FromForm] CertForm form)
    {
        throw new System.NotImplementedException();
    }

    [HttpPost("certrevoke")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> CertRevoke_v_3([FromForm] CertForm form)
    {
        throw new System.NotImplementedException();
    }
}