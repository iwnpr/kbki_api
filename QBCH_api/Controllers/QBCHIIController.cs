using System.Text.Json;
using Application_lib;
using Asp.Versioning;
using Confluent.Kafka;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.qcb_xml.v2_0.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH_api.QBCHProcessing.CreateAndValidation.DlRequestValidationMediatr;
using QBCH_api.QBCHProcessing.ResponseDataCollect.DlrequestProcessing;
using QBCH_api.QBCHProcessing.StoreProcessingData.Event;

namespace QBCH_api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cryptoService"></param>
    /// <param name="logger"></param>
    /// <param name="xmlService"></param>
    /// <param name="config"></param>
    /// <param name="redisСache"></param>
    /// <param name="kafka"></param>
    /// <param name="ticketService"></param>
    /// <param name="repository"></param>
    /// <param name="certManagement"></param>
    [ApiVersion("2.0")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    public class QBCHIIController(IMediator mediator,
                                  ICryptoAdapter cryptoService,
                                  ILogger<QBCHIIController> logger,
                                  IXmlService xmlService,
                                  IConfiguration config,
                                  IRedisAdapter redisСache,
                                  IKafkaAdapter kafka,
                                  ITicketService ticketService,
                                  IDBAdapter repository,
                                  ICertManagementService certManagement) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly ICryptoAdapter _cryptoService = cryptoService;
        private readonly ILogger<QBCHIIController> _logger = logger;
        private readonly IXmlService _xmlService = xmlService;
        private readonly IConfiguration _config = config;
        private readonly IRedisAdapter _redisCache = redisСache;
        private readonly IKafkaAdapter _kafka = kafka;
        private readonly ITicketService _ticketService = ticketService;
        private readonly IDBAdapter _repository = repository;
        private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");
        private readonly string? _kakfaTopic = config.GetValue<string>("KafkaService:Topic");

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
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> DlRequest_v_2(ApiVersion apiVersion)
        {
            // Считем время затраченное на проверки.        
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

            // Создание и валидация объекта transaction
            var transaction = await _mediator.Send(new DlRequestAggregateMediatrInput(apiVersion, Request));
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
            }
            ;

            transaction.TimeElapsedForValidation.Stop();
            _logger.LogDebug("{guid} Validation time elapsed: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

            try
            {
                // Опртавка запросов к КБКИ и поск инофрмации в БД
                var processingResult =
                    await _mediator
                            .Send(
                                new SendRequestsToQBCH(
                                    transaction,
                                    _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs"),
                                    _config.GetValue<int>("APIConfiguration:TicketTimeoutMs"),
                                    _config.GetValue<int>("APIConfiguration:ResponseTimeoutMs"),
                                    _ourBureauPSRN
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
        /// получение сведений о среднемесячных платежах Субъекта по идентификатору ответа
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
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> DlAnswer_v_2(string? id = null)
        {
            var RequestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
            var guid = Guid.NewGuid().ToString();
            byte[]? signedResponse = null;
            byte[]? responseXml = null;
            var serviceName = "dlanswer";
            var certificate = Request.HttpContext.Connection.ClientCertificate;
            var IpAddress = $"{Request.HttpContext.Connection.LocalIpAddress?.ToString()}|{Request.HttpContext.Connection.RemoteIpAddress?.ToString()}";

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
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResultv2(ResponseType.Error, "3", "Запрос не содержит обязательных параметров: id"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    await _redisCache.AddHash(serviceName, guid, "response_guid", id);

                    /* Проверка прав доступа 
                     */
                    if (!await _repository.IsPermissionGrantedv2(certificate?.Thumbprint, "dlrequest"))
                    {
                        _logger.LogError("Запрос не доступен для абонента");
                        await _redisCache.AddHash(serviceName, guid, "error_code", "22");
                        await _redisCache.AddHash(serviceName, guid, "error_message", "Запрос не доступен для абонента");
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResultv2(ResponseType.Error, "22", "Запрос не доступен для абонента"));
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
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResultv2(ResponseType.Error, "1", "Метод передачи запроса не соответствует ожидаемому"));
                        signedResponse = _cryptoService.SignMsg(responseXml);
                        return BadRequest(new MemoryStream(signedResponse));
                    }

                    /* Проверка сертификата в запросе */
                    if (!_cryptoService.ValidateCertificate(certificate, out var CryptoServiceResult))
                    {
                        await _redisCache.AddHash(serviceName, guid, "error_code", CryptoServiceResult.ErrorCode.ToString());
                        await _redisCache.AddHash(serviceName, guid, "error_message", CryptoServiceResult.Error ?? "-");
                        responseXml = _xmlService.SerializeAsByte(CryptoServiceResult.Ticket_v2);
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
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResultv2(ResponseType.Error, "16", $"Указан некорректный идентификатор ответа"));
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
                        responseXml = _xmlService.SerializeAsByte(_ticketService.CreateResultv2(ResponseType.Error, "12", "Ответ не готов"));
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
    }
}
