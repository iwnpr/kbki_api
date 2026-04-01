using Asp.Versioning;
using Cache_lib.Interfaces;
using CertManagement.Services.Interfaces;
using Confluent.Kafka;
using Crypto_lib.Model;
using Crypto_lib.Service;
using KafkaService_lib.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH_api.QBCHProcessing.CreateAndValidation.Command;
using QBCH_api.QBCHProcessing.Processing.Command;
using QBCH_api.QBCHProcessing.StoreProcessingData.Commands;
using QBCH_api.Services.Interfaces;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_put;
using QBCH_lib.Services.Interfaces;
using QBCHService_lib.Services.Interfaces;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using XmlService_lib.Services.Interfaces;

namespace QBCH_api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cryptoService"></param>
    /// <param name="logger"></param>
    /// <param name="xmlService"></param>
    /// <param name="validationService"></param>
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
    [ApiVersion("3.0")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    public class QBCHIIController(IMediator mediator,
                                  ICryptoService cryptoService,
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
                                  ICertManagementService certManagement) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly ICryptoService _cryptoService = cryptoService;
        private readonly ILogger<QbchController> _logger = logger;
        private readonly IXmlService _xmlService = xmlService;
        private readonly IValidationService _validationService = validationService;
        private readonly IQBCHService _qBCHService = qBCHService;
        private readonly IConfiguration _config = config;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IBKIRequisitsHandler _bKIRequisits = bKIRequisits;
        private readonly ICacheService _redisCache = redisСache;
        private readonly ITransformer _transformer = transformer;
        private readonly IKafkaService _kafka = kafka;
        private readonly ITicketService _ticketService = ticketService;
        private readonly IRepository _repository = repository;
        private readonly ICertManagementService _certManagement = certManagement;
        private readonly string? OurBureauPSRN = config.GetValue<string>("Bureau:PSRN");
        private readonly string? _kakfaTopic = config.GetValue<string>("KafkaService:Topic");

        /// <summary>
        /// Запрос сведений о CCП/самозапрет/антифрод
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
        [MapToApiVersion("3.0")]
        public async Task<IActionResult> DlRequest(ApiVersion apiVersion)
        {
            // Считем время затраченное на проверки.        
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

            // Создание и валидация объекта transaction
            var transaction = await _mediator.Send(new CreateToValidateCommand(apiVersion, Request));
            _logger.LogDebug("{guid} Request: {dt}", transaction.Id, RequestTime);
            if (transaction.ProcessingErrors.Count != 0)
            {
                var errorTicket = _ticketService.CreateResultV2Error(transaction.ProcessingErrors.First());
                var errorResult = _xmlService.SerializeAsByte(errorTicket);
                var signedResp = _cryptoService.SignMsg(errorResult);

                transaction
                    .Complete(response: errorResult,
                              singnedResponse: signedResp);

                // Отправка события завершения обработки transaction в Kafka и Redis
                await _mediator.Publish(new QBCHProcessingComplete(transaction));

                return BadRequest(new MemoryStream(transaction.Response.SignedTicket!));
            }

            try
            {
                // Дата не может быть пустой т.к. явялется обязательной по xsd, так же как id запроса и инн огрн в реквизитах.
                await _redisCache
                        .AddUniqueRequestId(
                            transaction.ServiceName,
                            transaction.ClentRequest.Request!.ИдентификаторЗапроса,
                            transaction.ClentRequest.Request.Абонент.Requisites!.ogrn!,
                            transaction.ClentRequest.Request.ДатаЗапроса
                        );
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка записи уникальнго requestId в redis");
            };

            transaction.TimeElapsedForValidation.Stop();
            _logger.LogDebug("{guid} Validation time elapsed: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

            try
            {
                // Опртавка запросов к КБКИ и поск инофрмации в БД
                var processingResult =
                    await _mediator
                            .Send(
                                new QBCHProcessedStart(
                                    transaction,
                                    _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs"),
                                    _config.GetValue<int>("APIConfiguration:TicketTimeoutMs"),
                                    _config.GetValue<int>("APIConfiguration:ResponseTimeoutMs"),
                                    OurBureauPSRN
                                )
                            );

                // Выполняется после ответа на запрос в контроллере
                // Отправка события завершения обработки transaction в Kafka и Redis
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
                    if (!await _kafka.Produce(new Message<Null, string> { Value = message }, _kakfaTopic)) // 1.3 - 2.0 разделить
                        _logger.LogCritical("Lost key:{key}", transaction.Id);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Критическая ошибка записи в redis");
                }

                _logger.LogCritical(ex, "Возникла критическая ошибка");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// получение сведений Субъекта по идентификатору ответа по идентификатору ответа
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <remarks>
        /// В случае получения ошибки «Ответ не готов» клиент должен повторить запрос не ранее, чем через 1 секунду
        /// </remarks>
        /// <response code="200">Результат запроса содержит сведения о среднемесячных платежах Субъекта.</response>
        /// <response code="202">результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов».</response>
        /// <response code="400">результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
        [HttpGet("dlanswer")]
        [MapToApiVersion("3.0")]
        public async Task<IActionResult> DlAnswer(string? id = null)
        {
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            var guid = Guid.NewGuid().ToString();
            byte[]? signedResponse = null;
            byte[]? responseXml = null;
            var serviceName = "dlanswer";
            var certificate = Request.HttpContext.Connection.ClientCertificate;
            var IpAddress = Request.HttpContext.Connection.RemoteIpAddress;

            try
            {
                await _redisCache.AddHash(serviceName, guid, "request_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                await _redisCache.AddHash(serviceName, guid, "temp_guid", guid);

                if (IpAddress != null)
                    await _redisCache.AddHash(serviceName, guid, "ip_address", IpAddress.ToString());

                await _redisCache.AddHash(serviceName, guid, "request_certificate_thumbprint", certificate?.Thumbprint ?? "-");
                await _redisCache.AddHash(serviceName, guid, "request_certificate_data", certificate?.Thumbprint ?? "-");

                try
                {
                    /*  3. Запрос не содержит обязательных параметров
                     *  Проверяется наличие обязательных параметров
                     */
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        _logger.LogError("Запрос не содержит обязательных параметров: id");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: id");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    await _redisCache.AddHash(serviceName, guid, "response_guid", id);

                    /* Проверка прав доступа 
                     */
                    if (!await _validationService.ValidateRules(certificate?.Thumbprint, "dlrequest"))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "22");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не доступен для абонента");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "22", "Запрос не доступен для абонента"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /*  1. Метод передачи запроса  не соответствует требуемому.
                     *  Проверяется тип метода который пришел в api 
                     */
                    if (Request.Method != "GET")
                    {
                        _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "1");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Метод передачи запроса не соответствует ожидаемому");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* Проверка сертификата в запросе */
                    if (!_validationService.ValidateCertificate(certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "error_code", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "error_message", CryptoServiceResult.Error ?? "-");
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
                        await _redisCache.AddHash(serviceName, guid, "error_code", "16");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Указан некорректный идентификатор ответа");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "16", $"Указан некорректный идентификатор ответа"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    // Время окончания валидации
                    await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Пока задача не готова возвращаем статус 12 - Ответ не готов.
                    if (_redisCache.TryGetHash("dlrequest", id, "qbch_tasks_aggregate_xml", out responseXml))
                    {
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return File(signedResponse, "application/octet-stream");
                    }
                    else
                    {
                        await _redisCache.AddHash(serviceName, guid, "error_code", "12");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Ответ не готов");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "12", "Ответ не готов"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return Accepted(new MemoryStream(signedResponse));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Возникла критическая ошибка");
                    await _redisCache.AddHash(serviceName, guid, "error_code", 500.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
                    return StatusCode(500);
                }
                finally
                {
                    if (signedResponse is not null)
                        await _redisCache.AddHash(serviceName, guid, "response_signed_data", signedResponse);

                    if (responseXml is not null)
                        await _redisCache.AddHash(serviceName, guid, "response_xml", responseXml);


                    /* Если время окончания валидации не существует, значит провекра не пройдена и результат таски возвращен не будет
                     */
                    if (!(await _redisCache.HashFieldExists(serviceName, guid, "validation_date_time")))
                        await _redisCache.AddHash(serviceName, guid, "validation_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    await _redisCache.AddHash(serviceName, guid, "response_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Выгрузка в кафку
                    await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" }, _kakfaTopic);
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
                await _kafka.Produce(new Message<Null, string> { Value = message }, _kakfaTopic);

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
        [MapToApiVersion("3.0")]
        [HttpPost("dlput")]
        public async Task<IActionResult> DlPut_v_2(ApiVersion apiVersion)
        {
            byte[]? signedResponse = null;
            byte[]? responseXml = null;
            var serviceName = "dlput";
            var guid = Guid.NewGuid().ToString();
            var certificate = Request.HttpContext.Connection.ClientCertificate;

            try
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                if (ms.Length == 0)
                {
                    responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "2", "Запрос не содержит данных"));
                    signedResponse = _cryptoService.SignMsg(responseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                if (!_validationService.ValidateMsg(ms.ToArray(), certificate, out var cryptoServiceResult))
                {
                    responseXml = _xmlService.SerializeAsByte(cryptoServiceResult.Ticket);
                    signedResponse = _cryptoService.SignMsg(responseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                if (!_validationService.ValidateXml(new MemoryStream(cryptoServiceResult.Body), serviceName, apiVersion, out var xsdValidation))
                {
                    responseXml = _xmlService.SerializeAsByte(xsdValidation.Ticket);
                    signedResponse = _cryptoService.SignMsg(responseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                var request = _xmlService.Deserialize<ПредставлениеСведений>(cryptoServiceResult.Body);
                if (request is null)
                {
                    responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "9", "Запрос не соответствует схеме"));
                    signedResponse = _cryptoService.SignMsg(responseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                await _redisCache.AddHash(serviceName, guid, "request", cryptoServiceResult.Body);
                await _redisCache.AddHash(serviceName, guid, "RequestId", request.ИдентификаторЗапроса ?? string.Empty);

                responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Ticket, requestId: request.ИдентификаторЗапроса, guid: guid));
                signedResponse = _cryptoService.SignMsg(responseXml);
                return Accepted(new MemoryStream(signedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Получение информации о результатах загрузки данных
        /// </summary>
        /// <param name="version"></param>
        /// <param name="id">Значение идентификатора ответа, содержащегося в квитанции, полученной при передаче данных о среднемесячных платежах Субъекта</param>
        /// <returns>Информация о результатах загрузки данных в базу данных КБКИ</returns>
        /// <response code="200">Результат запроса содержит информацию о результатах загрузки данных в базу данных КБКИ</response>
        /// <response code="202">Результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов»</response>
        /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
        /// <remarks>
        /// В случае получения ошибки «Ответ не готов» абонент должен повторить запрос не ранее, чем через 1 секунду.
        /// </remarks>
        [HttpGet("dlputanswer")]
        [MapToApiVersion("3.0")]
        public async Task<IActionResult> DlPutAnswer(ApiVersion version, string? id = null)
        {
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
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    await _redisCache.AddHash(serviceName, guid, "guid", id);

                    /* Проверка прав доступа */
                    if (!(await _validationService.ValidateRules(certificate?.Thumbprint, "dlput")))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "22");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "22", "Запрос не доступен для абонента"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /*  1. Метод передачи запроса  не соответствует требуемому.
                     *  Проверяется тип метода который пришел в api 
                     */
                    if (Request.Method != "GET")
                    {
                        _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "1");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Метод передачи запроса не соответствует ожидаемому");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* Проверка сертификата в запросе
                     */
                    if (!_validationService.ValidateCertificate(certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "error_code", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "error_message", CryptoServiceResult.Error ?? "-");
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
                        await _redisCache.AddHash(serviceName, guid, "error_code", "16");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Указан некорректный идентификатор ответа");
                        await _redisCache.AddHash(serviceName, guid, "Thumbprint", certificate?.Thumbprint ?? "-");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "16", $"Указан некорректный идентификатор ответа"));
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
                        await _redisCache.AddHash(serviceName, guid, "error_code", "12");
                        ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "12", "Ответ не готов"));
                        signedResponse = _cryptoService.SignMsg(ResponseXml);
                        return Accepted(new MemoryStream(signedResponse));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Возникла критическая ошибка");
                    await _redisCache.AddHash(serviceName, guid, "error_code", 500.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());

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
                    await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" }, _kakfaTopic);
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
                await _kafka.Produce(new Message<Null, string> { Value = message }, _kakfaTopic);

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
        [MapToApiVersion("3.0")]
        public async Task<IActionResult> CertAdd([FromForm] CertForm form)
        {
            throw new NotImplementedException();

            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var guid = Guid.NewGuid().ToString();
            var serviceName = "certadd";

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
                    await _redisCache.AddHash(serviceName, guid, "error_code", "22");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "22", "Запрос не доступен для абонента"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия идентифкатора
                if (string.IsNullOrWhiteSpace(form.id))
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                await _redisCache.AddHash(serviceName, guid, "RequestId", form.id);

                // Проверка наличия sign
                if (form.sign is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: sign"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия cert
                if (form.cert is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: cert"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /*  1. Метод передачи запроса  не соответствует требуемому.
                 *  Проверяется тип метода который пришел в api 
                 */
                if (Request.Method != "POST")
                {
                    _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "1");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", CryptoServiceResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", CryptoServiceResult.Error ?? "-");
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult.Error ?? "-");
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", "99");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Такой сертификат уже существует.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "99", "Такой сертификат уже существует."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (await _certManagement.AddCertificate(cert.ToArray(), CryptoServiceResult?.RequestOGRN, guid))
                {
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Success, requestId: form.id));
                    await _redisCache.AddHash(serviceName, guid, "response", _xmlService.SerializeAsByte(ResponseXml));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return File(signedResponse, "application/octet-stream");
                }
                else
                {
                    await _redisCache.AddHash(serviceName, guid, "error_code", "99");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Не удалось добавить сертификат.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "99", "Не удалось добавить сертификат."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                await _redisCache.AddHash(serviceName, guid, "error_code", 500.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
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
                await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" }, _kakfaTopic);
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
        [MapToApiVersion("3.0")]
        public async Task<IActionResult> CertRevoke([FromForm] CertForm form)
        {
            throw new NotImplementedException();

            byte[]? signedResponse = null;
            byte[]? ResponseXml = null;
            var guid = Guid.NewGuid().ToString();
            var serviceName = "certrevoke";

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
                    await _redisCache.AddHash(serviceName, guid, "error_code", "22");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не доступен для абонента");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "22", "Запрос не доступен для абонента"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия идентифкатора
                if (string.IsNullOrWhiteSpace(form.id))
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: id");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }
                await _redisCache.AddHash(serviceName, guid, "RequestId", form.id);

                // Проверка наличия sign
                if (form.sign is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: sign");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: sign"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                // Проверка наличия cert
                if (form.cert is null)
                {
                    _logger.LogError("Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "3");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не содержит обязательных параметров: cert");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: cert"));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(signedResponse));
                }

                /*  1. Метод передачи запроса  не соответствует требуемому.
                 *  Проверяется тип метода который пришел в api 
                 */
                if (Request.Method != "POST")
                {
                    _logger.LogError("Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "error_code", "1");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Метод передачи запроса не соответствует ожидаемому");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", CryptoServiceResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", CryptoServiceResult.Error ?? "-");
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", uniqueValidationResult.ErrorCode.ToString());
                    await _redisCache.AddHash(serviceName, guid, "error_message", uniqueValidationResult.Error ?? "-");
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
                    await _redisCache.AddHash(serviceName, guid, "error_code", "99");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Сертификат не найден.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "99", "Сертификат не найден."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

                await _redisCache.AddHash(serviceName, guid, "ValidationTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                if (await _certManagement.SetCertificateInactive(cert.ToArray(), guid))
                {
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Success, requestId: form.id));
                    await _redisCache.AddHash(serviceName, guid, "response", _xmlService.SerializeAsByte(ResponseXml));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return File(signedResponse, "application/octet-stream");
                }
                else
                {
                    await _redisCache.AddHash(serviceName, guid, "error_code", "99");
                    await _redisCache.AddHash(serviceName, guid, "error_message", "Не удалось удалить сертификат.");
                    await _redisCache.AddHash(serviceName, guid, "Thumbprint", requestCertificate?.Thumbprint ?? "-");
                    ResponseXml = _xmlService.SerializeAsByte(_ticketService.CreateResult(ResponseType.Error, "99", "Не удалось удалить сертификат."));
                    signedResponse = _cryptoService.SignMsg(ResponseXml);
                    return BadRequest(new MemoryStream(_cryptoService.SignMsg(ResponseXml)));
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла критическая ошибка");
                await _redisCache.AddHash(serviceName, guid, "error_code", 500.ToString());
                await _redisCache.AddHash(serviceName, guid, "error_message", ex.ToString());
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
                await _kafka.Produce(new Message<Null, string> { Value = $"QBCH:{serviceName}:{guid}" }, _kakfaTopic);
            }
        }
    }
}
