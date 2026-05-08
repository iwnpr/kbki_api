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

    // ApiV3ContractRules: правила контракта из конфигурации — polling-интервал, TTL ответа, лимит Сведений
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly IConfiguration _config = config;

    // ОГРН нашего бюро — используется при проверке ОГРН в теле dlput, чтобы убедиться, что данные адресованы именно нам
    private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");
    // Наименование бюро — проверяется в теле dlput вместе с ОГРН для двойной идентификации получателя
    private readonly string? _ourBureauName = config.GetValue<string>("Bureau:Name");

    // Ключ готового агрегированного XML в Redis-хеше dlput — пишется DlPutServiceV3 по завершении обработки
    private const string DlPutAnswerV3ReadyField = "putanswer_v3_response_xml";
    // Признак существования задачи dlput в Redis — позволяет отличить «нет такого id» от «ещё не готово»
    private const string DlPutAnswerV3ExistsField = "putanswer_v3_exists";
    // Метка первой успешной доставки ответа клиенту — защищает от повторного получения (ошибка 17)
    private const string ResponseDeliveredUtcField = "response_delivered_utc";
    // Префиксы ключей в Redis: scope изолирует данные разных эндпоинтов и версий протокола
    private const string DlRequestV3Scope = "dlrequest:v3";
    private const string DlPutV3Scope = "dlput:v3";
    private const string DlAnswerV3Scope = "dlanswer:v3";
    private const string DlPutAnswerV3Scope = "dlputanswer:v3";
    // Расчётное время готовности ответа (UTC и МСК) — пишется при 202, читается клиентом для планирования polling
    private const string ReadyAtUtcField = "ready_at_utc";
    private const string ReadyAtMskField = "ready_at_msk";
    // Самый ранний допустимый момент первого опроса — не раньше ВремяГотовности, исключает преждевременный polling
    private const string FirstPollAllowedAtUtcField = "first_poll_allowed_at_utc";
    // Время истечения хранения ответа — по истечении ключ в Redis протухает
    private const string ResponseExpireAtUtcField = "response_expire_at_utc";
    // Время последнего опроса клиента — для контроля минимального интервала между повторными запросами
    private const string LastPollUtcField = "last_poll_utc";


    [HttpPost("dlrequest")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlRequest_v_3(ApiVersion apiVersion)
    {
        // Фиксируем метку времени начала — нужна для трассировки жизненного цикла запроса в Redis и журналах
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

        // Десериализация тела запроса, XSD-валидация, проверка подписи, проверка прав доступа и согласия.
        // Handler: CreateToValidateCommandV3 → CreateAndValidateHandlerV3
        // Результат — объект transaction; при ошибках ProcessingErrors заполнен
        var transaction = await _mediator.Send(new CreateToValidateCommandV3(apiVersion, Request));
        _logger.LogDebug("{guid} Request: {dt}", transaction.Id, requestTime);

        if (transaction.ProcessingErrors.Count != 0)
        {
            // Формируем квитанцию ошибки по первому нарушению — клиент получает контрактный XML-ответ v3.
            // TicketServiceV3: CreateResultV3Error — строит Результат с ТипОшибка по коду и тексту
            var errorTicket = _ticketServiceV3.CreateResultV3Error(transaction.ProcessingErrors.First());

            // XmlServiceV3: SerializeAsByteV3 — сериализует доменный объект в байты UTF-8 XML
            var errorResult = _xmlServiceV3.SerializeAsByteV3(errorTicket);

            // CryptoService: SignMsg — подписываем УКЭП нашего бюро, т.к. протокол требует подписанного ответа
            var signedResp = _cryptoService.SignMsg(errorResult);

            // Сохраняем результат внутри transaction для использования downstream-обработчиками
            transaction.Complete(errorResult, signedResp);

            // Публикуем событие завершения для аудита — Handler QBCHProcessingCompleteV3 записывает факт в Redis.
            // Handler: QBCHProcessingCompleteV3 → QBCHProcessingCompleteHandlerV3
            await _mediator.Publish(new QBCHProcessingCompleteV3(transaction));

            // Возвращаем 400 с подписанной квитанцией ошибки
            return BadRequest(new MemoryStream(transaction.Response.SignedTicket!));
        }

        // Запись уникального requestId в Redis — защита от повторной обработки одного и того же запроса.
        // Выполняется в try/catch, чтобы сбой Redis не блокировал обработку: потеря записи не критична
        try
        {
            // Извлекаем ЗапросСведений из transaction — универсальный GetRequest<T> для типизированного доступа к телу
            var request = transaction.GetRequest<ЗапросСведений>();

            var requestId = request?.ИдентификаторЗапроса;
            var requestDate = request?.ДатаЗапроса;
            // ОГРН абонента различается по типу лица: ЮЛ, ИП, иностранное — извлекаем из полиморфного поля Item
            var requestOgrn = request?.Абонент?.Item switch
            {
                АбонентЮЛ юрЛицо => юрЛицо.ОГРН,
                АбонентИП ип => ип.ОГРНИП,
                АбонентИно ино => ино.РегНомер,
                _ => null
            };

            // CacheService: AddUniqueRequestId — фиксирует тройку (id, ОГРН, дата) под scope dlrequest:v3,
            // чтобы повторный запрос с тем же id вернул ошибку 11 (не уникален)
            if (!string.IsNullOrWhiteSpace(requestId) && !string.IsNullOrWhiteSpace(requestOgrn) && requestDate.HasValue)
                await _redisCache.AddUniqueRequestId(DlRequestV3Scope, requestId, requestOgrn, requestDate.Value);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи уникальнго requestId в redis");
        }

        // Останавливаем таймер этапа валидации — метрика для выявления медленных шагов пайплайна
        transaction.TimeElapsedForValidation.Stop();
        _logger.LogDebug("{guid} Validation time elapsed: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

        try
        {
            // Запускаем основной обработчик: параллельные запросы к КБКИ и агрегация ответов.
            // Таймаут берётся из конфигурации — если КБКИ не ответили вовремя, клиент получит квитанцию 202.
            // Handler: QBCHProcessedStartV3 → QBCHProcessingHandlerV3
            var processingResult = await _mediator.Send(new QBCHProcessedStartV3(transaction, _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs"), _ourBureauPSRN ?? string.Empty));

            // Publish выполняется после отправки HTTP-ответа, чтобы не задерживать клиента ожиданием записи в Redis.
            // Handler: QBCHProcessingCompleteV3 → QBCHProcessingCompleteHandlerV3
            Response.OnCompleted(async () => await _mediator.Publish(new QBCHProcessingCompleteV3(processingResult)));

            if (processingResult.Response.SignedTicket is not null && processingResult.Response.TicketXML is not null)
            {
                // Определяем HTTP-статус по содержимому квитанции: ТипОшибка → 400, РезультатИдентификаторОтвета → 202.
                // DetermineTicketStatusCode десериализует XML, чтобы не хранить статус отдельным полем
                var statusCode = DetermineTicketStatusCode(processingResult.Response.TicketXML);
                // Устанавливаем статус явно до вызова File(), т.к. File() по умолчанию ставит 200
                Response.StatusCode = statusCode;
                return File(processingResult.Response.SignedTicket, "application/octet-stream");
            }

            // Готовый ответ — SignedResponse содержит подписанный XML с агрегированными данными по субъекту
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
        // Генерируем GUID сессии — ключ хеша в Redis для аудит-записи этого конкретного вызова dlanswer
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = DlAnswerV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            // Открываем аудит-запись: фиксируем время, thumbprint и IP для последующей аналитики.
            // CacheService: AddHash — все поля пишутся в один Redis-хеш serviceName:guid
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            // Ошибка 3: id обязателен — без него невозможно найти ответ в Redis
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");
                // BuildV3ErrorResponseAsync: единый helper — пишет ошибку в Redis, строит квитанцию через TicketServiceV3,
                // сериализует через XmlServiceV3 и подписывает через CryptoService
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 3,
                    "Запрос не содержит обязательных параметров: id",
                    ResolveDlAnswerStatusCodeByErrorCode(3));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "response_guid", id);

            // Ошибка 22: сертификат не имеет прав на dlrequest/dlanswer.
            // ValidationServiceV3: ValidateRulesV3 — ACL-проверка по thumbprint и имени scope
            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, DlRequestV3Scope))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 22,
                    "Запрос не доступен для абонента",
                    ResolveDlAnswerStatusCodeByErrorCode(22));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Проверяем срок действия и формат клиентского TLS-сертификата.
            // ValidationServiceV3: ValidateCertificateV3 — возвращает TicketV3 с кодом ошибки при провале
            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 16: ключ dlrequest:v3:{id} отсутствует в Redis — id не выдавался или истёк TTL.
            // CacheService: KeyExists — проверяет наличие ключа по составному пути [scope, id]
            if (!await _redisCache.KeyExists([DlRequestV3Scope, id]))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Продлеваем TTL ключа при каждом опросе, чтобы активно используемый ответ не протухал.
            // CacheService: TrySetKeyExpiration — устанавливает срок по правилу ApiV3ContractRules.ResponseRetentionMinutes
            await _redisCache.TrySetKeyExpiration(DlRequestV3Scope, id, _contractRules.ResponseRetentionMinutes);

            var nowUtc = DateTimeOffset.UtcNow;
            // Читаем из Redis первый допустимый момент опроса — записывается при выдаче квитанции 202 с ВремяГотовности
            var firstPollAllowedAtUtc = await GetFirstPollAllowedAtUtcAsync(DlRequestV3Scope, id);

            // Клиент опрашивает раньше ВремяГотовности — нарушение протокола v3.
            // Фиксируем нарушение в Redis-списке polling_violations для аудита, но ответ всё равно отдаём
            if (firstPollAllowedAtUtc.HasValue && nowUtc < firstPollAllowedAtUtc.Value)
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlanswer v3 id={id}. Первый опрос разрешён с {firstPollAllowedAtUtc}, текущее UTC={nowUtc}.", id, firstPollAllowedAtUtc.Value, nowUtc);

                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
                // CacheService: ListSet — пишем запись о нарушении в список для будущей аналитики
                await _redisCache.ListSet([serviceName, "polling_violations", id], $"{nowUtc:O}|{ipAddress ?? "-"}|min_interval={minIntervalSec}s");
                await _redisCache.TrySetKeyExpiration(serviceName, $"polling_violations:{id}", _contractRules.ResponseRetentionMinutes);

                // Возвращаем 202 + ошибку 12 «Ответ не готов» — клиент должен повторить через ≥MinAnswerPollingIntervalSeconds
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 12,
                    "Ответ не готов",
                    ResolveDlAnswerStatusCodeByErrorCode(12));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Проверяем соблюдение минимального интервала между повторными опросами (LastPollUtcField).
            // ApiV3ContractRules: IsAnswerRetryAllowed — сравнивает lastPollUtc и nowUtc с порогом MinAnswerPollingIntervalSeconds
            if (_redisCache.TryGetHashValue(DlRequestV3Scope, id, LastPollUtcField, out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlanswer v3 id={id}. Последний опрос={lastPollUtc}, текущий UTC={nowUtc}, min={interval} сек.", id, lastPollUtc, nowUtc, minIntervalSec);
                // Логируем нарушение, но не блокируем — предупреждение для аудита, не отказ
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlRequestV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            // Обновляем LastPollUtcField — сдвигаем окно минимального интервала на текущий момент
            await _redisCache.AddHash(DlRequestV3Scope, id, LastPollUtcField, nowUtc.ToString("O"));
            await _redisCache.TrySetKeyExpiration(DlRequestV3Scope, id, _contractRules.ResponseRetentionMinutes);

            // Фиксируем время конца валидации — по наличию этого поля в finally определяем, прошли ли проверки
            await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            // Пытаемся получить готовый агрегированный XML из Redis.
            // Ключ qbch_tasks_aggregate_xml пишет QBCHProcessingCompleteHandlerV3 после завершения обработки
            if (_redisCache.TryGetHash(DlRequestV3Scope, id, "qbch_tasks_aggregate_xml", out responseXml))
            {
                // Ошибка 17: ответ уже был получен клиентом — повторная доставка запрещена протоколом v3
                if (await _redisCache.HashFieldExists(DlRequestV3Scope, id, ResponseDeliveredUtcField))
                {
                    var alreadyDeliveredResult = await BuildV3ErrorResponseAsync(
                        serviceName, guid, 17,
                        "Ответ уже получен",
                        ResolveDlAnswerStatusCodeByErrorCode(17));

                    responseXml = alreadyDeliveredResult.ResponseXml;
                    signedResponse = alreadyDeliveredResult.SignedResponse;
                    return alreadyDeliveredResult.ActionResult;
                }

                // Ставим метку доставки до отправки ответа, чтобы конкурентный запрос не получил ответ дважды
                await _redisCache.AddHash(DlRequestV3Scope, id, ResponseDeliveredUtcField, DateTimeOffset.UtcNow.ToString("O"));
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            // Ответ ещё не готов — возвращаем 202 + ошибку 12, клиент повторит опрос через ≥1 сек
            var notReadyResult = await BuildV3ErrorResponseAsync(
                serviceName, guid, 12,
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
            // Сохраняем финальные данные ответа в Redis для полноты аудит-лога
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            // Если validation_date_time не выставлено — валидация не прошла, проставляем факт для аудита
            if (!await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time"))
                await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            // Устанавливаем TTL на аудит-хеш, чтобы он автоматически удалился после истечения срока хранения
            await _redisCache.TrySetKeyExpiration(serviceName, guid, _contractRules.ResponseRetentionMinutes);
        }
    }

    [HttpPost("dlput")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlPut_v_3(ApiVersion apiVersion)
    {
        // Генерируем GUID сессии — ключ аудит-хеша в Redis для этого вызова dlput
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = DlPutV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            // Открываем аудит-запись в Redis: фиксируем время, thumbprint и IP
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            // Ошибка 1: метод не POST — проверка явная, несмотря на атрибут [HttpPost], т.к. требование протокола v3
            if (!string.Equals(Request.Method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 1,
                    "Метод передачи запроса не соответствует ожидаемому",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Читаем всё тело запроса в память для многократной обработки (XSD-валидация, десериализация)
            using var bodyStream = new MemoryStream();
            await Request.Body.CopyToAsync(bodyStream);
            var bodyBytes = bodyStream.ToArray();

            // Ошибка 2: пустое тело — нечего сохранять в БД, сразу отклоняем
            if (bodyBytes.Length == 0)
            {
                _logger.LogError("Пустое тело запроса");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 2,
                    "Тело запроса отсутствует",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Проверяем срок действия и формат TLS-сертификата.
            // ValidationServiceV3: ValidateCertificateV3
            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 8: тело не в UTF-8 — протокол v3 допускает только UTF-8.
            // ValidationServiceV3: ValidateEncodingV3 — проверяет BOM и корректность байтовой последовательности
            if (!_validationServiceV3.ValidateEncodingV3(bodyBytes, out var encodingValidationResult))
            {
                var ticket = encodingValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(8, "Неподдерживаемая кодировка, файл не в кодировке Utf-8"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", encodingValidationResult?.ErrorCode.ToString() ?? "8");
                await _redisCache.AddHash(serviceName, guid, "error_message", encodingValidationResult?.Error ?? "Неподдерживаемая кодировка, файл не в кодировке Utf-8");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 22: сертификат не имеет прав на dlput:v3.
            // ValidationServiceV3: ValidateRulesV3 — ACL-проверка по thumbprint и scope
            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 22,
                    "Запрос не доступен для абонента",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 9: тело не проходит XSD-схему ПредставлениеСведений.
            // ValidationServiceV3: ValidateXmlV3 — валидация по схеме xsd для конкретного scope
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

            // Десериализуем XML в доменный объект ПредставлениеСведений для бизнес-валидации.
            // XmlServiceV3: DeserializeV3<T>
            var requestV3 = _xmlServiceV3.DeserializeV3<ПредставлениеСведений>(bodyBytes);
            if (requestV3 is null)
            {
                _logger.LogError("Не удалось десериализовать тело запроса dlput v3");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 9,
                    "Не удалось прочитать XML запроса",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 20: ОГРН в теле запроса не совпадает с ОГРН нашего бюро — данные направлены не туда
            if (requestV3.БКИ is null || !string.Equals(requestV3.БКИ.ОГРН, _ourBureauPSRN, StringComparison.Ordinal))
            {
                _logger.LogError("ОГРН БКИ в запросе не соответствует ОГРН принимающего бюро");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 20,
                    "ОГРН БКИ в запросе не соответствует ОГРН принимающего бюро",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 21: наименование БКИ не совпадает — двойная идентификация получателя по протоколу v3
            if (!string.IsNullOrWhiteSpace(_ourBureauName) &&
                !string.Equals(requestV3.БКИ.Value, _ourBureauName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Наименование БКИ в запросе не соответствует наименованию принимающего бюро");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 21,
                    "Наименование БКИ в запросе не соответствует наименованию принимающего бюро",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            var entitiesCount = requestV3.Сведения?.Length ?? 0;

            // Ошибка 3: превышен лимит Сведений в одном пакете — защита от перегрузки БД.
            // ApiV3ContractRules: MaxDlPutEntities — лимит задаётся в конфигурации
            if (entitiesCount > _contractRules.MaxDlPutEntities)
            {
                _logger.LogError("Превышен лимит количества элементов Сведения: {count}", entitiesCount);
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, $"Количество элементов Сведения превышает допустимый лимит {_contractRules.MaxDlPutEntities}", StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 23: дата запроса некорректна (в будущем или слишком давно).
            // ValidationServiceV3: ValidateRequestDateV3
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

            // Ошибка 11: ИдентификаторЗапроса уже использовался этим БКИ — защита от двойной загрузки.
            // ValidationServiceV3: IsUniqueRequestIdV3 — проверяет по Redis/БД
            if (!_validationServiceV3.IsUniqueRequestIdV3(requestId, serviceName, requestOgrn, out var uniqueValidationResult))
            {
                var ticket = uniqueValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(11, "Идентификатор запроса не уникален"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Запускаем бизнес-обработку: сохранение Сведений в БД, возможно асинхронно.
            // DlPutServiceV3: ProcessAsync — возвращает IsAccepted=true (202) или готовый результат (200)
            var dlPutResult = await _dlPutServiceV3.ProcessAsync(requestV3);
            if (dlPutResult.IsAccepted)
            {
                // Фиксируем requestId как использованный только после успешного приёма задачи
                await _redisCache.AddUniqueRequestId(serviceName, requestId, requestOgrn, requestV3.ДатаЗапроса);

                if (dlPutResult.AcceptedTicket?.Item is QBCH.Lib.qcb_xml.v3_0.РезультатИдентификаторОтвета acceptedResponseId &&
                    !string.IsNullOrWhiteSpace(acceptedResponseId.ИдентификаторОтвета))
                {
                    var acceptedCreatedAtUtc = DateTimeOffset.UtcNow;
                    // Берём ВремяГотовности из ответа сервиса или дефолт из правил контракта
                    var readyTimeMs = acceptedResponseId.ВремяГотовностиSpecified
                        ? acceptedResponseId.ВремяГотовности
                        : _contractRules.MinAnswerPollingIntervalSeconds * 1000L;

                    var readyAtUtc = acceptedCreatedAtUtc.AddMilliseconds(Math.Max(1, readyTimeMs));
                    // firstPollAllowedAtUtc — не раньше MinAnswerPollingIntervalSeconds; не совпадает с readyAtUtc намеренно
                    var firstPollAllowedAtUtc = acceptedCreatedAtUtc.AddSeconds(_contractRules.MinAnswerPollingIntervalSeconds);
                    var responseExpireAtUtc = acceptedCreatedAtUtc.AddHours(_contractRules.ResponseRetentionHours);

                    // Перезаписываем ВремяГотовности в квитанции: пересчитываем относительно реального момента создания
                    acceptedResponseId.ВремяГотовности = Math.Max(1, (long)(readyAtUtc - acceptedCreatedAtUtc).TotalMilliseconds);
                    acceptedResponseId.ВремяГотовностиSpecified = true;

                    responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.AcceptedTicket);
                    signedResponse = _cryptoService.SignMsg(responseXml);

                    // Записываем метаданные задачи в Redis под ключом ИдентификаторОтвета — нужны для dlputanswer
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
                    // Квитанция без ИдентификаторОтвета — редкий случай, сохраняем без метаданных polling
                    responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.AcceptedTicket);
                    signedResponse = _cryptoService.SignMsg(responseXml);
                }

                // 202 Accepted — данные приняты в обработку, клиент опрашивает dlputanswer
                return Accepted(new MemoryStream(signedResponse));
            }

            // Синхронный результат: данные уже загружены в БД — возвращаем 200 с готовым ответом
            responseXml = _xmlServiceV3.SerializeAsByteV3(dlPutResult.ReadyResult);
            signedResponse = _cryptoService.SignMsg(responseXml);

            await _redisCache.AddUniqueRequestId(serviceName, requestId, requestOgrn, requestV3.ДатаЗапроса);
            // Сохраняем готовый ответ в Redis под ИдентификаторОтвета, чтобы dlputanswer мог его отдать при необходимости
            await _redisCache.AddHash(serviceName, dlPutResult.ReadyResult!.ИдентификаторОтвета, DlPutAnswerV3ReadyField, responseXml);
            await _redisCache.AddHash(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, DlPutAnswerV3ExistsField, "1");
            await _redisCache.TrySetKeyExpiration(serviceName, dlPutResult.ReadyResult.ИдентификаторОтвета, _contractRules.ResponseRetentionMinutes);
            await _redisCache.AddHash(serviceName, guid, "response_guid", dlPutResult.ReadyResult.ИдентификаторОтвета);

            return File(signedResponse, "application/octet-stream");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Количество элементов Сведения", StringComparison.Ordinal))
        {
            // DlPutServiceV3 может выбросить это исключение при нарушении лимита внутри сервиса — обрабатываем явно
            _logger.LogError(ex, "Ошибка бизнес-валидации dlput v3");
            var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, $"Количество элементов Сведения превышает допустимый лимит {_contractRules.MaxDlPutEntities}", StatusCodes.Status400BadRequest);

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
            // Сохраняем финальный ответ в Redis для полноты аудит-лога
            if (signedResponse is not null)
                await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

            if (responseXml is not null)
                await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);

            // Гарантируем наличие validation_date_time даже если запрос упал до успешной валидации
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
        // Генерируем GUID сессии — ключ аудит-хеша в Redis для этого вызова dlputanswer
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        const string serviceName = DlPutAnswerV3Scope;
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        try
        {
            // Открываем аудит-запись в Redis: фиксируем время, thumbprint и IP
            await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
            await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
            await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
            await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

            if (!string.IsNullOrWhiteSpace(ipAddress))
                await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

            // Ошибка 3: id обязателен — без него невозможно найти результат dlput в Redis
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 3,
                    "Запрос не содержит обязательных параметров: id",
                    ResolveDlPutAnswerStatusCodeByErrorCode(3));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "response_guid", id);

            // Проверяем срок действия и формат TLS-сертификата.
            // ValidationServiceV3: ValidateCertificateV3
            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 22: сертификат не имеет прав на dlput:v3/dlputanswer:v3.
            // ValidationServiceV3: ValidateRulesV3 — ACL-проверка по scope dlput:v3
            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, DlPutV3Scope))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 22,
                    "Запрос не доступен для абонента",
                    ResolveDlPutAnswerStatusCodeByErrorCode(22));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 16: ключ dlput:v3:{id} не существует в Redis — id не выдавался или истёк TTL
            if (!await _redisCache.KeyExists([DlPutV3Scope, id]))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlPutAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 16: ключ есть, но DlPutAnswerV3ExistsField не выставлен — задача не была принята dlput корректно.
            // Двойная проверка нужна, т.к. ключ мог появиться из другого источника (гипотетически)
            if (!await _redisCache.HashFieldExists(DlPutV3Scope, id, DlPutAnswerV3ExistsField))
            {
                _logger.LogError("Указан некорректный идентификатор ответа");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 16,
                    "Указан некорректный идентификатор ответа",
                    ResolveDlPutAnswerStatusCodeByErrorCode(16));

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Продлеваем TTL ключа задачи при каждом опросе, чтобы активно ожидаемый ответ не протухал
            await _redisCache.TrySetKeyExpiration(DlPutV3Scope, id, _contractRules.ResponseRetentionMinutes);
            var nowUtc = DateTimeOffset.UtcNow;
            // Читаем первый допустимый момент опроса — записывается в dlput при выдаче 202
            var firstPollAllowedAtUtc = await GetFirstPollAllowedAtUtcAsync(DlPutV3Scope, id);

            // Клиент опрашивает раньше ВремяГотовности — нарушение, фиксируем для аудита, но не блокируем
            if (firstPollAllowedAtUtc.HasValue && nowUtc < firstPollAllowedAtUtc.Value)
            {
                var minIntervalSec = _contractRules.MinAnswerPollingIntervalSeconds;
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Первый опрос разрешён с {firstPollAllowedAtUtc}, текущее UTC={nowUtc}.", id, firstPollAllowedAtUtc.Value, nowUtc);
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            // Проверяем соблюдение минимального интервала между повторными опросами.
            // ApiV3ContractRules: IsAnswerRetryAllowed — порог MinAnswerPollingIntervalSeconds из конфигурации
            if (_redisCache.TryGetHashValue(DlPutV3Scope, id, LastPollUtcField, out var lastPollRaw) &&
                DateTimeOffset.TryParse(lastPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastPollUtc) &&
                !_contractRules.IsAnswerRetryAllowed(lastPollUtc, nowUtc))
            {
                _logger.LogWarning("Нарушение polling-ограничения /dlputanswer v3 id={id}. Последний опрос={lastPollUtc}, текущий UTC={nowUtc}, min={interval} сек.", id, lastPollUtc, nowUtc, _contractRules.MinAnswerPollingIntervalSeconds);
                // Фиксируем нарушение для аудита, не блокируем
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_utc", nowUtc.ToString("O"));
                await _redisCache.AddHash(DlPutV3Scope, id, "polling_violation_ip", ipAddress ?? "-");
            }

            // Обновляем LastPollUtcField — сдвигаем окно минимального интервала на текущий момент
            await _redisCache.AddHash(DlPutV3Scope, id, LastPollUtcField, nowUtc.ToString("O"));
            await _redisCache.TrySetKeyExpiration(DlPutV3Scope, id, _contractRules.ResponseRetentionMinutes);

            // Пытаемся получить готовый XML результата загрузки из Redis.
            // Ключ DlPutAnswerV3ReadyField пишет DlPutServiceV3 по завершении сохранения в БД
            if (_redisCache.TryGetHash(DlPutV3Scope, id, DlPutAnswerV3ReadyField, out responseXml))
            {
                // Ошибка 17: ответ уже был получен клиентом — повторная доставка запрещена протоколом v3
                if (await _redisCache.HashFieldExists(DlPutV3Scope, id, ResponseDeliveredUtcField))
                {
                    var alreadyDeliveredResult = await BuildV3ErrorResponseAsync(
                        serviceName, guid, 17,
                        "Ответ уже получен",
                        ResolveDlPutAnswerStatusCodeByErrorCode(17));

                    responseXml = alreadyDeliveredResult.ResponseXml;
                    signedResponse = alreadyDeliveredResult.SignedResponse;
                    return alreadyDeliveredResult.ActionResult;
                }

                // Ставим метку первой доставки — защита от race condition при параллельных запросах
                await _redisCache.AddHash(DlPutV3Scope, id, ResponseDeliveredUtcField, DateTimeOffset.UtcNow.ToString("O"));
                signedResponse = _cryptoService.SignMsg(responseXml);
                return File(signedResponse, "application/octet-stream");
            }

            // Ответ ещё не готов — 202 + ошибка 12, клиент повторит через ≥1 сек
            var notReadyResult = await BuildV3ErrorResponseAsync(
                serviceName, guid, 12,
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
            // Сохраняем финальный ответ в Redis для полноты аудит-лога
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
        // Генерируем GUID сессии — ключ аудит-хеша в Redis для этого вызова certadd
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var guid = Guid.NewGuid().ToString();
        var serviceName = "certadd";
        var certificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

        byte[]? responseXml = null;
        byte[]? signedResponse = null;

        // Открываем аудит-запись сразу, до try-блока — чтобы зафиксировать даже самые ранние ошибки
        await _redisCache.AddHash(serviceName, guid, "request_date_time", requestTime);
        await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);
        await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
        // Сохраняем RawData (DER) сертификата для возможного ретроспективного анализа
        await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.RawData ?? Array.Empty<byte>());

        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            // Ошибка 1: метод не POST — явная проверка по требованию протокола v3
            if (!string.Equals(Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 1,
                    "Метод передачи запроса не соответствует ожидаемому",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 22: TLS-сертификат не имеет прав на certadd.
            // ValidationServiceV3: ValidateRulesV3 — ACL по thumbprint и имени сервиса
            if (!await _validationServiceV3.ValidateRulesV3(certificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 22,
                    "Запрос не доступен для абонента",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Проверяем срок действия и формат TLS-сертификата.
            // ValidationServiceV3: ValidateCertificateV3
            if (!_validationServiceV3.ValidateCertificateV3(certificate, out var certValidationResult))
            {
                var ticket = certValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(5, "Ошибка проверки сертификата"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", certValidationResult?.ErrorCode.ToString() ?? "5");
                await _redisCache.AddHash(serviceName, guid, "error_message", certValidationResult?.Error ?? "Ошибка проверки сертификата");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 3: id обязателен — идентифицирует запрос для проверки идемпотентности
            if (string.IsNullOrWhiteSpace(form.id))
            {
                _logger.LogError("Запрос не содержит обязательных параметров: id");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 3,
                    "Запрос не содержит обязательных параметров: id",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 3: sign обязателен — без подписи невозможно верифицировать подлинность cert
            if (form.sign is null)
            {
                _logger.LogError("Запрос не содержит обязательных параметров: sign");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 3,
                    "Запрос не содержит обязательных параметров: sign",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 3: cert обязателен — это и есть добавляемый сертификат
            if (form.cert is null)
            {
                _logger.LogError("Запрос не содержит обязательных параметров: cert");
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 3,
                    "Запрос не содержит обязательных параметров: cert",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "request_id", form.id);

            // Читаем бинарные данные cert и sign из multipart-формы в память для обработки
            var signStream = new MemoryStream();
            var certStream = new MemoryStream();

            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certBytes = certStream.ToArray(); // DER-формат
            var signBytes = signStream.ToArray();

            // Сохраняем сырые байты для аудита — позволяет восстановить сертификат при разборе инцидентов
            await _redisCache.AddHash(serviceName, guid, "cert", certBytes);
            await _redisCache.AddHash(serviceName, guid, "sign", signBytes);

            // Извлекаем thumbprint из тела сертификата для аудита — отдельно от TLS-thumbprint
            try
            {
                X509Certificate2 certFromForm = new(certBytes);
                await _redisCache.AddHash(serviceName, guid, "cert_thumbprint", certFromForm.Thumbprint ?? "-");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{serviceName} не удалось считать thumbprint сертификата из формы", serviceName);
            }

            // Проверяем, что cert подписан именно TLS-сертификатом запроса (владелец подтверждает добавление).
            // CryptoService: ValidateMsg — верифицирует УКЭП sign как подпись cert, выполненную certificate
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

            // Ошибка 99: из подписи не удалось извлечь ОГРН — без него невозможно привязать сертификат к абоненту
            if (string.IsNullOrWhiteSpace(validateMsgResult.RequestOGRN))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 99,
                    "Не удалось определить ОГРН абонента по действующему сертификату",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 11: id запроса уже использовался — защита от повторного добавления одного сертификата.
            // ValidationServiceV3: IsUniqueRequestIdV3
            if (!_validationServiceV3.IsUniqueRequestIdV3(form.id, serviceName, validateMsgResult.RequestOGRN, out var uniqueValidationResult))
            {
                var ticket = uniqueValidationResult?.TicketV3 ?? _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(11, "Идентификатор запроса не уникален"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);

                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult?.ErrorCode.ToString() ?? "11");
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult?.Error ?? "Идентификатор запроса не уникален");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Фиксируем id как использованный после успешной проверки подписи — момент записи важен для идемпотентности
            await _redisCache.AddUniqueRequestId(serviceName, form.id, validateMsgResult.RequestOGRN, DateTime.Now);

            // Ошибка 99: сертификат с таким thumbprint уже зарегистрирован — дубликат не допускается.
            // ValidationServiceV3: IsCertExistsV3 — проверяет по БД через RepositoryV3
            if (await _validationServiceV3.IsCertExistsV3(certBytes))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 99,
                    "Такой сертификат уже существует.",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Фиксируем время конца валидации для корректного аудита
            await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            // Сохраняем сертификат в БД и привязываем к абоненту по ОГРН.
            // CertManagementService: AddCertificate — записывает в таблицу активных сертификатов
            if (!await _certManagement.AddCertificate(certBytes, validateMsgResult.RequestOGRN, guid))
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 99,
                    "Не удалось добавить сертификат.",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // TicketServiceV3: CreateResultV3Success — квитанция успешного добавления с датой для аудита
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
            // Сохраняем финальный ответ в Redis для полноты аудит-лога
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
        // Генерируем GUID сессии — ключ аудит-хеша в Redis для этого вызова certrevoke
        var guid = Guid.NewGuid().ToString();
        const string serviceName = "certrevoke";

        // Открываем аудит-запись сразу — фиксируем данные до любых проверок
        await _redisCache.AddHash(serviceName, guid, "request_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);

        var requestCertificate = Request.HttpContext.Connection.ClientCertificate;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", requestCertificate?.Thumbprint ?? "-");
        // Сохраняем RawData (DER) для ретроспективного анализа при разборе инцидентов
        await _redisCache.AddHash(serviceName, guid, "request_certificate_data", requestCertificate?.RawData ?? Encoding.UTF8.GetBytes("-"));
        if (!string.IsNullOrWhiteSpace(ipAddress))
            await _redisCache.AddHash(serviceName, guid, "ip_address", ipAddress);

        try
        {
            // Ошибка 22: TLS-сертификат не имеет прав на certrevoke.
            // ValidationServiceV3: ValidateRulesV3 — ACL по thumbprint и имени сервиса
            if (!await _validationServiceV3.ValidateRulesV3(requestCertificate?.Thumbprint, serviceName))
            {
                _logger.LogError("Запрос не доступен для абонента");
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 22, "Запрос не доступен для абонента", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 3: id обязателен — нужен для проверки идемпотентности отзыва
            if (string.IsNullOrWhiteSpace(form.id))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: id", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 3: sign обязателен — подтверждает, что отзыв инициирован владельцем сертификата
            if (form.sign is null)
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: sign", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 3: cert обязателен — это отзываемый сертификат
            if (form.cert is null)
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 3, "Запрос не содержит обязательных параметров: cert", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            await _redisCache.AddHash(serviceName, guid, "request_guid", form.id);

            // Читаем cert и sign из multipart-формы в память; using await для корректной утилизации стримов
            await using var certStream = new MemoryStream();
            await using var signStream = new MemoryStream();
            await form.cert.CopyToAsync(certStream);
            await form.sign.CopyToAsync(signStream);

            var certRaw = certStream.ToArray();
            var signRaw = signStream.ToArray();
            // Сохраняем сырые байты для аудита
            await _redisCache.AddHash(serviceName, guid, "request_certificate_to_revoke", certRaw);
            await _redisCache.AddHash(serviceName, guid, "request_sign", signRaw);

            // Парсим cert как DER для извлечения thumbprint — отдельная попытка с явной ошибкой 7 при провале
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

            // Проверяем УКЭП: cert подписан sign с использованием TLS-сертификата запроса.
            // CryptoService: ValidateMsg — верифицирует подпись и извлекает ОГРН подписанта (SignThumbprint, RequestOGRN)
            if (!_cryptoService.ValidateMsg(certRaw, requestCertificate, out var cryptoResult, signRaw))
            {
                var ticket = _ticketServiceV3.CreateResultV3Error(new QBCH_lib.core.Error(cryptoResult.ErrorCode, cryptoResult.Error ?? "УЭП некорректна"));
                responseXml = _xmlServiceV3.SerializeAsByteV3(ticket);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", cryptoResult.ErrorCode.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", cryptoResult.Error ?? "-");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Ошибка 99: сертификат, которым подписан запрос, сам является неактивным.
            // ValidationServiceV3: IsCertActiveV3 — проверяет статус сертификата подписи в БД через RepositoryV3
            if (!await _validationServiceV3.IsCertActiveV3(cryptoResult.SignThumbprint ?? string.Empty))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Сертификат подписи не является действующим", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 11: id запроса уже использовался этим абонентом — защита от повторного отзыва.
            // ValidationServiceV3: IsUniqueRequestIdV3
            if (!_validationServiceV3.IsUniqueRequestIdV3(form.id, serviceName, cryptoResult.RequestOGRN ?? string.Empty, out var uniqueResult))
            {
                responseXml = _xmlServiceV3.SerializeAsByteV3(uniqueResult!.TicketV3!);
                signedResponse = _cryptoService.SignMsg(responseXml);
                await _redisCache.AddHash(serviceName, guid, "error_code", uniqueResult.ErrorCode.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", uniqueResult.Error ?? "-");
                return BadRequest(new MemoryStream(signedResponse));
            }

            // Фиксируем id как использованный после успешной проверки подписи
            await _redisCache.AddUniqueRequestId(serviceName, form.id, cryptoResult.RequestOGRN!, DateTime.Now);

            // Ошибка 99: отзываемый сертификат не найден в БД — нечего деактивировать.
            // ValidationServiceV3: IsCertExistsV3 — проверяет наличие активной записи через RepositoryV3
            if (!await _validationServiceV3.IsCertExistsV3(certRaw))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Сертификат не найден", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Ошибка 99: новое ограничение протокола v3 — нельзя отозвать последний активный сертификат абонента.
            // Без хотя бы одного активного сертификата абонент потеряет доступ к API навсегда.
            // ValidationServiceV3: GetActiveCertificatesCountV3 — считает активные записи по ОГРН через RepositoryV3
            var activeCertsCount = await _validationServiceV3.GetActiveCertificatesCountV3(certRaw);
            if (activeCertsCount <= 1)
            {
                var errorResult = await BuildV3ErrorResponseAsync(
                    serviceName, guid, 99,
                    "Отзыв последнего действующего сертификата абонента запрещен Порядком 3.0",
                    StatusCodes.Status400BadRequest);

                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // Деактивируем сертификат в БД без физического удаления — сохраняем историю для аудита.
            // ValidationServiceV3: SetCertificateInactiveV3 — помечает запись неактивной через RepositoryV3
            if (!await _validationServiceV3.SetCertificateInactiveV3(certRaw))
            {
                var errorResult = await BuildV3ErrorResponseAsync(serviceName, guid, 99, "Не удалось отозвать сертификат", StatusCodes.Status400BadRequest);
                responseXml = errorResult.ResponseXml;
                signedResponse = errorResult.SignedResponse;
                return errorResult.ActionResult;
            }

            // TicketServiceV3: CreateResultV3Success — квитанция успешного отзыва с датой
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
            // Сохраняем финальный ответ в Redis для полноты аудит-лога
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

    // Вспомогательный DTO для единообразного возврата ошибки из BuildV3ErrorResponseAsync
    private sealed class V3ErrorResponseBuildResult
    {
        public byte[] ResponseXml { get; init; } = Array.Empty<byte>();
        public byte[] SignedResponse { get; init; } = Array.Empty<byte>();
        public IActionResult ActionResult { get; init; } = default!;
    }

    // Единый helper для формирования ошибочного ответа: избавляет от дублирования кода в каждом методе.
    // Пишет ошибку в Redis, строит квитанцию через TicketServiceV3, сериализует через XmlServiceV3,
    // подписывает через CryptoService и устанавливает HTTP-статус
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

        // Устанавливаем статус до вызова File(), чтобы итоговый HTTP-статус совпадал с логикой контракта
        Response.StatusCode = statusCode;

        return new V3ErrorResponseBuildResult
        {
            ResponseXml = responseXml,
            SignedResponse = signedResponse,
            ActionResult = File(signedResponse, "application/octet-stream")
        };
    }

    // Определяет HTTP-статус по типу элемента внутри квитанции, избегая хранения статуса как отдельного поля.
    // XmlServiceV3: DeserializeV3<Результат> — десериализует XML-байты в доменный объект для анализа типа Item
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

    // Преобразует код ошибки dlanswer в HTTP-статус: 12 (не готово) → 202, остальные → 400
    private static int ResolveDlAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    // Преобразует код ошибки dlputanswer в HTTP-статус: 12 (не готово) → 202, остальные → 400
    private static int ResolveDlPutAnswerStatusCodeByErrorCode(int errorCode) =>
        errorCode == 12 ? StatusCodes.Status202Accepted : StatusCodes.Status400BadRequest;

    // Читает из Redis первый допустимый момент опроса для конкретного ответа.
    // Сначала ищет прямое поле FirstPollAllowedAtUtcField; если отсутствует — берёт ReadyAtUtcField и кэширует его.
    // CacheService: TryGetHashValue — синхронное чтение одного поля хеша без блокировки
    private async Task<DateTimeOffset?> GetFirstPollAllowedAtUtcAsync(string scope, string responseId)
    {
        if (_redisCache.TryGetHashValue(scope, responseId, FirstPollAllowedAtUtcField, out var firstPollRaw) &&
            DateTimeOffset.TryParse(firstPollRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var firstPollAllowedAtUtc))
        {
            return firstPollAllowedAtUtc;
        }

        // Фоллбэк: если firstPollAllowedAtUtc не записан — используем readyAtUtc и сохраняем для последующих вызовов
        if (_redisCache.TryGetHashValue(scope, responseId, ReadyAtUtcField, out var readyAtRaw) &&
            DateTimeOffset.TryParse(readyAtRaw?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var readyAtUtc))
        {
            await _redisCache.AddHash(scope, responseId, FirstPollAllowedAtUtcField, readyAtUtc.ToString("O"));
            return readyAtUtc;
        }

        return null;
    }
}
