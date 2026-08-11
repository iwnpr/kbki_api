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

    public async Task<QBCHProcessingTransactionV3> Handle(QBCHProcessedStartV3 request, CancellationToken cancellationToken)
    {
        var transaction = request.Transaction;
        _logger.LogDebug("QBCHProcessingHandlerV3.Handle начало: TransactionId={TransactionId}, ImmediateDeadlineMs={ImmediateDeadlineMs}",
            transaction.Id, request.ImmediateResponseDeadlineMs);

        var clientRequest = transaction.GetRequest<ЗапросСведений>();

        var requestId = clientRequest.ИдентификаторЗапроса;
        var requestDate = clientRequest.ДатаЗапроса;
        var requestType = clientRequest.ТипЗапроса;
        var requestMode = clientRequest.РежимЗапроса;

        _logger.LogDebug("QBCHProcessingHandlerV3: параметры запроса RequestId={requestId}, RequestDate={requestDate}, RequestType={requestType}, RequestMode={requestMode}",
            requestId, requestDate, requestType, requestMode);

        byte[]? responseXml = null;

        try
        {
            var process = Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("QBCHProcessingHandlerV3: добавление задачи RequestFromDB, TransactionId={TransactionId}", transaction.Id);
                    _tasksList.Add(_qbchService.RequestFromDB(transaction));

                    // Item2 в API 3.0 — запрос "во все КБКИ".
                    if (clientRequest.ТипЗапроса == СправочникСпособыЗапроса.Item2)
                    {
                        _logger.LogDebug("QBCHProcessingHandlerV3: режим \"Во все БКИ\" — добавление задач для {bureauCount} КБКИ", _qbchList.Count);
                        _qbchList.ForEach(qbch =>
                        {
                            _logger.LogDebug("QBCHProcessingHandlerV3: добавление задачи RequestFromExternalBureau, bureau={bureau}", qbch.Name);
                            _tasksList.Add(_qbchService.RequestFromExternalBureau(transaction, _httpClientFactory.CreateClient($"{qbch.Name}v3"), qbch));
                        });
                    }

                    _logger.LogDebug("QBCHProcessingHandlerV3: ожидание выполнения {taskCount} задач", _tasksList.Count);
                    var results = await Task.WhenAll(_tasksList);
                    responseXml = await BuildAndStoreAggregateResponseAsync(results, transaction, clientRequest, request.OurBureauPSRN, requestId, requestDate, requestType, requestMode);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка выполнения запроса QBCH API 3.0");

                    await StoreProcessingErrorAsync(transaction, AnswerErrorCode.Code99_OtherError(ex.Message));
                }
            }).Wait(TimeSpan.FromMilliseconds(_contractRules.ImmediateResponseDeadlineMs - transaction.TimeElapsedForValidation.ElapsedMilliseconds));

            if (process && responseXml is not null)
            {
                _logger.LogDebug("QBCHProcessingHandlerV3: немедленный ответ готов, TransactionId={TransactionId}", transaction.Id);
                transaction.Complete(responseXml, _cryptoService.SignMsg(responseXml));
                return transaction;
            }

            logger.LogDebug("QBCHProcessingHandlerV3: процесс завершен до дедлайна, но ответ не готов (process={process}, responseXml={hasXml}) — переход к отложенному ответу",
                process, responseXml is not null);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Время проверки превысило {ImmediateResponseDeadlineMs} миллисекунд.", request.ImmediateResponseDeadlineMs);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка выполнения запроса QBCH API 3.0");
        }

        _logger.LogDebug("QBCHProcessingHandlerV3: формирование тикета Accepted, TransactionId={TransactionId}", transaction.Id);
        return await CompleteAcceptedTransactionAsync(transaction, requestId, requestDate);
    }

    private async Task StoreProcessingErrorAsync(QBCHProcessingTransactionV3 transaction, AnswerErrorCode error)
    {
        _logger.LogDebug("QBCHProcessingHandlerV3.StoreProcessingErrorAsync начало: TransactionId={TransactionId}, errorCode={errorCode}, errorMessage={errorMessage}",
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
        //NOTE: переименовал, так как input слишком абстрактен
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
                logger.LogDebug("{guid} {bureau}: Количество ответов {count}",
                    transaction.Id,
                    taskResult.BureauPSRN,
                    taskResult.Answer3?.Сведения?.Length ?? 0);

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
                    var errorKbki = new ОтветНаЗапросСведенийСведенияКБКИ
                    {
                        ОГРН = taskResult.BureauPSRN,
                        ПоСостояниюНа = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    };
                    errorKbki.УстановитьОшибку(28, "В ответе КБКИ отсутствуют запрошенные сведения");
                    kbkiItems.Add(errorKbki);
                }
            }
            info.КБКИ = kbkiItems.ToArray();
        }

        var responseXml = _xmlService.SerializeAsByteV3(response);
        _logger.LogDebug("QBCHProcessingHandlerV3.BuildAndStoreAggregateResponseAsync: агрегированный ответ сериализован, size={size} байт", responseXml.Length);

        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_aggregate_xml", responseXml);
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        return responseXml;
    }

    private async Task<QBCHProcessingTransactionV3> CompleteAcceptedTransactionAsync(QBCHProcessingTransactionV3 transaction, string requestId, DateTime requestDate)
    {
        logger.LogDebug("QBCHProcessingHandlerV3.CompleteAcceptedTransactionAsync начало: TransactionId={TransactionId}, requestId={requestId}", transaction.Id, requestId);


        var acceptedTicket = _ticketService.CreateResultV3Accepted(
            requestId: requestId,
            responseId: transaction.Id.ToString(),
            requestDate: requestDate
            );

        var ticketBytes = _xmlService.SerializeAsByteV3(acceptedTicket);
        transaction.Accepted();
        transaction.Complete(ticketBytes, _cryptoService.SignMsg(ticketBytes));

        _logger.LogDebug("QBCHProcessingHandlerV3.CompleteAcceptedTransactionAsync завершено: TransactionId={TransactionId}, Status={Status}", transaction.Id, transaction.Status);
        return transaction;
    }
}