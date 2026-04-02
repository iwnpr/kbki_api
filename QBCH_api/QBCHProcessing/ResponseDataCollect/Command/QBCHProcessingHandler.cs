using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_answer;
using QBCH_lib.Services.Interfaces;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using XmlService_lib.Services.Interfaces;

namespace QBCH_api.QBCHProcessing.Processing.Command;

/// <summary>
/// 
/// </summary>
public class QBCHProcessingHandler : IRequestHandler<QBCHProcessedStart, QBCHProcessingTransaction>
{
    private readonly ConcurrentBag<Task<QBCHTaskResult>> _tasksList = [];
    private readonly ILogger<QBCHProcessingHandler> _logger;
    private readonly IQBCHService _qBCHService;
    private readonly ICacheService _redisСache;
    private readonly ICryptoService _cryptoService;
    private readonly ITicketService _ticketService;
    private readonly IXmlService _xmlService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<QBCHRequisite> QBCHList;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="qBCHService"></param>
    /// <param name="redisСache"></param>
    /// <param name="cryptoService"></param>
    /// <param name="ticketService"></param>
    /// <param name="xmlService"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="bKIRequisitsHandler"></param>
    public QBCHProcessingHandler(ILogger<QBCHProcessingHandler> logger,
                                 IQBCHService qBCHService,
                                 ICacheService redisСache,
                                 ICryptoService cryptoService,
                                 ITicketService ticketService,
                                 IXmlService xmlService,
                                 IHttpClientFactory httpClientFactory,
                                 IBKIRequisitsHandler bKIRequisitsHandler)
    {
        _logger = logger;
        _qBCHService = qBCHService;
        _redisСache = redisСache;
        _cryptoService = cryptoService;
        _ticketService = ticketService;
        _xmlService = xmlService;
        _httpClientFactory = httpClientFactory;
        QBCHList = bKIRequisitsHandler.GetBureaList();

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QBCHProcessingTransaction> Handle(QBCHProcessedStart request, CancellationToken cancellationToken)
    {
        var transaction = request.Transaction;
        ОтветНаЗапросСведений response = new();
        byte[]? responseXml = null;

        try
        {
            var proccess = Task.Run(async () =>
            {
                try
                {
                    // Запрос ССП в БД
                    _tasksList.Add(_qBCHService.AmpFromDBv2(transaction));
                    if (transaction.ClentRequest.Request!.ТипЗапроса.Equals(СправочникСпособыЗапроса.All))
                    {
                        // Запросы в КБКИ
                        QBCHList.ForEach(qbch =>
                        {
                            _tasksList.Add(_qBCHService.AmpRequestv2(transaction, _httpClientFactory.CreateClient($"{qbch.Name!}v2"),qbch));
                        });
                    }

                    ОтветНаЗапросСведений response = new()
                    {
                        ИдентификаторЗапроса = transaction.ClentRequest.RequestId!,
                        ИдентификаторОтвета = transaction.Id.ToString(),
                        ДатаЗапроса = DateTime.Today.ToString("yyyy-MM-dd"),
                        РежимЗапроса = transaction.ClentRequest.Request.РежимЗапроса,
                        ТипОтвета = transaction.ClentRequest.Request.ТипЗапроса,
                        ОГРН = request.OurBureauPSRN,
                        Сведения = transaction.ClentRequest.Request.Запрос.Select(x => new Сведения()
                        {
                            ТитульнаяЧасть = x.Субъект,
                            ПорядковыйНомер = x.ПорядковыйНомер
                        }).ToList()
                    };

                    var tasksResult = (await Task.WhenAll(_tasksList)).ToArray();
                    QBCH_lib.qcb_xml.v3_0.CommonTypes.ТипОшибка? ошибка = null;

                    for (int i = 0; i < response.Сведения.Count; i++)
                    {
                        // Перебираем 4 таски и достаем данные из ноды КБКИ, сверяясь по порядковому номеру
                        foreach (var taskResult in tasksResult)
                        {
                            _logger.LogDebug("{guid} {BureauPSRN}: Количество ответов {i}", transaction.Id, taskResult.BureauPSRN, taskResult.Answer2?.Сведения.Count ?? 0);

                            var TaskResultXml = _xmlService.SerializeAsString(taskResult.Answer2);
                            await _redisСache.AddHash("dlrequest", $"{transaction.Id}:{taskResult.BureauPSRN}", "task_result_xml", TaskResultXml);
                            await _redisСache.AddHash("dlrequest", $"{transaction.Id}:{taskResult.BureauPSRN}", "task_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                            // Если данные есть то кладем в наш xml-ответа КБКИ данные из ответа, включая вложенные ошибки/данные и т.д.
                            if (taskResult.Answer2!.Сведения.FirstOrDefault(x => x.ПорядковыйНомер == response.Сведения[i].ПорядковыйНомер) is Сведения Сведения)
                            {
                                response.Сведения[i]?.КБКИ.AddRange(Сведения.КБКИ);

                                if (transaction.ClentRequest.Request.РежимЗапроса == СправочникРежимыЗапроса.Single)
                                    ошибка = Сведения.КБКИ?.FirstOrDefault()?.Ошибка;
                            }
                            // Если данных нет, формируем в наш xml-ответа ноду КБКИ с ошибкой 
                            else
                            {
                                ошибка = new()
                                {
                                    Код = "28",
                                    Value = "В ответе КБКИ отсутствуют запрошенные сведения"
                                };
                                response.Сведения[i]?.КБКИ.Add(new()
                                {
                                    ПоСостояниюНа = DateTime.Now,
                                    ОГРН = taskResult.BureauPSRN,
                                    Ошибка = ошибка
                                });
                            }
                        }
                    }

                    responseXml = _xmlService.SerializeAsByte(response);
                    _logger.LogInformation("Данные по задачам были добавлены в Redis");

                    await _redisСache.AddHash(transaction.ServiceName, transaction.Id.ToString(), "qbch_tasks_aggregate_xml", responseXml);
                    _logger.LogInformation("Данные о времени окончания задач добавлены в Redis");

                    await _redisСache.AddHash(transaction.ServiceName, transaction.Id.ToString(), "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "QBCH exception");
                    //REDIS CANCHELATION_FLAG-MESSAGE 
                    await _redisСache.AddHash(transaction.ServiceName, transaction.Id.ToString(), "cancellation_flag", "true");
                    await _redisСache.AddHash(transaction.ServiceName, transaction.Id.ToString(), "error_code", "99");
                    await _redisСache.AddHash(transaction.ServiceName, transaction.Id.ToString(), "error_message", ex.Message);
                }
            }).Wait(TimeSpan.FromMilliseconds(request.TicketTimeout - transaction.TimeElapsedForValidation.ElapsedMilliseconds));

            if (proccess)
            {
                if (responseXml != null)
                {
                    transaction.Complete(responseXml, _cryptoService.SignMsg(responseXml));
                    return transaction;
                }
            }
        }
        catch (ArgumentOutOfRangeException ex) //превышение таймаута 
        {
            _logger.LogError(ex, "Ошибка времени ожидания выполнения запроса. Время проверки превысило {TicketTimeout} миллисекунд.", request.TicketTimeout);
        }

        var commonTicket = _ticketService.CreateReceiptWithAnswerId(
            requestId: transaction.ClentRequest.RequestId!,
            answerId: transaction.Id.ToString(),
            requestDate: transaction.ClentRequest.Request!.ДатаЗапроса,
            readyInMs: request.ResponseTimeout);

        var commonTicketBytes = _xmlService.SerializeAsByte(commonTicket);

        transaction.Accepted();
        transaction.Complete(commonTicketBytes, _cryptoService.SignMsg(commonTicketBytes));

        return transaction;
    }
}

