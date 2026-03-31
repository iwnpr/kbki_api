using Asp.Versioning;
using Cache_lib.Interfaces;
using CertManagement.Services.Interfaces;
using Common_lib;
using Confluent.Kafka;
using Crypto_lib.Service;
using KafkaService_lib.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using QBCH_api.Services.Interfaces;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.Enums;
using QBCH_lib.qcb_xml.v1_3.qcb_answer;
using QBCH_lib.qcb_xml.v1_3.qcb_request;
using QBCH_lib.Services.Interfaces;
using QBCH_lib.upload_xml;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using IXmlService = XmlService_lib.Services.Interfaces.IXmlService;
using ResultType = QBCH_lib.qcb_xml.v1_3.Enums.ResultType;

namespace QBCH_api.Controllers
{
    /// <summary>
    /// Контроллер QBCH
    /// </summary>
    [ApiController]
    [ApiVersion("1.3", Deprecated = true)]
    [Route("v{version:apiVersion}")]
    public class QbchController : Controller
    {
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<QbchController> _logger;
        private readonly IXmlService _xmlService;
        private readonly IValidationService _validationService;
        private readonly IQBCHService _qBCHService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBKIRequisitsHandler _BKIRequisits;
        private readonly ICacheService _redisCache;
        private readonly ITransformer _transformer;
        private readonly IKafkaService _kafka;
        private readonly ITicketService _ticketService;
        private readonly IRepository _repository;
        private readonly ICertManagementService _certManagement;
        private readonly string? OurBureauPSRN;
        private readonly string? _kafkaTopic;
        private const string _depricated = "Версия API 1.3 устарела и больше не поддерживается. (API version 1.3 has been deprecated and is no longer supported.)";

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="cryptoService">Сервис криптографии</param>
        /// <param name="xmlService">Сервис для работы с xml</param>
        /// <param name="logger">Логирование</param>
        /// <param name="validationService">Сервис валидации</param>
        /// <param name="qBCHService"></param>
        /// <param name="config"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="bKIRequisits"></param>
        /// <param name="redisСache"></param>
        /// <param name="transformer"></param>
        /// <param name="kafka"></param>
        /// <param name="ticketService"></param>
        /// <param name="repository"></param>
        /// <param name="certManagement"></param>
        public QbchController(ICryptoService cryptoService,
                              ILogger<QbchController> logger,
                              IXmlService xmlService,
                              IValidationService validationService,
                              IQBCHService qBCHService,
                              IConfiguration config,
                              IHttpClientFactory httpClientFactory,
                              IBKIRequisitsHandler bKIRequisits,
                              ICacheService redisСache,
                              ITransformer transformer,
                              IKafkaService kafka,
                              ITicketService ticketService,
                              IRepository repository,
                              ICertManagementService certManagement)
        {
            _cryptoService = cryptoService;
            _logger = logger;
            _xmlService = xmlService;
            _validationService = validationService;
            _qBCHService = qBCHService;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _BKIRequisits = bKIRequisits;
            _redisCache = redisСache;
            _transformer = transformer;
            _kafka = kafka;
            _ticketService = ticketService;
            _repository = repository;
            _certManagement = certManagement;
            OurBureauPSRN = _config.GetValue<string>("Bureau:PSRN");
            _kafkaTopic = config.GetValue<string>("Old:KafkaService:Topic");
        }

        /// <summary>
        /// Запрос сведений о среднемесячных платежах Субъекта.
        /// </summary>
        /// <remarks>
        /// 
        /// Для снижения нагрузки в квитанцию, содержащую идентификатор ответа, сервер может поместить атрибут «ВремяГотовности», указав в качестве значения время(в миллисекундах), требующееся серверу на подготовку ответа.
        /// При наличии атрибута «ВремяГотовности» клиент должен обращаться за получением сведений о среднемесячных платежах Субъекта не ранее, чем по истечении времени, указанного в атрибуте.
        ///
        /// </remarks>
        /// <response code="200">Результат запроса содержит сведения о среднемесячных платежах Субъекта.</response>
        /// <response code="202">Результат запроса содержит квитанцию с идентификатором ответа.</response>
        /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке.</response>
        [HttpPost("dlrequest")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> DlRequest_v_1_3(ApiVersion apiVersion, IFeatureManager manager)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            // Считем время затраченное на проверки.
            var Timer = Stopwatch.StartNew();
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            byte[]? signedRequest = null;
            byte[]? signedResponse = null;
            byte[]? signedTicket = null;
            byte[]? ResponseXml = null;
            byte[]? TicketXml = null;
            byte[]? requestBody = null;
            string serviceName = "dlrequest";
            string guid = Guid.NewGuid().ToString();
            string? ErrorCode = null;
            string? ErrorMessage = null;
            string? ValidationTime = null;
            string? responseTime = null;
            string? requestId = null;
            string? requestType = null;
            string? IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var certificate = Request.HttpContext.Connection.ClientCertificate;
            ЗапросСведенийОПлатежах? request = null;

            // Выполняется после ответа на запрос в контроллере
            Response.OnCompleted(async () =>
            {
                var key = $"QBCH:{serviceName}:{guid}";
                responseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

                // Сохранение в REDIS
                try
                {
                    var dict = new Dictionary<string, byte[]>
                    {
                        // Создаем объект, который будет логировать все что происходит с запросом
                        { "RequestTime", Encoding.UTF8.GetBytes(RequestTime) },
                        // Сертификаты.
                        { "Thumbprint", Encoding.UTF8.GetBytes(certificate?.Thumbprint ?? "-") },
                        { "ResponseTime", Encoding.UTF8.GetBytes(responseTime) }
                    };

                    // Ip адрес
                    if (!string.IsNullOrWhiteSpace(IpAddress))
                        dict.Add("IpAddress", Encoding.UTF8.GetBytes(IpAddress));

                    // Код ошибки
                    if (!string.IsNullOrWhiteSpace(ErrorCode))
                        dict.Add("ErrorCode", Encoding.UTF8.GetBytes(ErrorCode));

                    // Текст ошибки
                    if (!string.IsNullOrWhiteSpace(ErrorMessage))
                        dict.Add("ErrorMessage", Encoding.UTF8.GetBytes(ErrorMessage));

                    // Подписанное тело запроса
                    if (signedRequest is not null)
                        dict.Add("SignedRequest", signedRequest);

                    // Тело запроса без подписи
                    if (requestBody is not null)
                        dict.Add("request", requestBody);

                    // Идентфикатор запроса request id
                    if (!string.IsNullOrWhiteSpace(requestId))
                        dict.Add("RequestId", Encoding.UTF8.GetBytes(requestId));

                    // Тип запроса, в одно окно/не в одно окно
                    if (!string.IsNullOrWhiteSpace(requestType))
                        dict.Add("RequestType", Encoding.UTF8.GetBytes(requestType));

                    if (signedTicket is not null && TicketXml is not null)
                    {
                        dict.Add("SignedResponse", signedTicket);
                        dict.Add("ResponseXml", TicketXml);
                    }
                    else
                    {
                        if (signedResponse is not null)
                            dict.Add("SignedResponse", signedResponse);

                        if (ResponseXml is not null)
                            dict.Add("ResponseXml", ResponseXml);
                    }

                    /* Если время окончания валидации не существует, значит проверка не пройдена и результат таски возвращен не будет
                     */
                    if (!(await _redisCache.HashFieldExists(serviceName, guid, "ValidationTime")))
                        dict.Add("ValidationTime", Encoding.UTF8.GetBytes(ValidationTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff")));

                    await _redisCache.AddHashArray(serviceName, guid, dict);

                    // Попытка отправки в кафку                    
                    if (!await _kafka.Produce(new Message<Null, string> { Value = key }, _kafkaTopic))
                        _logger.LogCritical("Lost key:{key}", key);
                }
                catch (Exception ex)
                {
                    var message = JsonSerializer.Serialize(new
                    {
                        ServiceName = serviceName,
                        guid,
                        IpAddress = IpAddress?.ToString(),
                        certificate?.Thumbprint,
                        RequestTime,
                        ResponseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                        ex.Message
                    });
                    _logger.LogCritical(ex, "Критическая ошибка при сохранении в redis");

                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine(JsonSerializer.Serialize(new
                        {
                            RequestTime = RequestTime,
                            IpAddress = IpAddress,
                            Thumbprint = certificate?.Thumbprint,
                            ErrorCode = ErrorCode,
                            ErrorMessage = ErrorMessage,
                            SignedRequest = signedRequest,
                            request = requestBody,
                            RequestId = requestId,
                            RequestType = requestType,
                            SignedResponse_Ticket = signedTicket,
                            ResponseXml_Ticket = TicketXml,
                            SignedResponse = signedResponse,
                            ResponseXml = ResponseXml,
                            ValidationTime = ValidationTime,
                            ResponseTime = responseTime
                        }));
                        Directory.CreateDirectory("backup");
                        await System.IO.File.WriteAllTextAsync(Path.Combine("backup", $"{guid}.json"), sb.ToString());
                    }
                    catch
                    {
                        _logger.LogCritical(ex, "Критическая ошибка при сохранении в файлик {guid}", guid);
                    }
                }
            });

            try
            {
                // Файл запроса.
                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                /*  2. Запрос не содержит данных.
                 *  Проверяется длинна массива контента
                 */
                if (ms.Length == 0)
                {
                    ErrorCode = "2";
                    ErrorMessage = "Запрос не содержит данных";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, ErrorCode, ErrorMessage));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }
                signedRequest = ms.ToArray();

                var stopwatch = Stopwatch.StartNew();

                /* Обрабокта подписи файла
                 * Проверка подиси
                 * Снятие подписи
                 */
                if (!_validationService.ValidateMsg(ms.ToArray(), certificate, out var CryptoServiceResult))
                {
                    ErrorCode = CryptoServiceResult.ErrorCode.ToString();
                    ErrorMessage = CryptoServiceResult.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /* 9. Запрос не соответствует схеме
                 * Запрос не соответствует XSD-схеме запроса.
                 * В описании ошибки должна быть включена
                 * информация о том, почему запрос не
                 * соответствует схеме 
                 */
                if (!_validationService.ValidateXml(new MemoryStream(CryptoServiceResult.Body), serviceName, apiVersion, out BaseResult? xsdValidation))
                {
                    ErrorCode = xsdValidation.ErrorCode.ToString();
                    ErrorMessage = xsdValidation.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(xsdValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                try
                {
                    request = _xmlService.Deserialize<ЗапросСведенийОПлатежах>(CryptoServiceResult.Body);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка десериализации");
                    ErrorCode = "500";
                    ErrorMessage = ex.ToString();
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "9", "Запрос не соответствует схеме"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                if (request is null)
                {
                    _logger.LogCritical("Ошибка десериализации");
                    ErrorCode = "500";
                    ErrorMessage = "Ошибка десериализации";
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "9", "Запрос не соответствует схеме"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                requestBody = CryptoServiceResult.Body;
                requestId = request.ИдентификаторЗапроса;
                requestType = StaticHelpers.GetEnumDescription(request.ТипЗапроса);
                List<QBCHRequisite> QBCHList = _BKIRequisits.GetBureaList();
                bool IsAlarm = false;

                try
                {
                    // Проверка прав доступа
                    if (!await _validationService.ValidateRules(certificate?.Thumbprint, serviceName))
                    {
                        ErrorCode = "22";
                        ErrorMessage = "Запрос не доступен для абонента";
                        _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, ErrorCode, ErrorMessage));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }
                }
                catch
                {
                    if (certificate?.Thumbprint is null || !QBCHList.Any(x => x.Thumbprint == certificate.Thumbprint))
                    {
                        ErrorCode = "22";
                        ErrorMessage = "Запрос не доступен для абонента";
                        _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, ErrorCode, ErrorMessage));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    IsAlarm = true;
                }

                // Таймауты
                var QBCHTimeout = _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs");
                var TicketTimeout = _config.GetValue<int>("APIConfiguration:TicketTimeoutMs");
                var ResponseTimeout = _config.GetValue<int>("APIConfiguration:ResponseTimeoutMs");

                /*  1. Метод передачи запроса  не соответствует требуемому.
                 *  Проверяется тип метода который пришел в api 
                 */
                if (Request.Method != "POST")
                {
                    ErrorCode = "1";
                    ErrorMessage = "Метод передачи запроса не соответствует ожидаемому";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, ErrorCode, ErrorMessage));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }
                bool IsQBCH = request.ТипЗапроса == ЗапросСведенийОПлатежахТипЗапроса.All;

                /* 14. Взаимодействие с абонентом 
                 * в режиме «одно окно» 
                 * не предусмотрено договором
                 */
                if (IsQBCH)
                {
                    if (!_validationService.ValidateQBCH(request.Абонент?.Requisites?.ogrn, QBCHList, out BaseResult? QBCHValiadationResult))
                    {
                        _logger.LogError("Пользовательская ошибка {Error}", QBCHValiadationResult.Error);
                        _logger.LogError(QBCHValiadationResult.Error ?? "-");
                        ErrorCode = QBCHValiadationResult.ErrorCode.ToString();
                        ErrorMessage = QBCHValiadationResult.Error ?? "-";
                        ResponseXml = _xmlService.SerializeAsByte(QBCHValiadationResult.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }
                }

                if (IsAlarm)
                    throw new Exception("Ошибка чтения прав из БД и Redis. В ответ будет отдана заглушка.");

                /* 11. Идентификатор запроса не уникален
                 * Идентификатор запроса ранее передавался
                 * данным абонентом в составе другого запроса
                 * такого же типа
                 */
                if (!_validationService.IsUniqueRequestId(request.ИдентификаторЗапроса!, serviceName, request.Абонент!.Requisites!.ogrn!, out BaseResult? uniqueValidationResult))
                {
                    ErrorCode = uniqueValidationResult.ErrorCode.ToString();
                    ErrorMessage = uniqueValidationResult.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(uniqueValidationResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /* 23. Дата запроса указана некорректно
                 * В атрибуте «Дата» блока «Запрос» запроса
                 * dlrequest указана дата, не являющаяся текущей
                 */
                if (!_validationService.ValidateRequestDate(request.Запрос?.Дата, out BaseResult? dateValidation))
                {
                    ErrorCode = dateValidation.ErrorCode.ToString();
                    ErrorMessage = dateValidation.Error;
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(dateValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /* 15. Запрос содержит ошибочные данные, не
                 * выявляющиеся XSD схемой запроса, например,
                 * дата выдачи ДУЛ более ранняя, чем дата
                 * рождения. Описание ошибки должно включать
                 * конкретную причину возникновения ошибки
                 */
                if (!_validationService.AdditionalValidation(request, out BaseResult? additionalValidation))
                {
                    ErrorCode = additionalValidation.ErrorCode.ToString();
                    ErrorMessage = additionalValidation.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(additionalValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /* 13. Отсутствует действующее согласие Субъекта
                 * Указанная дата выдачи согласия плюс срок
                 * действия согласия более ранняя, чем текущая
                 * дата; реквизиты лица, указанные в блоке
                 * «Выдано», не соответствуют реквизитам лица,
                 * указанным в блоке «Источник»; одна или
                 * несколько целей, указанных в блоке «Запрос»
                 * отсутствует, в блоке «Согласие» и др.
                 */
                if (!_validationService.ValidateAgreement(request, out BaseResult? agreementValidation))
                {
                    ErrorCode = agreementValidation.ErrorCode.ToString();
                    ErrorMessage = agreementValidation.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(agreementValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /* Сравнение реквизитов запроса с данными в сертификате.
                 */
                if (!_validationService.AbonentValidation(Request.HttpContext.Connection.ClientCertificate?.Thumbprint, request.Абонент.Requisites.inn, request.Абонент.Requisites.ogrn, out var abonentValidation))
                {
                    ErrorCode = abonentValidation.ErrorCode.ToString();
                    ErrorMessage = abonentValidation.Error ?? "-";
                    _logger.LogError("Пользовательская ошибка: {Error}", ErrorMessage);
                    ResponseXml = _xmlService.SerializeAsByte(abonentValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                try
                {
                    // Дата не может быть пустой т.к. явялется обязательной по xsd, так же как id запроса и инн огрн в реквизитах.
                    await _redisCache.AddUniqueRequestId(serviceName, request.ИдентификаторЗапроса!, request.Абонент!.Requisites!.ogrn!, request.Запрос!.Дата);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка записи уникальнго requestId в redis");
                }

                // Время окончания валидации
                ValidationTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
                ConcurrentBag<Task<QBCHTaskResult>> TaskList = [];

                try
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            if (IsQBCH)
                            {
                                // Запрос ССП в БД
                                TaskList.Add(_qBCHService.AmpFromDB(request, guid));

                                // Запросы в КБКИ
                                foreach (var _qbchTask in QBCHList)
                                {
                                    TaskList.Add(_qBCHService.AmpRequest(guid, request, _httpClientFactory.CreateClient(_qbchTask.Name), QBCHTimeout - Timer.ElapsedMilliseconds, _qbchTask));
                                }

                                // Ждем таски
                                var result = await Task.WhenAll(TaskList);
                                СведенияОПлатежах response = new()
                                {
                                    Версия = "1.2",
                                    ИдентификаторЗапроса = requestId,
                                    ИдентификаторОтвета = guid,
                                    ОГРН = OurBureauPSRN,
                                    ТипОтвета = "2",
                                    ТитульнаяЧасть = request.Запрос?.Субъект
                                };

                                // Разбор ответов
                                foreach (var item in result)
                                {
                                    await _redisCache.AddHash("dlrequest", $"{guid}:{item.BureauPSRN}", "ResponseXml", _xmlService.SerializeAsString(item.Answer));

                                    if (item.Answer?.КБКИ != null)
                                    {
                                        response.КБКИ ??= [];
                                        response.КБКИ.AddRange(item.Answer.КБКИ);
                                    }
                                }

                                ResponseXml = _xmlService.SerializeAsByte(response);
                                await _redisCache.AddHash(serviceName, guid, "response", ResponseXml);
                            }
                            else
                            {
                                // Запрос сведений из БД бюро.
                                var dbresult = await _qBCHService.AmpFromDB(request, guid);
                                ResponseXml = _xmlService.SerializeAsByte(dbresult.Answer);
                                await _redisCache.AddHash(serviceName, guid, "response", ResponseXml);
                                await _redisCache.AddHash("dlrequest", $"{guid}:{dbresult.BureauPSRN}", "ResponseXml", _xmlService.SerializeAsString(dbresult.Answer));
                            }

                            // Время окончания выполнения таска
                            await _redisCache.AddHash(serviceName, guid, "QBCHTotalTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "QBCH exception");

                            await _redisCache.AddHash(serviceName, guid, "ErrorCode", "99");
                            await _redisCache.AddHash(serviceName, guid, "QBCHError", ex.Message);
                            await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.Message);
                            await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        }
                    }).Wait(TimeSpan.FromMilliseconds(TicketTimeout - Timer.ElapsedMilliseconds));

                    // Если по всем задачам мы успели получить ответ, просто возвращаем ответ.
                    if (task)
                    {
                        if (_redisCache.TryGetHash(serviceName, guid, "response", out ResponseXml))
                        {
                            signedResponse = _cryptoService.SignMsg(ResponseXml);
                            return File(signedResponse, "application/octet-stream");
                        }
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _logger.LogError(ex, "Ошибка времени ожидания выполнения запроса. Время проверки превысило {TicketTimeout} миллисекунд.", TicketTimeout);
                    ErrorMessage = $"Ошибка времени ожидания выполнения запроса. Время проверки превысило {TicketTimeout} миллисекунд.";
                }

                ErrorCode = "12";
                TicketXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Ticket, requestId: request.ИдентификаторЗапроса, guid: guid));
                signedTicket = _cryptoService.SignMsg(TicketXml);
                return Accepted(new MemoryStream(signedTicket));
            }
            catch (Exception ex)
            {
                // НАДО ВЕРНУТЬ ОТВЕТ
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                ErrorCode = "500";
                ErrorMessage = ex.ToString();

                if (request is null)
                {
                    _logger.LogCritical("Ошибка десериализации");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "9", "Запрос не соответствует схеме"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }
                else if (request.ТипЗапроса == ЗапросСведенийОПлатежахТипЗапроса.OurBureau)
                {
                    СведенияОПлатежах response = new()
                    {
                        Версия = "1.2",
                        ИдентификаторЗапроса = requestId,
                        ИдентификаторОтвета = guid,
                        ОГРН = OurBureauPSRN,
                        ТипОтвета = "1",
                        ТитульнаяЧасть = request.Запрос?.Субъект,
                        КБКИ =
                        [
                            new()
                            {
                                ОГРН = OurBureauPSRN,
                                ПоСостояниюНа = DateTime.Now,
                                СубъектНеНайден = new(),
                                ИдентификаторОтвета = guid
                            }
                        ]
                    };
                    ResponseXml = _xmlService.SerializeAsByte(response);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return Ok(new MemoryStream(signedResponse));
                }

                return StatusCode(500);
            }
        }

        /// <summary>
        /// получение сведений о среднемесячных платежах Субъекта по идентификатору ответа
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// В случае получения ошибки «Ответ не готов» клиент должен повторить запрос не ранее, чем через 1 секунду
        /// </remarks>
        /// <response code="200">Результат запроса содержит сведения о среднемесячных платежах Субъекта.</response>
        /// <response code="202">результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов».</response>
        /// <response code="400">результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
        [HttpGet("dlanswer")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> DlAnswer(IFeatureManager manager, string? id = null)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            var guid = Guid.NewGuid().ToString();
            byte[]? signedResponse = null;
            byte[]? responseXml = null;
            var serviceName = "dlanswer";
            var certificate = Request.HttpContext.Connection.ClientCertificate;
            var IpAddress = Request.HttpContext.Connection.RemoteIpAddress;

            try
            {
                await _redisCache.AddHash(serviceName, guid, "RequestTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (IpAddress != null)
                    await _redisCache.AddHash(serviceName, guid, "IpAddress", IpAddress.ToString());

                await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");

                try
                {

                    /*  3. Запрос не содержит обязательных параметров
                     *  Проверяется наличие обязательных параметров
                     */
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        _logger.LogError("Запрос не содержит обязательных параметров: id");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: id");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    await _redisCache.AddHash(serviceName, guid, "guid", id);

                    /* Проверка прав доступа 
                     */
                    if (!(await _validationService.ValidateRules(certificate?.Thumbprint, "dlrequest")))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "22");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "22", "Запрос не доступен для абонента"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /*  1. Метод передачи запроса  не соответствует требуемому.
                     *  Проверяется тип метода который пришел в api 
                     */
                    if (Request.Method != "GET")
                    {
                        _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "1");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* Проверка сертификата в запросе */
                    if (!_validationService.ValidateCertificate(certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        responseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* 16. В качестве значения параметра id запроса
                     * /dlanswer или /dlputanswer указан
                     * идентификатор, который не выдавался абоненту
                     */
                    if (!await _redisCache.KeyExists(["dlrequest", id]))
                    {
                        _logger.LogError("Указан некорректный идентификатор ответа");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "16");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Указан некорректный идентификатор ответа");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "16", $"Указан некорректный идентификатор ответа"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Время окончания валидации
                    await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Пока задача не готова возвращаем статус 12 - Ответ не готов.
                    if (_redisCache.TryGetHash("dlrequest", id, "response", out responseXml))
                    {
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return File(signedResponse, "application/octet-stream");
                    }
                    else
                    {
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "12");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "12", "Ответ не готов"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return Accepted(new MemoryStream(signedResponse));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Возникла критическая ошибка");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.ToString());
                    return StatusCode(500);
                }
                finally
                {
                    if (signedResponse is not null)
                        await _redisCache.AddHash(serviceName, guid, "SignedResponse", signedResponse);

                    if (responseXml is not null)
                        await _redisCache.AddHash(serviceName, guid, "ResponseXml", responseXml);


                    /* Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                     */
                    if (!(await _redisCache.HashFieldExists(serviceName, guid, "ValidationTime")))
                        await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Выгрузка в кафку
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
                           IpAddress = IpAddress?.ToString(),
                           certificate?.Thumbprint,
                           RequestTime,
                           ResponseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                           ex.Message
                       });

                // Выгрузка в кафку
                await _kafka.Produce(new Message<Null, string> { Value = message }, _kafkaTopic);

                _logger.LogCritical(ex, "Возникла критическая ошибка");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Передача от БКИ данных, необходимых для формирования и предоставления пользователям кредитных историй сведений о среднемесячных платежах Субъекта.
        /// </summary>
        /// <returns>Результат запроса – информация о результатах загрузки данных в базу данных КБКИ</returns>
        /// <response code="200">Результат запроса содержит информацию о результатах загрузки данных в базу данных КБКИ</response>
        /// <response code="202">Результат запроса содержит квитанцию с идентификатором ответа</response>
        /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке</response>
        [HttpPost("dlput")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> DlPut_v_1_3(ApiVersion apiVersion, IFeatureManager manager)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            var Timer = Stopwatch.StartNew();
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var serviceName = "dlput";
            var guid = Guid.NewGuid().ToString();
            var certificate = Request.HttpContext.Connection.ClientCertificate;
            var IpAddress = Request.HttpContext.Connection.RemoteIpAddress;

            try
            {
                await _redisCache.AddHash(serviceName, guid, "RequestTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (IpAddress != null)
                    await _redisCache.AddHash(serviceName, guid, "IpAddress", IpAddress.ToString());

                await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");

                QBCH_lib.qcb_xml.v1_3.qcb_put.ПредставлениеСведенийОПлатежах? request = null;

                try
                {

                    /* Провекра прав доступа */
                    if (!(await _validationService.ValidateRules(certificate?.Thumbprint, serviceName)))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "22");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не доступен для абонента");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "22", "Запрос не доступен для абонента"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Таймауты
                    var QBCHTimeout = _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs");
                    var TicketTimeout = _config.GetValue<int>("APIConfiguration:TicketTimeoutMs");
                    var ResponseTimeout = _config.GetValue<int>("APIConfiguration:ResponseTimeoutMs");

                    /*  1. Метод передачи запроса  не соответствует требуемому.
                     *  Проверяется тип метода который пришел в api 
                     */
                    if (Request.Method != "POST")
                    {
                        _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "1");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Метод передачи запроса не соответствует ожидаемому");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Файл запроса.
                    using var ms = new MemoryStream();
                    await Request.Body.CopyToAsync(ms);

                    /*  2. Запрос не содержит данных.
                     *  Проверяется длинна массива контента
                     */
                    if (ms.Length == 0)
                    {
                        _logger.LogError("Запрос не содержит данных");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "2");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит данных");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "2", "Запрос не содержит данных"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Подписанное тело запроса
                    await _redisCache.AddHash(serviceName, guid, "SignedRequest", ms.ToArray());

                    /* Обрабокта подписи файла
                     * Проверка подиси
                     * Снятие подписи
                     */
                    if (!_validationService.ValidateMsg(ms.ToArray(), certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Проверка по xsd.
                    using var mems = new MemoryStream(CryptoServiceResult.Body);

                    /* 9. Запрос не соответствует схеме
                     * Запрос не соответствует XSD-схеме запроса.
                     * В описании ошибки должна быть включена
                     * информация о том, почему запрос не
                     * соответствует схеме 
                     */
                    if (!_validationService.ValidateXml(new MemoryStream(CryptoServiceResult.Body), serviceName, apiVersion, out var xsdValidation))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(xsdValidation.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Тело запроса без подписи
                    await _redisCache.AddHash(serviceName, guid, "request", CryptoServiceResult.Body);

                    // Десериализация.
                    request = _xmlService.Deserialize<QBCH_lib.qcb_xml.v1_3.qcb_put.ПредставлениеСведенийОПлатежах>(CryptoServiceResult.Body);
                    await _redisCache.AddHash(serviceName, guid, "RequestId", request!.ИдентификаторЗапроса!);

                    /* Ошибка десериализации
                     */
                    if (request is null)
                    {
                        _logger.LogCritical("Ошибка десериализации");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Ошибка десериализации");
                        return StatusCode(500);
                    }

                    /* Подсчет кол-ва договоров
                     */
                    if (request.Договоры.Count > 1000)
                    {
                        _logger.LogError("Количество записей в методе put превышает 1000.");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "15");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "В одном запросе от БКИ на передачу данных допускается передавать данные не более чем о 1000 обязательств");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "15", "В одном запросе от БКИ на передачу данных допускается передавать данные не более чем о 1000 обязательств"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* 11. Идентификатор запроса не уникален
                     * Идентификатор запроса ранее передавался
                     * данным абонентом в составе другого запроса
                     * такого же типа
                     */
                    if (!_validationService.IsUniqueRequestId(request.ИдентификаторЗапроса!, serviceName, request.БКИ!.ОГРН!, out var uniqueValidationResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", uniqueValidationResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", uniqueValidationResult.Error ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(uniqueValidationResult.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Сравнение реквизитов запроса с данными в сертификате.                
                    if (!_validationService.AbonentValidation(Request.HttpContext.Connection.ClientCertificate?.Thumbprint, request.БКИ.ОГРН, out var abonentValidation))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", abonentValidation.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", abonentValidation.Error ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(abonentValidation.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    //var upload = _transformer.ConvertDlPutToUpload(request, abonentValidation).Select(x => _xmlService.SerializeAsString(x)).ToList();
                    //for (int i = 0; i < upload.Count; i++)
                    //{
                    //    System.IO.File.WriteAllText($@"\\W2016-term-023\ИТ\a.bakaev\Downloads\scheme_1_3\dlputs\dlput{i}.xml", upload[i]);
                    //}

                    // Дата не может быть пустой т.к. явялется обязательной по xsd, так же как id запроса и инн огрн в реквизитах.
                    await _redisCache.AddUniqueRequestId(serviceName, request.ИдентификаторЗапроса!, request.БКИ.ОГРН!);

                    //Запись Request в Redis
                    await _redisCache.AddHash(serviceName, guid, "request", _xmlService.SerializeAsByte(request));

                    // Время окончания валидации
                    await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    try
                    {
                        var task = Task.Run(async () =>
                        {
                            //Формируем события для выгрузки для отправки в Кафку
                            List<Document> upload = _transformer.ConvertDlPutToUpload(request, abonentValidation);

                            if (await _kafka.Produce(upload.Select(doc => new Message<string, string> { Key = "", Value = _xmlService.SerializeAsString(doc) }).ToList(), _config.GetSection("KafkaService:UploadTopic").Value))
                            {
                                //Отправляем в топик
                                QBCH_lib.qcb_xml.v1_3.qcb_putanswer.РезультатПредставленияСведений response = new()
                                {
                                    Версия = "1.2",
                                    БКИ = new()
                                    {
                                        ОГРН = "1077123000003",
                                        Value = "Общество с ограниченной ответственностью \"Бюро кредитных историй\""
                                    },
                                    ИдентификаторЗапроса = request.ИдентификаторЗапроса,
                                    ИдентификаторОтвета = guid,
                                    ОГРН = "1077123000003",
                                    Договоры = request.Договоры.Select(contract =>
                                    {
                                        QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорДобавить? add = contract.Item is QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорДобавить ? contract.Item as QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорДобавить : null;
                                        QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорУдалить? delete = contract.Item is QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорУдалить ? contract.Item as QBCH_lib.qcb_xml.v1_3.qcb_put.ДоговорУдалить : null;

                                        return new QBCH_lib.qcb_xml.v1_3.qcb_putanswer.РезультатПредставленияСведенийДоговор
                                        {
                                            Item = new(),
                                            УИД = contract.УИД,
                                            ДатаРасчета = add?.СреднемесячныйПлатеж?.ДатаРасчета ?? delete?.ДатаРасчета,
                                            Операция = add is not null ? QBCH_lib.qcb_xml.v1_3.Enums.СправочникОперации.Добавить : QBCH_lib.qcb_xml.v1_3.Enums.СправочникОперации.Удалить
                                        };
                                    }).ToList()
                                };

                                await _redisCache.AddHash(serviceName, guid, "response", _xmlService.SerializeAsByte(response));
                            };

                            await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        }).Wait(TimeSpan.FromMilliseconds(TicketTimeout - Timer.ElapsedMilliseconds));

                        // Если по всем задачам мы успели получить ответ, просто возвращаем ответ.
                        if (task)
                        {
                            if (_redisCache.TryGetHash(serviceName, guid, "response", out ResponseXml))
                            {
                                signedResponse = _cryptoService.SignMsg(ResponseXml);
                                return File(signedResponse, "application/octet-stream");
                            }
                        }
                        else
                        {
                            await _redisCache.AddHash(serviceName, guid, "ErrorCode", "12");
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        _logger.LogError(ex, "Ошибка времени ожидания выполнения запроса. Время проверки превысило {TicketTimeout} миллисекунд.", TicketTimeout);
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", $"Ошибка времени ожидания выполнения запроса. Время проверки превысило {TicketTimeout} миллисекунд.");
                    }

                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Ticket, requestId: request.ИдентификаторЗапроса, guid: guid));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);

                    return Accepted(new MemoryStream(signedResponse));
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Возникла критическая ошибка");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.ToString());
                    return StatusCode(500);
                }
                finally
                {
                    if (signedResponse is not null)
                        await _redisCache.AddHash(serviceName, guid, "SignedResponse", signedResponse);

                    if (ResponseXml is not null)
                        await _redisCache.AddHash(serviceName, guid, "ResponseXml", ResponseXml);

                    /* Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                     */
                    if (!(await _redisCache.HashFieldExists(serviceName, guid, "ValidationTime")))
                        await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Выгрузка в кафку
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
                           IpAddress = IpAddress?.ToString(),
                           certificate?.Thumbprint,
                           RequestTime,
                           ResponseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                           ex.Message
                       });

                // Выгрузка в кафку
                await _kafka.Produce(new Message<Null, string> { Value = message }, _kafkaTopic);

                _logger.LogCritical(ex, "Возникла критическая ошибка");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Получение информации о результатах загрузки данных
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="id">Значение идентификатора ответа, содержащегося в квитанции, полученной при передаче данных о среднемесячных платежах Субъекта</param>
        /// <returns>Информация о результатах загрузки данных в базу данных КБКИ</returns>
        /// <response code="200">Результат запроса содержит информацию о результатах загрузки данных в базу данных КБКИ</response>
        /// <response code="202">Результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов»</response>
        /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
        /// <remarks>
        /// В случае получения ошибки «Ответ не готов» абонент должен повторить запрос не ранее, чем через 1 секунду.
        /// </remarks>
        [HttpGet("dlputanswer")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> DlPutAnswer(IFeatureManager manager, string? id = null)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var serviceName = "dlputanswer";
            var guid = Guid.NewGuid().ToString();
            var IpAddress = Request.HttpContext.Connection.RemoteIpAddress;
            var certificate = Request.HttpContext.Connection.ClientCertificate;

            try
            {
                await _redisCache.AddHash(serviceName, guid, "RequestTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (IpAddress != null)
                    await _redisCache.AddHash(serviceName, guid, "IpAddress", IpAddress.ToString());

                await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");

                try
                {

                    /*  3. Запрос не содержит обязательных параметров
                     *  Проверяется наличие обязательных параметров
                     */
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        _logger.LogError("Запрос не содержит обязательных параметров: id");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    await _redisCache.AddHash(serviceName, guid, "guid", id);

                    /* Проверка прав доступа */
                    if (!(await _validationService.ValidateRules(certificate?.Thumbprint, "dlput")))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "22");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "22", "Запрос не доступен для абонента"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /*  1. Метод передачи запроса  не соответствует требуемому.
                     *  Проверяется тип метода который пришел в api 
                     */
                    if (Request.Method != "GET")
                    {
                        _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "1");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* Проверка сертификата в запросе
                     */
                    if (!_validationService.ValidateCertificate(certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                    }

                    /* 16. В качестве значения параметра id запроса
                     * /dlanswer или /dlputanswer указан
                     * идентификатор, который не выдавался абоненту
                     */
                    if (!await _redisCache.KeyExists(["dlput", id]))
                    {
                        _logger.LogError("Указан некорректный идентификатор ответа");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "16");
                        await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Указан некорректный идентификатор ответа");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "16", $"Указан некорректный идентификатор ответа"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Пока задача не готова возвращаем статус 12 - Ответ не готов.
                    if (_redisCache.TryGetHash("dlput", id, "response", out ResponseXml))
                    {
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return File(signedResponse, "application/octet-stream");
                    }
                    else
                    {
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        await _redisCache.AddHash(serviceName, guid, "ErrorCode", "12");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "12", "Ответ не готов"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return Accepted(new MemoryStream(signedResponse));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Возникла критическая ошибка");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.ToString());

                    return StatusCode(500);
                }
                finally
                {
                    if (signedResponse is not null)
                        await _redisCache.AddHash(serviceName, guid, "SignedResponse", signedResponse);

                    if (ResponseXml is not null)
                        await _redisCache.AddHash(serviceName, guid, "ResponseXml", ResponseXml);

                    /* Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                     */
                    if (!(await _redisCache.HashFieldExists(serviceName, guid, "ValidationTime")))
                        await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Выгрузка в кафку
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
                           IpAddress = IpAddress?.ToString(),
                           certificate?.Thumbprint,
                           RequestTime,
                           ResponseTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),
                           ex.Message
                       });

                // Выгрузка в кафку
                await _kafka.Produce(new Message<Null, string> { Value = message }, _kafkaTopic);

                _logger.LogCritical(ex, "Возникла критическая ошибка");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Добавление нового сертификата абонента
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Квитанция содержит информацию об успешной обработке запроса</response>
        /// <response code="400">Квитанция содержит информацию об ошибке</response>
        [HttpPost("certadd")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> CertAdd([FromForm] CertForm form, IFeatureManager manager)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            throw new NotImplementedException();

            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var guid = Guid.NewGuid().ToString();
            var serviceName = nameof(CertAdd);

            await _redisCache.AddHash(serviceName, guid, "RequestTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            var ip = Request.HttpContext.Connection.RemoteIpAddress;
            if (ip != null)
                await _redisCache.AddHash(serviceName, guid, "IpAddress", ip.ToString());

            try
            {
                /* Проверка прав доступа 
                 */
                var requestCertificate = Request.HttpContext.Connection.ClientCertificate;
                await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                if (!(await _validationService.ValidateRules(requestCertificate?.Thumbprint, serviceName)))
                {
                    _logger.LogError("Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "22");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "22", "Запрос не доступен для абонента"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия идентифкатора
                if (string.IsNullOrWhiteSpace(form.id))
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                await _redisCache.AddHash(serviceName, guid, "RequestId", form.id);

                // Проверка наличия sign
                if (form.sign is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: sign"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия cert
                if (form.cert is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: cert"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /*  1. Метод передачи запроса  не соответствует требуемому.
                 *  Проверяется тип метода который пришел в api 
                 */
                if (Request.Method != "POST")
                {
                    _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "1");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                var sign = new MemoryStream();
                var cert = new MemoryStream();

                await form.cert.CopyToAsync(cert);
                await form.sign.CopyToAsync(sign);

                await _redisCache.AddHash(serviceName, guid, "Cert", cert.ToArray());
                await _redisCache.AddHash(serviceName, guid, "Sign", sign.ToArray());

                try
                {
                    X509Certificate2 certifcate = new(cert.ToArray());
                    await _redisCache.AddHash(serviceName, guid, "FormCertThumbprint", certifcate.Thumbprint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{serviceName} не удалось считать thumbprint", serviceName);
                }

                // Проверка сертификата в запросе
                if (!_validationService.ValidateMsg(cert.ToArray(), requestCertificate, out var CryptoServiceResult, sign.ToArray()))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                /* 11. Идентификатор запроса не уникален
                 * Идентификатор запроса ранее передавался
                 * данным абонентом в составе другого запроса
                 * такого же типа
                 */
                if (!_validationService.IsUniqueRequestId(form.id, serviceName, CryptoServiceResult.RequestOGRN!, out var uniqueValidationResult))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", uniqueValidationResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", uniqueValidationResult.Error ?? "-");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(uniqueValidationResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Дата не может быть пустой т.к. явялется обязательной по xsd, так же как id запроса и инн огрн в реквизитах.
                await _redisCache.AddUniqueRequestId(serviceName, form.id, CryptoServiceResult.RequestOGRN!, DateTime.Now);

                // Проверка наличия записи о сертификате с таким thumbprint в БД
                if (await _validationService.IsCertExists(cert.ToArray()))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "99");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Такой сертификат уже существует.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "99", "Такой сертификат уже существует."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (await _certManagement.AddCertificate(cert.ToArray(), CryptoServiceResult?.RequestOGRN, guid))
                {
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Success, requestId: form.id));
                    await _redisCache.AddHash(serviceName, guid, "response", _xmlService.SerializeAsByte(ResponseXml));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return File(signedResponse, "application/octet-stream");
                }
                else
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "99");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Не удалось добавить сертификат.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "99", "Не удалось добавить сертификат."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.ToString());
                return StatusCode(500);
            }
            finally
            {
                if (signedResponse is not null)
                    await _redisCache.AddHash(serviceName, guid, "SignedResponse", signedResponse);

                if (ResponseXml is not null)
                    await _redisCache.AddHash(serviceName, guid, "ResponseXml", ResponseXml);

                // Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                // Время ответа
                await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                // Выгрузка в кафку
                await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" });
            }
        }

        /// <summary>
        /// Отзыв сертификата абонента
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <response code="200">Квитанция содержит информацию об успешной обработке запроса</response>
        /// <response code="400">Квитанция содержит информацию об ошибке</response>
        [HttpPost("certrevoke")]
        [MapToApiVersion("1.3")]
        public async Task<IActionResult> CertRevoke([FromForm] CertForm form, IFeatureManager manager)
        {
            if (await manager.IsEnabledAsync("DisableOldApiVersion_1_3"))
                return StatusCode(405, _depricated);

            throw new NotImplementedException();

            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var guid = Guid.NewGuid().ToString();
            var serviceName = nameof(CertRevoke);

            await _redisCache.AddHash(serviceName, guid, "RequestTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            var ip = Request.HttpContext.Connection.RemoteIpAddress;
            if (ip != null)
                await _redisCache.AddHash(serviceName, guid, "IpAddress", ip.ToString());

            try
            {
                // Проверка прав доступа
                var requestCertificate = Request.HttpContext.Connection.ClientCertificate;
                await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                if (!(await _validationService.ValidateRules(requestCertificate?.Thumbprint, serviceName)))
                {
                    _logger.LogError("Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "22");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "22", "Запрос не доступен для абонента"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия идентифкатора
                if (string.IsNullOrWhiteSpace(form.id))
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }
                await _redisCache.AddHash(serviceName, guid, "RequestId", form.id);

                // Проверка наличия sign
                if (form.sign is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: sign"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия cert
                if (form.cert is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "3");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "3", "Запрос не содержит обязательных параметров: cert"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /*  1. Метод передачи запроса  не соответствует требуемому.
                 *  Проверяется тип метода который пришел в api 
                 */
                if (Request.Method != "POST")
                {
                    _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "1");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                var sign = new MemoryStream();
                var cert = new MemoryStream();

                await form.cert.CopyToAsync(cert);
                await form.sign.CopyToAsync(sign);

                await _redisCache.AddHash(serviceName, guid, "Cert", cert.ToArray());
                await _redisCache.AddHash(serviceName, guid, "Sign", sign.ToArray());

                try
                {
                    X509Certificate2 certifcate = new(cert.ToArray());
                    await _redisCache.AddHash(serviceName, guid, "FormCertThumbprint", certifcate.Thumbprint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{serviceName} не удалось считать thumbprint", serviceName);
                }

                // Проверка сертификата в запросе
                if (!_validationService.ValidateMsg(cert.ToArray(), requestCertificate, out var CryptoServiceResult, sign.ToArray()))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", CryptoServiceResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", CryptoServiceResult.Error ?? "-");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                /* 11. Идентификатор запроса не уникален
                 * Идентификатор запроса ранее передавался
                 * данным абонентом в составе другого запроса
                 * такого же типа
                 */
                if (!_validationService.IsUniqueRequestId(form.id, serviceName, CryptoServiceResult.RequestOGRN!, out var uniqueValidationResult))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", uniqueValidationResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", uniqueValidationResult.Error ?? "-");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(uniqueValidationResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Дата не может быть пустой т.к. явялется обязательной по xsd, так же как id запроса и инн огрн в реквизитах.
                await _redisCache.AddUniqueRequestId(serviceName, form.id, CryptoServiceResult.RequestOGRN!, DateTime.Now);

                // Проверка наличия записи о сертификате с таким thumbprint в БД
                if (!await _validationService.IsCertExists(cert.ToArray()))
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "99");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Сертификат не найден.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "99", "Сертификат не найден."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (await _certManagement.SetCertificateInactive(cert.ToArray(), guid))
                {
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Success, requestId: form.id));
                    await _redisCache.AddHash(serviceName, guid, "response", _xmlService.SerializeAsByte(ResponseXml));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return File(signedResponse, "application/octet-stream");
                }
                else
                {
                    await _redisCache.AddHash(serviceName, guid, "ErrorCode", "99");
                    await _redisCache.AddHash(serviceName, guid, "ErrorMessage", "Не удалось удалить сертификат.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResultType.Error, "99", "Не удалось удалить сертификат."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                await _redisCache.AddHash(serviceName, guid, "ErrorCode", 500.ToString());
                await _redisCache.AddHash(serviceName, guid, "ErrorMessage", ex.ToString());
                return StatusCode(500);
            }
            finally
            {
                if (signedResponse is not null)
                    await _redisCache.AddHash(serviceName, guid, "SignedResponse", signedResponse);

                if (ResponseXml is not null)
                    await _redisCache.AddHash(serviceName, guid, "ResponseXml", ResponseXml);

                // Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                // Время ответа
                await _redisCache.AddHash(serviceName, guid, "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                // Выгрузка в кафку
                await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" });
            }
        }
    }

}