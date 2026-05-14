using Cache_lib.Interfaces;
using Crypto_lib.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.domain.aggregate;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces.V3;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.Xml.Linq;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCHService_lib.Services.Implementations.V3;

/// <summary>
/// Реализация сервиса обработки запросов КБКИ по API версии 3.
/// </summary>
public class QBCHServiceV3(
    ICryptoService cryptoService,
    IXmlServiceV3 xmlService,
    ILogger<QBCHServiceV3> logger,
    IRepositoryV3 qbchDb,
    ICacheService redisCache,
    IConfiguration config,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : IQBCHServiceV3
{
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ILogger<QBCHServiceV3> _logger = logger;
    private readonly IRepositoryV3 _qbchDb = qbchDb;
    private readonly ICacheService _redisCache = redisCache;
    private readonly IConfiguration _config = config;
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly string _ourBureauPsrn = config.GetValue<string>("Bureau:PSRN") ?? string.Empty;
    private readonly string _ourBureauInn = config.GetValue<string>("Bureau:INN") ?? string.Empty;
    private readonly int _qbchTicketTimeoutMs = config.GetValue<int>("APIConfiguration:QBCHTicketTimeoutMs", 4000);
    private readonly int _qbchTicketDelayMs = config.GetValue<int>("APIConfiguration:QBCHTicketDelayMs", 1000);
    private readonly int _qbchResponseTimeoutMs = config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs", 10000);
    private readonly int _qbchResponseDelayMs = config.GetValue<int>("APIConfiguration:QBCHResponseDelayMs", 1000);

    /// <summary>
    /// Выполняет обработку запроса на основе данных, полученных из внутренней базы.
    /// </summary>
    /// <param name="processing">Транзакция с телом запроса и техническим контекстом обработки.</param>
    /// <returns>Результат обработки с ответом <c>ОтветНаЗапросСведений</c>.</returns>
    public async Task<QBCHTaskResult> RequestFromDB(QBCHProcessingTransaction processing)
    {
        await _redisCache.AddHash("dlrequest", $"{processing.Id}:{_ourBureauPsrn}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        var package = processing.GetRequest<ЗапросСведений>();

        if (package is null)
            return new QBCHTaskResult(_ourBureauPsrn);

        var answer = new ОтветНаЗапросСведений
        {
            ИдентификаторЗапроса = package.ИдентификаторЗапроса,
            ИдентификаторОтвета = processing.Id.ToString(),
            ОГРН = _ourBureauPsrn,
            ТипОтвета = package.ТипЗапроса,
            РежимЗапроса = package.РежимЗапроса,
            ДатаЗапроса = package.ДатаЗапроса.ToString("yyyy-MM-dd")
        };

        var requests = package.Запрос ?? [];
        var timeLeft = _qbchResponseTimeoutMs * requests.Length - processing.TimeElapsedForValidation.ElapsedMilliseconds;
        _logger.LogDebug("{guid} {bureau}: Таймаут для запросов {timeLeft} ms", processing.Id, _ourBureauPsrn, timeLeft);

        var responseRows = new List<ОтветНаЗапросСведенийСведения>(requests.Length);

        foreach (var requestItem in requests)
        {
            var response = new ОтветНаЗапросСведенийСведения
            {
                ПорядковыйНомер = requestItem.ПорядковыйНомер,
                ТитульнаяЧасть = requestItem.Субъект
            };

            var kbki = new ОтветНаЗапросСведенийСведенияКБКИ
            {
                ОГРН = _ourBureauPsrn,
                ПоСостояниюНа = DateTime.Now,
                ИдентификаторОтвета = processing.Id.ToString()
            };

            var error = processing.PackageValidationErrors.FirstOrDefault(x => x.Id.ToString() == requestItem.ПорядковыйНомер);
            if (error is not null)
            {
                kbki.УстановитьОшибку(error.error_code, error.error_message ?? string.Empty);
                response.КБКИ = [kbki];
                responseRows.Add(response);
                continue;
            }

            var template = new ЗапросСведений
            {
                Абонент = package.Абонент,
                ДатаЗапроса = package.ДатаЗапроса,
                Версия = package.Версия,
                ИдентификаторЗапроса = package.ИдентификаторЗапроса,
                КодСведений = package.КодСведений,
                РежимЗапроса = package.РежимЗапроса,
                ТипЗапроса = package.ТипЗапроса,
                Запрос = [requestItem]
            };

            var xml = _xmlService.SerializeAsStringV3(template);
            var timer = Stopwatch.StartNew();
            var subjectKeys = await _qbchDb.GetSearchAllSubjectsV3(xml, timeLeft);
            timer.Stop();
            timeLeft -= timer.ElapsedMilliseconds;

            if (subjectKeys.Count == 0)
            {
                kbki.ПометитьКакСубъектНеНайден();
                response.КБКИ = [kbki];
                responseRows.Add(response);
                continue;
            }

            var getSelfProhibitionTask = _qbchDb.GetSelfProhibitionV3(subjectKeys, timeLeft);

            var includeAmp = package.КодСведений == СправочникВидыСведений.Item7;
            var includeAntifraud = package.КодСведений is СправочникВидыСведений.Item7 or СправочникВидыСведений.Item8;
            var getAmpTask = includeAmp ? _qbchDb.GetCalculationOfAmpV3(subjectKeys, timeLeft) : null;
            var getAntifraudTask = includeAntifraud ? _qbchDb.GetAntifraudV3(subjectKeys, timeLeft) : null;

            var pendingTasks = new List<Task<XElement?>> { getSelfProhibitionTask };

            if (getAmpTask is not null)
                pendingTasks.Add(getAmpTask);

            if (getAntifraudTask is not null)
                pendingTasks.Add(getAntifraudTask);

            await Task.WhenAll(pendingTasks);

            FillObligationsSection(kbki, includeAmp, getAmpTask?.Result);
            FillSelfProhibitionSection(kbki, getSelfProhibitionTask.Result, requestItem.Субъект?.ИНН);
            FillAntifraudSection(kbki, includeAntifraud, getAntifraudTask?.Result);

            response.КБКИ = [kbki];
            responseRows.Add(response);
        }

        answer.Сведения = responseRows.ToArray();
        return new QBCHTaskResult(_ourBureauPsrn, answer3: answer);
    }

    /// <summary>
    /// Отправляет запрос во внешнее бюро и возвращает итоговый ответ.
    /// </summary>
    /// <param name="processing">Транзакция обработки с данными запроса.</param>
    /// <param name="client">HTTP-клиент для взаимодействия с внешним сервисом бюро.</param>
    /// <param name="bureau">Реквизиты целевого бюро кредитных историй.</param>
    /// <returns>Результат обработки, содержащий ответ бюро или информацию об ошибке.</returns>
    public async Task<QBCHTaskResult> RequestFromExternalBureau(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau)
    {
        var request = processing.GetRequest<ЗапросСведений>();
        if (request is null)
        {
            return new QBCHTaskResult(bureau.ogrn);
        }

        var guid = processing.Id.ToString();
        var orderNumbers = request.Запрос?.Select(x => x.ПорядковыйНомер).ToArray() ?? [];

        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        request.ИдентификаторЗапроса = guid;
        request.Абонент = new ЗапросСведенийАбонент
        {
            Item = new ЗапросСведенийАбонентЮридическоеЛицо
            {
                ИНН = _ourBureauInn,
                ОГРН = _ourBureauPsrn
            }
        };
        request.ТипЗапроса = СправочникСпособыЗапроса.Item1;

        var dlrequestBytes = _xmlService.SerializeAsByteV3(request);
        var signedDlrequestBytes = _cryptoService.SignMsg(dlrequestBytes);
        var dlrequestContent = new ByteArrayContent(signedDlrequestBytes);

        using var ticketCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_qbchTicketTimeoutMs));
        using var ticketCheckCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Math.Max(100, _qbchTicketTimeoutMs - 1000)));
        var ticketTimer = Stopwatch.StartNew();

        ОтветНаЗапросСведений? dlrequestResult = null;
        Результат? ticket = null;
        HttpResponseMessage? responseMessage = null;

        try
        {
            while (dlrequestResult is null && ticket is null)
            {
                var redisMsg = DlRequestRedisMessage.Create(DateTime.Now, signedDlrequestBytes, dlrequestBytes);

                try
                {
                    responseMessage = await client.PostAsync("dlrequest", dlrequestContent, ticketCts.Token);
                    using var ms = new MemoryStream();
                    await responseMessage.Content.CopyToAsync(ms, ticketCts.Token);
                    redisMsg.SetResponseCode(responseMessage.StatusCode).SetResponseTime(DateTime.Now);

                    switch (responseMessage.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var answerValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_answer.xsd", redisMsg, ticketCheckCts.Token);
                            if (answerValidation.IsError)
                            {
                                dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, answerValidation.ErrorCode.ToString(), answerValidation.Error ?? "Ошибка валидации", orderNumbers, guid, request);
                                break;
                            }

                            dlrequestResult = _xmlService.DeserializeV3<ОтветНаЗапросСведений>(answerValidation.Body);
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "response_id", dlrequestResult?.ИдентификаторОтвета ?? "-");
                            break;

                        case HttpStatusCode.BadRequest:
                            var badValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ticketCheckCts.Token);
                            if (badValidation.IsError)
                            {
                                dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, badValidation.ErrorCode.ToString(), badValidation.Error ?? "Ошибка валидации", orderNumbers, guid, request);
                                break;
                            }

                            var badTicket = _xmlService.DeserializeV3<Результат>(badValidation.Body);
                            var badError = badTicket?.Item as ТипОшибка;
                            dlrequestResult = badError is not null
                                ? CreateErrorAnswerV3(badTicket?.ОГРН ?? bureau.ogrn!, badError.Код ?? "99", badError.Value ?? "Ошибка", orderNumbers, guid, request)
                                : CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers, guid, request);
                            break;

                        case HttpStatusCode.Accepted:
                            var ticketValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ticketCheckCts.Token);
                            if (ticketValidation.IsError)
                            {
                                dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, ticketValidation.ErrorCode.ToString(), ticketValidation.Error ?? "Ошибка валидации", orderNumbers, guid, request);
                                break;
                            }

                            ticket = _xmlService.DeserializeV3<Результат>(ticketValidation.Body);
                            if (ticket?.Item is not РезультатИдентификаторОтвета)
                            {
                                dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers, guid, request);
                            }
                            break;

                        default:
                            redisMsg.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{await responseMessage.Content.ReadAsStringAsync(ticketCts.Token)}");
                            break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau} address: {address}", bureau.Name, "/dlrequest");
                    redisMsg.SetError("17", "Не удалось установить соединение.");
                }
                finally
                {
                    await _redisCache.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                }

                if (dlrequestResult is null && ticket is null)
                {
                    await Task.Delay(_qbchTicketDelayMs, ticketCts.Token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, "18", "Время ожидания ответа истекло.", orderNumbers, guid, request);
        }

        if (dlrequestResult is not null)
        {
            return new QBCHTaskResult(bureau.ogrn!, answer3: dlrequestResult);
        }

        var responseId = (ticket?.Item as РезультатИдентификаторОтвета)?.Value;
        if (string.IsNullOrWhiteSpace(responseId))
        {
            var invalidTicket = CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers, guid, request);
            return new QBCHTaskResult(bureau.ogrn!, answer3: invalidTicket);
        }

        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "response_id", responseId);

        var timeLeftMs = _qbchResponseTimeoutMs * (request.Запрос?.Length ?? 1)
            - ticketTimer.ElapsedMilliseconds
            - processing.TimeElapsedForValidation.ElapsedMilliseconds;

        using var resendCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Math.Max(100, timeLeftMs)));

        try
        {
            var dlanswerResult = await ResendDlanswer(responseId, client, bureau, guid, resendCts.Token, orderNumbers, request);
            return new QBCHTaskResult(bureau.ogrn!, answer3: dlanswerResult);
        }
        catch (TaskCanceledException)
        {
            var timeout = CreateErrorAnswerV3(bureau.ogrn!, "18", "Время ожидания ответа истекло.", orderNumbers, guid, request);
            return new QBCHTaskResult(bureau.ogrn!, answer3: timeout);
        }
    }

    private async Task<ОтветНаЗапросСведений> ResendDlanswer(string responseId, HttpClient client, QBCHRequisite bureau, string guid, CancellationToken ct, string[] orderNumbers, ЗапросСведений request)
    {
        HttpResponseMessage? responseMessage = null;

        while (true)
        {
            var redisMsg = DlAnswerRedisMessage.Create();
            try
            {
                responseMessage = await client.GetAsync($"dlanswer?id={responseId}", ct);
                using var ms = new MemoryStream();
                await responseMessage.Content.CopyToAsync(ms, ct);

                redisMsg.SetResponseCode(responseMessage.StatusCode).SetResponseTime(DateTime.Now);

                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var okValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_answer.xsd", redisMsg, ct);
                        if (okValidation.IsError)
                        {
                            return CreateErrorAnswerV3(bureau.ogrn!, okValidation.ErrorCode.ToString(), okValidation.Error ?? "Ошибка валидации", orderNumbers, guid, request);
                        }

                        var answer = _xmlService.DeserializeV3<ОтветНаЗапросСведений>(okValidation.Body);
                        return answer ?? CreateErrorAnswerV3(bureau.ogrn!, "19", "Ответ не соответствует схеме", orderNumbers, guid, request);

                    case HttpStatusCode.Accepted:
                    case HttpStatusCode.BadRequest:
                        var ticketValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ct);
                        if (ticketValidation.IsError)
                        {
                            return CreateErrorAnswerV3(bureau.ogrn!, ticketValidation.ErrorCode.ToString(), ticketValidation.Error ?? "Ошибка валидации", orderNumbers, guid, request);
                        }

                        var ticket = _xmlService.DeserializeV3<Результат>(ticketValidation.Body);
                        var ticketError = ticket?.Item as ТипОшибка;

                        if (ticketError is not null)
                        {
                            if (ticketError.Код != "12")
                            {
                                return CreateErrorAnswerV3(ticket?.ОГРН ?? bureau.ogrn!, ticketError.Код ?? "99", ticketError.Value ?? "Ошибка", orderNumbers, guid, request);
                            }
                        }
                        else
                        {
                            return CreateErrorAnswerV3(bureau.ogrn!, "99", "Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа.", orderNumbers, guid, request);
                        }
                        break;

                    default:
                        redisMsg.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{await responseMessage.Content.ReadAsStringAsync(ct)}");
                        break;
                }

                await Task.Delay(_qbchResponseDelayMs, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau} address: {address}", bureau.Name, $"/dlanswer?id={responseId}");
                redisMsg.SetError("17", "Не удалось установить соединение.").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
            }
            finally
            {
                await _redisCache.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
            }
        }
    }

    private QBCHResult ValidateAnswer(byte[] body, QBCHRequisite bureau, string schemaName, BaseRedisMessage? redisMessage = null, CancellationToken? ct = null)
    {
        var result = new QBCHResult();
        redisMessage?.SetSignedResponse(body);

        if (!_cryptoService.ValidateMsg(body, out var cryptoResult, ct: ct))
        {
            switch (cryptoResult.ErrorCode)
            {
                case 4:
                    result.Error = "УЭП КБКИ некорректна";
                    result.ErrorCode = 4;
                    break;
                case 7:
                    result.Error = "Некорректный формат ответа КБКИ";
                    result.ErrorCode = 7;
                    break;
                default:
                    result.Error = "Ошибка при проверке УЭП";
                    result.ErrorCode = 24;
                    break;
            }

            result.IsError = true;
            return result;
        }

        if (cryptoResult.Body is null)
        {
            result.Error = "Ответ не соответствует схеме";
            result.ErrorCode = 19;
            result.IsError = true;
            return result;
        }

        result.Body = cryptoResult.Body;
        redisMessage?.SetResponseXml(cryptoResult.Body);

        var xsdValidation = _xmlService.ValidateXmlV3(new MemoryStream(cryptoResult.Body), [schemaName, @"xsd\3\qcb_common.xsd"]);
        if (xsdValidation != null && !string.IsNullOrWhiteSpace(xsdValidation.Error))
        {
            result.Error = $"Ответ не соответствует схеме: {xsdValidation.Error}.";
            result.ErrorCode = 19;
            result.IsError = true;
        }

        return result;
    }

    private static ОтветНаЗапросСведений CreateErrorAnswerV3(string psrn, string code, string message, string[] orderNumbers, string requestId, ЗапросСведений request)
    {
        var rows = orderNumbers.Select(order =>
        {
            var kbki = new ОтветНаЗапросСведенийСведенияКБКИ
            {
                ОГРН = psrn,
                ПоСостояниюНа = DateTime.Now,
                ИдентификаторОтвета = requestId,
            };
            kbki.УстановитьОшибку(int.TryParse(code, out var codeValue) ? codeValue : 99, message);

            return new ОтветНаЗапросСведенийСведения
            {
                ПорядковыйНомер = order,
                КБКИ = [kbki]
            };
        }).ToArray();

        return new ОтветНаЗапросСведений
        {
            ИдентификаторЗапроса = request.ИдентификаторЗапроса,
            ИдентификаторОтвета = requestId,
            ОГРН = psrn,
            ТипОтвета = request.ТипЗапроса,
            РежимЗапроса = request.РежимЗапроса,
            ДатаЗапроса = request.ДатаЗапроса.ToString("yyyy-MM-dd"),
            Сведения = rows
        };
    }

    private void FillObligationsSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, bool includeAmp, XElement? ampXml)
    {
        if (!includeAmp)
        {
            return;
        }

        var amp = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИОбязательства>(ToDocument(ampXml));
        if (amp?.БКИ is { Length: > 0 })
        {
            kbki.ДобавитьОбязательства(amp);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияОбязательств();
    }

    private void FillSelfProhibitionSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, XElement? prohibitionXml, ТипИННФЛсПризнаком? inn)
    {
        if (!IsInnVerified(inn))
        {
            kbki.ДобавитьПризнакНепредоставленияСведенийОЗапрете();
            return;
        }

        var prohibition = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИУсловияЗапрета>(ToDocument(prohibitionXml));
        if (prohibition?.Условие is { Length: > 0 })
        {
            kbki.ДобавитьУсловияЗапрета(prohibition);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияСведенийОЗапрете();
    }

    private void FillAntifraudSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, bool includeAntifraud, XElement? antifraudXml)
    {
        if (!includeAntifraud)
        {
            return;
        }

        var antifraud = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИСведенияДляПредупреждения>(ToDocument(antifraudXml));
        if (antifraud?.БКИ is { Length: > 0 })
        {
            kbki.ДобавитьСведенияДляПредупреждения(antifraud);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияАнтифродСведений();
    }

    private static XDocument? ToDocument(XElement? xml) => xml is null ? null : new XDocument(xml);

    private static bool IsInnVerified(ТипИННФЛсПризнаком? inn) =>
        inn is not null &&
        !string.IsNullOrWhiteSpace(inn.Value) &&
        inn.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1;
}