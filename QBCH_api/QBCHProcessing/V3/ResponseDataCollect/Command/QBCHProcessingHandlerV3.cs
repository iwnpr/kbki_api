using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH.Lib.qcb_xml.v3_0;
using qbch_lib.domain.errors;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.domain.aggregate;
using QBCH_lib.Services.Interfaces.V3;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces.V3;
using System.Diagnostics;
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
    : IRequestHandler<QBCHProcessedStartV3, QBCHProcessingTransaction>
{
    private const string DlRequestV3Scope = "dlrequest:v3";
    private const string ReadyAtUtcField = "ready_at_utc";
    private const string ReadyAtMskField = "ready_at_msk";
    private const string FirstPollAllowedAtUtcField = "first_poll_allowed_at_utc";
    private const string ResponseExpireAtUtcField = "response_expire_at_utc";
    private const string LastPollUtcField = "last_poll_utc";
    private const string ResponseGuidField = "response_guid";
    private readonly ILogger<QBCHProcessingHandlerV3> _logger = logger;
    private readonly IQBCHServiceV3 _qbchService = qbchService;
    private readonly IKeyValueStorageService _storageService = storageService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ITicketServiceV3 _ticketService = ticketService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly List<QBCHRequisite> _qbchList = bkiRequisitsHandler.GetBureaList();
    private readonly ApiV3ContractRules _contractRules = contractRules;

    public async Task<QBCHProcessingTransaction> Handle(QBCHProcessedStartV3 request, CancellationToken cancellationToken)
    {
        var transaction = request.Transaction;
        var input = transaction.GetRequest<ЗапросСведений>();

        if (input is null)
        {
            var error = Error.Code99_OtherError("Не удалось получить данные запроса");
            var nullRequestTicket = _ticketService.CreateResultV3Error(error);
            var nullRequestTicketBytes = _xmlService.SerializeAsByteV3(nullRequestTicket);

            transaction.Complete(nullRequestTicketBytes, _cryptoService.SignMsg(nullRequestTicketBytes));
            return transaction;
        }

        var requestId = input.ИдентификаторЗапроса;
        var requestDate = input.ДатаЗапроса;
        var requestType = input.ТипЗапроса;
        var requestMode = input.РежимЗапроса;

        byte[]? responseXml = null;

        try
        {
            var process = Task.Run(async () =>
            {
                try
                {
                    var tasks = new List<Task<QBCHTaskResult>>
                    {
                        _qbchService.RequestFromDB(transaction)
                    };

                    // Item2 в API 3.0 — запрос "во все КБКИ".
                    if (requestType == СправочникСпособыЗапроса.Item2)
                    {
                        _qbchList.ForEach(qbch =>
                        {
                            tasks.Add(_qbchService.RequestFromExternalBureau(transaction, _httpClientFactory.CreateClient($"{qbch.Name}v3"), qbch));
                        });
                    }

                    var results = await Task.WhenAll(tasks);
                    responseXml = await BuildAndStoreAggregateResponseAsync(results, transaction, input, request.OurBureauPSRN, requestId, requestDate, requestType, requestMode);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка выполнения запроса QBCH API 3.0");

                    await StoreProcessingErrorAsync(transaction, Error.Code99_OtherError(ex.Message));
                }
            }).Wait(TimeSpan.FromMilliseconds(request.ImmediateResponseDeadlineMs - transaction.TimeElapsedForValidation.ElapsedMilliseconds));

            if (process && responseXml is not null)
            {
                transaction.Complete(responseXml, _cryptoService.SignMsg(responseXml));
                return transaction;
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Время проверки превысило {ImmediateResponseDeadlineMs} миллисекунд.", request.ImmediateResponseDeadlineMs);
            return await CompleteAcceptedTransactionAsync(transaction, requestId, requestDate);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка выполнения запроса QBCH API 3.0");
            return await CompleteAcceptedTransactionAsync(transaction, requestId, requestDate);
        }

        return await CompleteAcceptedTransactionAsync(transaction, requestId, requestDate);
    }

    private async Task StoreProcessingErrorAsync(QBCHProcessingTransaction transaction, Error error)
    {
        var responseId = transaction.Id.ToString();

        await _storageService.AddHash(DlRequestV3Scope, responseId, "cancellation_flag", "true");
        await _storageService.AddHash(DlRequestV3Scope, responseId, "error_code", error.Code.ToString());
        await _storageService.AddHash(DlRequestV3Scope, responseId, "error_message", error.Message);
        await _storageService.AddHash(DlRequestV3Scope, responseId, "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        await _storageService.TrySetKeyExpiration(DlRequestV3Scope, responseId, _contractRules.ResponseRetentionMinutes);
    }

    private async Task<byte[]> BuildAndStoreAggregateResponseAsync(
        QBCHTaskResult[] results,
        QBCHProcessingTransaction transaction,
        ЗапросСведений input,
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
            Сведения = (input.Запрос ?? [])
                .Select(x => new ОтветНаЗапросСведенийСведения
                {
                    ПорядковыйНомер = x.ПорядковыйНомер,
                    ТитульнаяЧасть = x.Субъект,
                    КБКИ = []
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
                        ПоСостояниюНа = DateTime.Now,
                        ИдентификаторОтвета = transaction.Id.ToString()
                    };
                    errorKbki.УстановитьОшибку(28, "В ответе КБКИ отсутствуют запрошенные сведения");
                    kbkiItems.Add(errorKbki);
                }
            }
            info.КБКИ = kbkiItems.ToArray();
        }

        var responseXml = _xmlService.SerializeAsByteV3(response);
        await _storageService.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_aggregate_xml", responseXml);
        await _storageService.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        return responseXml;
    }

    private async Task<QBCHProcessingTransaction> CompleteAcceptedTransactionAsync(QBCHProcessingTransaction transaction, string requestId, DateTime requestDate)
    {
        var acceptedCreatedAtUtc = DateTimeOffset.UtcNow;
        var firstPollAllowedAtUtc = acceptedCreatedAtUtc.AddSeconds(_contractRules.MinAnswerPollingIntervalSeconds);
        var responseExpireAtUtc = acceptedCreatedAtUtc.AddHours(_contractRules.ResponseRetentionHours);
        var readyAtUtc = firstPollAllowedAtUtc;
        var readyTimeMs = Math.Max(1L, (long)(readyAtUtc - acceptedCreatedAtUtc).TotalMilliseconds);

        var acceptedTicket = _ticketService.CreateResultV3Accepted(
            requestId: requestId,
            responseId: transaction.Id.ToString(),
            requestDate: requestDate,
            readyTime: readyTimeMs);

        await SaveAcceptedPollingMetadataAsync(
            responseId: transaction.Id.ToString(),
            readyAtUtc: readyAtUtc,
            firstPollAllowedAtUtc: firstPollAllowedAtUtc,
            responseExpireAtUtc: responseExpireAtUtc);

        var ticketBytes = _xmlService.SerializeAsByteV3(acceptedTicket);
        transaction.Accepted();
        transaction.Complete(ticketBytes, _cryptoService.SignMsg(ticketBytes));
        return transaction;
    }

    private async Task SaveAcceptedPollingMetadataAsync(
        string responseId,
        DateTimeOffset readyAtUtc,
        DateTimeOffset firstPollAllowedAtUtc,
        DateTimeOffset responseExpireAtUtc)
    {
        await _storageService.AddHash(DlRequestV3Scope, responseId, ReadyAtUtcField, readyAtUtc.ToString("O"));
        await _storageService.AddHash(DlRequestV3Scope, responseId, ReadyAtMskField, readyAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("O"));
        await _storageService.AddHash(DlRequestV3Scope, responseId, FirstPollAllowedAtUtcField, firstPollAllowedAtUtc.ToString("O"));
        await _storageService.AddHash(DlRequestV3Scope, responseId, ResponseExpireAtUtcField, responseExpireAtUtc.ToString("O"));
        await _storageService.AddHash(DlRequestV3Scope, responseId, ResponseGuidField, responseId);
        await _storageService.AddHash(DlRequestV3Scope, responseId, LastPollUtcField, string.Empty);
        await _storageService.TrySetKeyExpiration(DlRequestV3Scope, responseId, _contractRules.ResponseRetentionMinutes);
    }
}