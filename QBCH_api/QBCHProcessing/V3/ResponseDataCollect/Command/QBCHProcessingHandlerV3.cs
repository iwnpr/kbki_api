using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH.Lib.qcb_xml.v3_0;
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
    ICacheService redisCache,
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
    private readonly ICacheService _redisCache = redisCache;
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
            var nullRequestTicket = _ticketService.CreateResultV3Error(new QBCH_lib.core.Error(99, "Не удалось получить данные запроса API 3.0"));
            var nullRequestTicketBytes = _xmlService.SerializeAsByteV3(nullRequestTicket);
            transaction.Complete(nullRequestTicketBytes, _cryptoService.SignMsg(nullRequestTicketBytes));
            return transaction;
        }

        var processingTimer = Stopwatch.StartNew();
        var tasks = new List<Task<QBCHTaskResult>>
        {
            _qbchService.AmpFromDBv3(transaction)
        };

        // Item2 в API 3.0 — запрос "во все КБКИ".
        if (input.ТипЗапроса == СправочникСпособыЗапроса.Item2)
        {
            _qbchList.ForEach(qbch =>
            {
                tasks.Add(_qbchService.AmpRequestv3(
                    processing: transaction,
                    client: _httpClientFactory.CreateClient($"{qbch.Name}v3"),
                    bureau: qbch));
            });
        }

        try
        {
            var results = await Task.WhenAll(tasks);

            var response = new ОтветНаЗапросСведений
            {
                ИдентификаторЗапроса = input.ИдентификаторЗапроса,
                ИдентификаторОтвета = transaction.Id.ToString(),
                ДатаЗапроса = input.ДатаЗапроса.ToString("yyyy-MM-dd"),
                РежимЗапроса = input.РежимЗапроса,
                ТипОтвета = input.ТипЗапроса,
                ОГРН = request.OurBureauPSRN,
                Сведения = (input.Запрос ?? [])
                    .Select(x => new ОтветНаЗапросСведенийСведения
                    {
                        ПорядковыйНомер = x.ПорядковыйНомер,
                        ТитульнаяЧасть = x.Субъект,
                        КБКИ = []
                    })
                    .ToArray()
            };

            for (var i = 0; i < response.Сведения.Length; i++)
            {
                var info = response.Сведения[i];
                var kbkiItems = new List<ОтветНаЗапросСведенийСведенияКБКИ>();

                foreach (var taskResult in results)
                {
                    _logger.LogDebug("{guid} {bureau}: Количество ответов {count}",
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
            await _redisCache.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_aggregate_xml", responseXml);
            await _redisCache.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "qbch_tasks_end_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            processingTimer.Stop();

            // Для API 3.0: если время сборки превышает 4 секунды, возвращаем qcb_result с ИдентификаторОтвета.
            var hardDeadlineMs = 4000;
            var configuredDeadlineMs = request.ImmediateResponseDeadlineMs > 0
                ? request.ImmediateResponseDeadlineMs
                : _contractRules.ImmediateResponseDeadlineMs;

            var immediateDeadlineMs = Math.Min(hardDeadlineMs, configuredDeadlineMs);
            if (processingTimer.ElapsedMilliseconds > immediateDeadlineMs)
            {
                var acceptedCreatedAtUtc = DateTimeOffset.UtcNow;
                var firstPollAllowedAtUtc = acceptedCreatedAtUtc.AddSeconds(_contractRules.MinAnswerPollingIntervalSeconds);
                var responseExpireAtUtc = acceptedCreatedAtUtc.AddHours(_contractRules.ResponseRetentionHours);
                var readyAtUtc = firstPollAllowedAtUtc;
                var readyTimeMs = Math.Max(1L, (long)(readyAtUtc - acceptedCreatedAtUtc).TotalMilliseconds);

                var acceptedTicket = _ticketService.CreateResultV3Accepted(
                    requestId: input.ИдентификаторЗапроса,
                    responseId: transaction.Id.ToString(),
                    requestDate: input.ДатаЗапроса,
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

            transaction.Complete(responseXml, _cryptoService.SignMsg(responseXml));
            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "QBCH API 3.0 exception");
            await _redisCache.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "cancellation_flag", "true");
            await _redisCache.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "error_code", "99");
            await _redisCache.AddHash(DlRequestV3Scope, transaction.Id.ToString(), "error_message", ex.Message);

            var failedTicket = _ticketService.CreateResultV3Error(new QBCH_lib.core.Error(99, ex.Message));
            var failedTicketBytes = _xmlService.SerializeAsByteV3(failedTicket);
            transaction.Complete(failedTicketBytes, _cryptoService.SignMsg(failedTicketBytes));
            return transaction;
        }
    }

    private async Task SaveAcceptedPollingMetadataAsync(
        string responseId,
        DateTimeOffset readyAtUtc,
        DateTimeOffset firstPollAllowedAtUtc,
        DateTimeOffset responseExpireAtUtc)
    {
        await _redisCache.AddHash(DlRequestV3Scope, responseId, ReadyAtUtcField, readyAtUtc.ToString("O"));
        await _redisCache.AddHash(DlRequestV3Scope, responseId, ReadyAtMskField, readyAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("O"));
        await _redisCache.AddHash(DlRequestV3Scope, responseId, FirstPollAllowedAtUtcField, firstPollAllowedAtUtc.ToString("O"));
        await _redisCache.AddHash(DlRequestV3Scope, responseId, ResponseExpireAtUtcField, responseExpireAtUtc.ToString("O"));
        await _redisCache.AddHash(DlRequestV3Scope, responseId, ResponseGuidField, responseId);
        await _redisCache.AddHash(DlRequestV3Scope, responseId, LastPollUtcField, string.Empty);
        await _redisCache.TrySetKeyExpiration(DlRequestV3Scope, responseId, _contractRules.ResponseRetentionMinutes);
    }
}