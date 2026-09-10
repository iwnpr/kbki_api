using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH.Lib.qcb_xml.v3_0;
using qbch_lib;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces.V3;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces.V3;
using System.Collections.Concurrent;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;

/// <summary>
/// Сбор данных ответа API 3.0 через отдельный handler.
/// </summary>
public class QBCHProcessingHandlerV3(
    ILogger<QBCHProcessingHandlerV3> logger,
    IQBCHServiceV3 qbchService,
    IKeyValueStorageService storageService,
    ICryptoService cryptoService,
    ITicketServiceV3 ticketService,
    IXmlServiceV3 xmlService,
    IHttpClientFactory httpClientFactory,
    IBKIRequisitsHandler bkiRequisitsHandler,
    ApiV3ContractRules contractRules)
    : IRequestHandler<QBCHProcessedStartV3, QBCHProcessingTransactionV3>
{
    private readonly ILogger<QBCHProcessingHandlerV3> _logger = logger;
    private readonly IQBCHServiceV3 _qbchService = qbchService;
    private readonly IKeyValueStorageService _storageService = storageService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ITicketServiceV3 _ticketService = ticketService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly List<QBCHRequisite> _qbchList = bkiRequisitsHandler.GetBureaList();
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly ConcurrentBag<Task<QBCHTaskResult>> _tasksList = [];

    private const string ProcessingStageTaskScheduling = "Постановка задач получения сведений";
    private const string ProcessingStageAwaitingTasks = "Ожидание ответов от источников сведений (БД и внешние КБКИ)";
    private const string ProcessingStageBuildingResponse = "Формирование и сохранение агрегированного ответа";

    public async Task<QBCHProcessingTransactionV3> Handle(QBCHProcessedStartV3 request, CancellationToken cancellationToken)
    {
        var transaction = request.Transaction;
        _logger.LogDebug("Начало формирования ответа: TransactionId={TransactionId}, ImmediateDeadlineMs={ImmediateDeadlineMs}",
            transaction.Id, request.ImmediateResponseDeadlineMs);

        var clientRequest = transaction.GetRequest<ЗапросСведений>();

        var requestId = clientRequest.ИдентификаторЗапроса;
        var requestDate = clientRequest.ДатаЗапроса;
        var requestType = clientRequest.ТипЗапроса;
        var requestMode = clientRequest.РежимЗапроса;

        _logger.LogDebug("Параметры запроса RequestId={requestId}, RequestDate={requestDate}, RequestType={requestType}, RequestMode={requestMode}",
            requestId, requestDate, requestType, requestMode);

        byte[]? responseXml = null;

        try
        {
            // Этап обработки: попадает в лог, чтобы по записи было видно, на чем именно упал сбор ответа.
            var processingStage = ProcessingStageTaskScheduling;

            var process = Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("Добавление задачи RequestFromDB, TransactionId={TransactionId}", transaction.Id);
                    _tasksList.Add(_qbchService.RequestFromDB(transaction));

                    // Item2 в API 3.0 — запрос "во все КБКИ".
                    if (clientRequest.ТипЗапроса == СправочникСпособыЗапроса.Item2)
                    {
                        _logger.LogDebug("Режим \"Во все БКИ\" — добавление задач для {bureauCount} КБКИ", _qbchList.Count);
                        _qbchList.ForEach(qbch =>
                        {
                            _logger.LogDebug("Добавление задачи RequestFromExternalBureau, bureau={bureau}", qbch.Name);
                            _tasksList.Add(_qbchService.RequestFromExternalBureau(transaction, _httpClientFactory.CreateClient($"{qbch.Name}v3"), qbch));
                        });
                    }

                    _logger.LogDebug("Ожидание выполнения {taskCount} задач", _tasksList.Count);
                    processingStage = ProcessingStageAwaitingTasks;

                    var results = await Task.WhenAll(_tasksList);

                    processingStage = ProcessingStageBuildingResponse;
                    responseXml = await BuildAndStoreAggregateResponseAsync(results, transaction, clientRequest, request.OurBureauPSRN, requestId, requestDate, requestType, requestMode);
                }
                catch (Exception ex)
                {
                    var error = AnswerErrorCode.Code99_OtherError(ex.Message);

                    _logger.LogCritical(ex,
                        "Не удалось сформировать ответ dlrequest v3 на этапе \"{ProcessingStage}\": задач={TaskCount}, requestId={RequestId}, типЗапроса={RequestType}, режимЗапроса={RequestMode}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                        processingStage, _tasksList.Count, requestId, requestType, requestMode, transaction.Id, error.Code, error.Message);

                    await StoreProcessingErrorAsync(transaction, error);
                }
            }).Wait(TimeSpan.FromMilliseconds(_contractRules.ImmediateResponseDeadlineMs - transaction.TimeElapsedForValidation.ElapsedMilliseconds));

            if (process && responseXml is not null)
            {
                _logger.LogDebug("Немедленный ответ готов, TransactionId={TransactionId}", transaction.Id);
                transaction.Complete(responseXml, _cryptoService.SignMsg(responseXml));
                return transaction;
            }

            logger.LogDebug("Процесс завершен до дедлайна, но ответ не готов (process={process}, responseXml={hasXml}) — переход к отложенному ответу",
                process, responseXml is not null);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex,
                "Время ответа истекло: валидация dlrequest v3 заняла {ValidationElapsedMs} мс при дедлайне немедленного ответа {ImmediateResponseDeadlineMs} мс, будет сформирован отложенный ответ. requestId={RequestId}, TransactionId={TransactionId}",
                transaction.TimeElapsedForValidation.ElapsedMilliseconds, request.ImmediateResponseDeadlineMs, requestId, transaction.Id);
        }
        catch (Exception ex)
        {
            var error = AnswerErrorCode.Code99_OtherError(ex.Message);

            _logger.LogCritical(ex,
               "Ошибка немедленной обработки dlrequest v3: RequestId={RequestId}, ТипЗапроса={RequestType}, РежимЗапроса={RequestMode}, ОтветСформирован={HasResponseXml}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                    requestId, requestType, requestMode, responseXml is not null, transaction.Id, error.Code, error.Message);
        }

        _logger.LogDebug("Формирование ответа завершено, TransactionId={TransactionId}", transaction.Id);
        return await CompleteAcceptedTransactionAsync(transaction, requestId, requestDate);
    }

    private async Task StoreProcessingErrorAsync(QBCHProcessingTransactionV3 transaction, AnswerErrorCode error)
    {
        _logger.LogDebug("Сохранение ошибки обработки в Redis. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
            transaction.Id, error.Code, error.Message);
        var responseId = transaction.Id.ToString();

        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, responseId, "cancellation_flag", "true");
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, responseId, "error_code", error.Code.ToString());
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, responseId, "error_message", error.Message);
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, responseId, "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        await _storageService.TrySetKeyExpiration(RedisConstants.DlRequestV3Scope, responseId, _contractRules.ResponseRetentionMinutes);
    }

    private async Task<byte[]> BuildAndStoreAggregateResponseAsync(
        QBCHTaskResult[] results,
        QBCHProcessingTransactionV3 transaction,
        ЗапросСведений clientRequest,
        string ourBureauPsrn,
        string requestId,
        DateTime requestDate,
        СправочникСпособыЗапроса requestType,
        СправочникРежимыЗапроса requestMode)
    {
        var response = new ОтветНаЗапросСведений
        {
            ИдентификаторЗапроса = requestId,
            ИдентификаторОтвета = transaction.Id.ToString(),
            ДатаЗапроса = requestDate.ToString("yyyy-MM-dd"),
            РежимЗапроса = requestMode,
            ТипОтвета = requestType,
            ОГРН = ourBureauPsrn,
            Сведения = (clientRequest.Запрос ?? [])
                .Select(x => new ОтветНаЗапросСведенийСведения
                {
                    ПорядковыйНомер = x.ПорядковыйНомер,
                    ТитульнаяЧасть = x.Субъект,
                })
                .ToArray()
        };

        foreach (var info in response.Сведения)
        {
            var kbkiItems = new List<ОтветНаЗапросСведенийСведенияКБКИ>();
            foreach (var taskResult in results)
            {
                var TaskResultXml = _xmlService.SerializeAsStringV3(taskResult.Answer3);
                await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{transaction.Id}:{taskResult.BureauPSRN}", "task_result_xml", TaskResultXml);
                await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{transaction.Id}:{taskResult.BureauPSRN}", "task_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                var sourceInfo = taskResult.Answer3?.Сведения?.FirstOrDefault(x => x.ПорядковыйНомер == info.ПорядковыйНомер);
                if (sourceInfo?.КБКИ is { Length: > 0 })
                {
                    kbkiItems.AddRange(sourceInfo.КБКИ);
                }
                else
                {
                    
                    var missingDataError = AnswerErrorCode.Code28_RequestDataNotFound();

                    _logger.LogWarning("В ответе КБКИ отсутствуют запрошенные сведения: ОГРН КБКИ={Bureau}, запрос {OrderNumber}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                        taskResult.BureauPSRN, info.ПорядковыйНомер, transaction.Id, missingDataError.Code, missingDataError.Message);

                    var errorKbki = new ОтветНаЗапросСведенийСведенияКБКИ
                    {
                        ОГРН = taskResult.BureauPSRN,
                        ПоСостояниюНа = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    };
                    errorKbki.УстановитьОшибку(missingDataError.Code, missingDataError.Message);
                    kbkiItems.Add(errorKbki);
                }
            }
            info.КБКИ = kbkiItems.ToArray();
        }

        var responseXml = _xmlService.SerializeAsByteV3(response);
        _logger.LogDebug("Агрегированный ответ сериализован, size={size} байт", responseXml.Length);

        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_aggregate_xml", responseXml);
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        return responseXml;
    }

    private async Task<QBCHProcessingTransactionV3> CompleteAcceptedTransactionAsync(QBCHProcessingTransactionV3 transaction, string requestId, DateTime requestDate)
    {
        var acceptedTicket = _ticketService.CreateResultV3Accepted(
            requestId: requestId,
            responseId: transaction.Id.ToString(),
            requestDate: requestDate
            );

        var ticketBytes = _xmlService.SerializeAsByteV3(acceptedTicket);
        transaction.Accepted();
        transaction.Complete(ticketBytes, _cryptoService.SignMsg(ticketBytes));

        return transaction;
    }
}