using Cache_lib.Interfaces;
using Crypto_lib.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using Qbch_db_lib.Services.Interfaces;
using qbch_lib;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.Configuration;
using QBCH_lib.domain.aggregate;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces.V3;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
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
    IKeyValueStorageService redisCache,
    IConfiguration config,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : IQBCHServiceV3
{
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ILogger<QBCHServiceV3> _logger = logger;
    private readonly IRepositoryV3 _qbchDb = qbchDb;
    private readonly IKeyValueStorageService _storageService = redisCache;
    private readonly IConfiguration _config = config;
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
    private readonly string _ourBureauPsrn = config.GetValue<string>("Bureau:PSRN") ?? string.Empty;
    private readonly string _ourBureauItn = config.GetValue<string>("Bureau:ITN") ?? string.Empty;
    private readonly int _qbchTicketTimeoutMs = config.GetValue<int>("APIConfiguration:QBCHTicketTimeoutMs", 4000);
    private readonly int _qbchTicketDelayMs = config.GetValue<int>("APIConfiguration:QBCHTicketDelayMs", 1000);
    private readonly int _qbchResponseTimeoutMs = config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs", 10000);
    private readonly int _qbchResponseDelayMs = config.GetValue<int>("APIConfiguration:QBCHResponseDelayMs", 1000);

    /// <summary>
    /// Выполняет обработку запроса на основе данных, полученных из внутренней базы.
    /// </summary>
    /// <param name="transaction">Транзакция с телом запроса и техническим контекстом обработки.</param>
    /// <returns>Результат обработки с ответом <c>ОтветНаЗапросСведений</c>.</returns>
    public async Task<QBCHTaskResult> RequestFromDB(QBCHProcessingTransaction transaction)
    {
        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{transaction.Id}:{_ourBureauPsrn}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        var package = transaction.GetRequest<ЗапросСведений>();

        //NOTE: Убрал лишнюю проверку, такого быть не должно
        //if (package is null)
        //    return new QBCHTaskResult(_ourBureauPsrn);

        var answer = new ОтветНаЗапросСведений
        {
            ИдентификаторЗапроса = package.ИдентификаторЗапроса,
            ИдентификаторОтвета = transaction.Id.ToString(),
            ОГРН = _ourBureauPsrn,
            ТипОтвета = package.ТипЗапроса,
            РежимЗапроса = package.РежимЗапроса,
            ДатаЗапроса = package.ДатаЗапроса.ToString("yyyy-MM-dd")
        };

        var requests = package.Запрос ?? [];
        var timeLeft = _qbchResponseTimeoutMs * requests.Length - transaction.TimeElapsedForValidation.ElapsedMilliseconds;
        _logger.LogDebug("{guid} {bureau}: Таймаут для запросов {timeLeft} ms", transaction.Id, _ourBureauPsrn, timeLeft);

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
                ПоСостояниюНа = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")
            };

            var error = transaction.PackageValidationErrors.FirstOrDefault(x => x.Id.ToString() == requestItem.ПорядковыйНомер);
            if (error is not null)
            {
                kbki.УстановитьОшибку(error.error_code, error.error_message ?? string.Empty);
                response.КБКИ = [kbki];
                responseRows.Add(response);
                continue;
            }

            kbki.ИдентификаторОтвета = transaction.Id.ToString();

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

            var isInnVerified = IsInnVerified(requestItem.Субъект?.ИНН);

            var includeAmp = package.КодСведений == СправочникВидыСведений.Item7;
            var includeAntifraud = package.КодСведений is СправочникВидыСведений.Item7 or СправочникВидыСведений.Item8;

            var pendingTasks = new List<Task<XElement?>>();
            Task<XElement?>? getSelfProhibitionTask = null;
            Task<XElement?>? getAmpTask = null;

            if (subjectKeys.Count != 0)
            {
                getSelfProhibitionTask = isInnVerified ? _qbchDb.GetSelfProhibitionV3(subjectKeys, timeLeft) : null;
                if (getSelfProhibitionTask is not null)
                    pendingTasks.Add(getSelfProhibitionTask);

                getAmpTask = includeAmp ? _qbchDb.GetCalculationOfAmpV3(subjectKeys, timeLeft) : null;
                if (getAmpTask is not null)
                    pendingTasks.Add(getAmpTask);
            }

            var getAntifraudTask = includeAntifraud && isInnVerified
              ? _qbchDb.GetAntifraudV3(requestItem.Субъект!.ДатаРождения, requestItem.Субъект.ИНН!.Value, timeLeft)
              : null;
            if (getAntifraudTask is not null)
                pendingTasks.Add(getAntifraudTask);

            await Task.WhenAll(pendingTasks);

            bool antifraudTaskHasResult = (getAntifraudTask != null && getAntifraudTask.Result != null && getAntifraudTask.Result.Name == "СведенияДляПредупреждения");

            if (subjectKeys.Count == 0 && !antifraudTaskHasResult)
            {
                kbki.ПометитьКакСубъектНеНайден();
                response.КБКИ = [kbki];
                responseRows.Add(response);
                continue;
            }

            FillObligationsSection(kbki, includeAmp, getAmpTask?.Result);
            FillSelfProhibitionSection(kbki, getSelfProhibitionTask?.Result, isInnVerified);
            FillAntifraudSection(kbki, includeAntifraud, getAntifraudTask?.Result, isInnVerified);

            response.КБКИ = [kbki];
            responseRows.Add(response);
        }

        answer.Сведения = responseRows.ToArray();
        return new QBCHTaskResult(_ourBureauPsrn, answer3: answer);
    }

    /// <summary>
    /// Отправляет запрос во внешнее бюро и возвращает итоговый ответ.
    /// </summary>
    /// <param name="transaction ">Транзакция обработки с данными запроса.</param>
    /// <param name="client">HTTP-клиент для взаимодействия с внешним сервисом бюро.</param>
    /// <param name="bureau">Реквизиты целевого бюро кредитных историй.</param>
    /// <returns>Результат обработки, содержащий ответ бюро или информацию об ошибке.</returns>
    public async Task<QBCHTaskResult> RequestFromExternalBureau(QBCHProcessingTransaction transaction, HttpClient client, QBCHRequisite bureau)
    {
        var request = transaction.GetRequest<ЗапросСведений>();

        if (request is null)
            return new QBCHTaskResult(bureau.ogrn);

        var guid = transaction.Id.ToString();
        var orderNumbers = request.Запрос?.Select(x => x.ПорядковыйНомер).ToArray() ?? [];

        await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{guid}:{bureau.ogrn}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        var externalRequest = new ЗапросСведений
        {
            Абонент = new ЗапросСведенийАбонент
            {
                Item = new ЗапросСведенийАбонентЮридическоеЛицо
                {
                    ИНН = _ourBureauItn,
                    ОГРН = _ourBureauPsrn
                }
            },
            ДатаЗапроса = request.ДатаЗапроса,
            Версия = request.Версия,
            ИдентификаторЗапроса = guid,
            КодСведений = request.КодСведений,
            РежимЗапроса = request.РежимЗапроса,
            ТипЗапроса = СправочникСпособыЗапроса.Item1,
            Запрос = request.Запрос
        };

        var dlrequestBytes = _xmlService.SerializeAsByteV3(externalRequest);
        var signedDlrequestBytes = _cryptoService.SignMsg(dlrequestBytes);
        var dlrequestContent = new ByteArrayContent(signedDlrequestBytes);

        using var ticketCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_qbchTicketTimeoutMs));
        //NOTE: Убрал Math.Max. На работу это не влияло, но смысл этого для меня большая загадка. Единственный вариант, это не доверие тому, кто будет заполнять конфиг.
        using var ticketCheckCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_qbchTicketTimeoutMs - 1000));
        var ticketTimer = Stopwatch.StartNew();

        ОтветНаЗапросСведений? dlrequestResult = null;
        Результат? ticket = null;

        // Статус и текст последнего ответа: сам HttpResponseMessage освобождается через using
        // внутри итерации цикла, поэтому нужные за ее пределами поля копируются в переменные.
        HttpStatusCode? lastStatusCode = null;
        string? lastResponseText = null;

        var redisMsg = DlRequestRedisMessage.Create(DateTime.Now, signedDlrequestBytes, dlrequestBytes);

        try
        {
            //NOTE: Извиняюсь, но у меня нет времени анализировать правки логики одного из самых сложных и важных мест кода, где в третьей версии не должно быть изменений, кроме версии формата пересылаемых сообщений.
            //Поэтому здесь я просто вернул код Артема.
            dlrequestResult = await Task.Run(async Task<ОтветНаЗапросСведений?>? () =>
            {
                while (true)
                {
                    redisMsg = DlRequestRedisMessage.Create(DateTime.Now, signedDlrequestBytes, dlrequestBytes);
                    try
                    {
                        //NOTE: Куда-то исчезло все логирование, что странно так как именно здесь оно лишним никогда не будет.
                        _logger.LogDebug("{guid} {Bureau}: dlrequest send {dt}", guid, bureau.ogrn!, DateTime.Now);
                        using var responseMessage = await client.PostAsync("dlrequest", dlrequestContent, ticketCts.Token);
                        lastStatusCode = responseMessage.StatusCode;
                        lastResponseText = await responseMessage.Content.ReadAsStringAsync(ticketCts.Token);
                        using var ms = new MemoryStream();
                        await responseMessage.Content.CopyToAsync(ms, ticketCts.Token);
                        redisMsg.SetResponseCode(responseMessage.StatusCode).SetResponseTime(DateTime.Now);

                        _logger.LogDebug("{guid} {Bureau}: Status {Status}", guid, bureau.ogrn!, (int?)responseMessage.StatusCode);

                        switch (responseMessage.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                var answerValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_answer.xsd", redisMsg, ticketCheckCts.Token);
                                if (answerValidation.IsError)
                                {
                                    //NOTE: Возвратил пропавший код Артема
                                    _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, answerValidation.Error);
                                    redisMsg.SetError(answerValidation.ErrorCode.ToString(), answerValidation.Error!).SetResponseTime(DateTime.Now);
                                    await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                    dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, answerValidation.ErrorCode.ToString(), answerValidation.Error ?? "Ошибка валидации", orderNumbers);
                                    break;
                                }

                                //NOTE: Возвратил пропавший код Артема
                                _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                                redisMsg.SetSignedResponse(ms.ToArray()).SetResponseXml(answerValidation.Body).SetResponseTime(DateTime.Now);
                                dlrequestResult = _xmlService.DeserializeV3<ОтветНаЗапросСведений>(answerValidation.Body);
                                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{guid}:{bureau.ogrn}", "response_id", dlrequestResult?.ИдентификаторОтвета ?? "-");
                                break;

                            case HttpStatusCode.BadRequest:
                                var badValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ticketCheckCts.Token);
                                if (badValidation.IsError)
                                {
                                    //NOTE: Возвратил пропавший код Артема
                                    _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, badValidation.Error);
                                    redisMsg.SetError(badValidation.ErrorCode.ToString(), badValidation.Error!).SetResponseTime(DateTime.Now);
                                    await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                    dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, badValidation.ErrorCode.ToString(), badValidation.Error ?? "Ошибка валидации", orderNumbers);
                                    break;
                                }
                                _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                                var badTicket = _xmlService.DeserializeV3<Результат>(badValidation.Body);
                                //NOTE: Возвратил пропавший код Артема. И безусловное приведение вместо сравнения, это ошибка. 
                                if (badTicket?.Item is ТипОшибка badError)
                                {
                                    redisMsg.SetError(badError.Код ?? "-", badError.Value ?? "-").SetResponseTime(DateTime.Now);
                                    dlrequestResult = CreateErrorAnswerV3(badTicket?.ОГРН ?? bureau.ogrn!, badError.Код ?? "99", badError.Value ?? "Ошибка", orderNumbers);
                                }
                                else
                                {
                                    redisMsg.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                                    dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers);
                                }

                                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                break;

                            case HttpStatusCode.Accepted:
                                var ticketValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ticketCheckCts.Token);
                                if (ticketValidation.IsError)
                                {
                                    _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, ticketValidation.Error);
                                    redisMsg.SetError(ticketValidation.ErrorCode.ToString(), ticketValidation.Error!).SetResponseTime(DateTime.Now);
                                    await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                    dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, ticketValidation.ErrorCode.ToString(), ticketValidation.Error ?? "Ошибка валидации", orderNumbers);
                                    break;
                                }
                                _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                                ticket = _xmlService.DeserializeV3<Результат>(ticketValidation.Body);

                                //NOTE: Возвратил пропавший код Артема
                                if (ticket?.Item is РезультатИдентификаторОтвета)
                                {
                                    _logger.LogDebug("{guid} {Bureau}: Ticket", guid, bureau.ogrn);
                                    redisMsg.SetSignedResponse(ms.ToArray()).SetResponseXml(ticketValidation.Body).SetResponseTime(DateTime.Now);
                                }
                                else
                                {
                                    _logger.LogDebug("{guid} {Bureau}: Непредвиденные данные в ответе КБКИ", guid, bureau.ogrn);
                                    redisMsg.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                                    dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers);
                                }

                                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                return null;

                            default:
                                redisMsg.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{lastResponseText}");
                                // NOTE: Возвратил пропавший код Артема
                                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                break;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau} address: {address}", bureau.Name, "/dlrequest");
                        //NOTE: Возвратил пропавший код Артема
                        redisMsg.SetError("17", "Не удалось установить соединение.").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now); ;
                        await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                    }
                    //NOTE: Возвратил пропавший код Артема
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка получения ответа от КБКИ: {bureau}  address: {address}", bureau.Name, "/dlrequest");
                        redisMsg.SetError("99", $"Код ответа: {lastStatusCode} Message:{lastResponseText ?? string.Empty}").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now);
                        await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                    }
                    //NOTE: Закомментил непонятный по логике код
                    //finally
                    //{
                    //    await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                    //}

                    //NOTE: Привел код в соответствие коду Артема
                    if (dlrequestResult is not null)
                        return dlrequestResult;

                    await Task.Delay(_qbchTicketDelayMs, ticketCts.Token);

                }
            }).WaitAsync(ticketCts.Token);
        }
        catch (TaskCanceledException ex)
        {
            //NOTE: Возвратил пропавший код Артема
            _logger.LogWarning(ex, "Запрос {guid} в бюро {bureauName} по адресу {baseAddress} был отменен по истечению таймаута {timeout}.", guid, bureau.Name, "/dlrequest", _qbchTicketTimeoutMs);
            redisMsg.SetError("18", "Время ожидания ответа истекло.").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now);
            await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
            dlrequestResult = CreateErrorAnswerV3(bureau.ogrn!, "18", "Время ожидания ответа истекло.", orderNumbers);
        }

        if (dlrequestResult is not null)
        {
            return new QBCHTaskResult(bureau.ogrn!, answer3: dlrequestResult);
        }

        //NOTE: Привел код в соответствие к коду Артема
        _logger.LogDebug("{guid} {Bureau}: РежимЗапроса {req}", guid, bureau.ogrn!, transaction.ClentRequest.Request?.РежимЗапроса);

        /* Таймауты для пакетных и непакетных запросов отличаются.
         * Таймауты для пакета считаются как n запросов * 10 секунд.
         */
        var timeLeftMs = _qbchResponseTimeoutMs * request.Запрос!.Length - ticketTimer.ElapsedMilliseconds - transaction.TimeElapsedForValidation.ElapsedMilliseconds;



        DlAnswerRedisMessage DLAnswerRedisMessage = DlAnswerRedisMessage.Create();

        // Ответ на запрос метода dlanswer
        ОтветНаЗапросСведений? dlanswerResult = null;
        string? responseId = string.Empty;

        try
        {
            if (timeLeftMs <= 0)
            {
                throw new Exception("После получения тикета времени на получение ответа не осталось");
            }
            using var resendCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeLeftMs));
            dlanswerResult = await Task.Run(async Task<ОтветНаЗапросСведений?>? () =>
            {
                if (ticket?.Item is РезультатИдентификаторОтвета результатИдентификаторОтвета)
                {
                    responseId = результатИдентификаторОтвета.Value;
                    await _storageService.AddHash(RedisConstants.DlRequestV3Scope, $"{guid}:{bureau.ogrn}", "response_id", responseId ?? "-");
                    dlanswerResult = await ResendDlanswer(responseId!, client, bureau, request.ИдентификаторЗапроса!, resendCts.Token, orderNumbers);
                }
                else
                {
                    DLAnswerRedisMessage.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                    dlanswerResult = CreateErrorAnswerV3(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", orderNumbers);
                    await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                }

                return dlanswerResult;
            }).WaitAsync(resendCts.Token);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Таймаут запроса в бюро {bureauName} по адресу {baseAddress}.", bureau.Name, $"/dlanswer?id={responseId}");
            DLAnswerRedisMessage = DlAnswerRedisMessage.Create();
            DLAnswerRedisMessage.SetError("18", "Время ожидания ответа истекло.").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now);
            dlanswerResult = CreateErrorAnswerV3(bureau.ogrn!, "18", "Время ожидания ответа истекло.", orderNumbers);
            await _storageService.ListSet(key: [DLAnswerRedisMessage.Name, guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
        }

        _logger.LogDebug("{guid} {Bureau}: dlanswer response {dt}", guid, bureau.ogrn!, DateTime.Now);

        return new QBCHTaskResult(bureau.ogrn!, answer3: dlanswerResult);

    }

    private async Task<ОтветНаЗапросСведений> ResendDlanswer(string responseId, HttpClient client, QBCHRequisite bureau, string guid, CancellationToken ct, string[] orderNumbers)
    {
        // Статус и текст последнего ответа: сам HttpResponseMessage освобождается через using
        // внутри итерации цикла, поэтому нужные за ее пределами поля копируются в переменные.
        HttpStatusCode? lastStatusCode = null;
        string? lastResponseText = null;

        while (true)
        {
            var redisMsg = DlAnswerRedisMessage.Create();
            try
            {
                //NOTE: Вернул логирование Артема
                _logger.LogDebug("{guid} {Bureau}: dlanswer send {dt}", guid, bureau.ogrn!, DateTime.Now);
                using var responseMessage = await client.GetAsync($"dlanswer?id={responseId}", ct);
                lastStatusCode = responseMessage.StatusCode;
                lastResponseText = await responseMessage.Content.ReadAsStringAsync(ct);
                using var ms = new MemoryStream();
                await responseMessage.Content.CopyToAsync(ms, ct);

                redisMsg.SetResponseCode(responseMessage.StatusCode).SetResponseTime(DateTime.Now);
                _logger.LogDebug("{guid} {Bureau}: Status {Status}", guid, bureau.ogrn!, (int?)responseMessage.StatusCode);

                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var okValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_answer.xsd", redisMsg, ct);
                        if (okValidation.IsError)
                        {
                            //NOTE: Возвратил пропавший код Артема
                            _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, okValidation.Error);
                            redisMsg.SetError(okValidation.ErrorCode.ToString(), okValidation.ErrorMessage).SetResponseTime(DateTime.Now);
                            await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                            return CreateErrorAnswerV3(bureau.ogrn!, okValidation.ErrorCode.ToString(), okValidation.Error ?? "Ошибка валидации", orderNumbers);
                        }

                        _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                        await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));

                        var answer = _xmlService.DeserializeV3<ОтветНаЗапросСведений>(okValidation.Body);
                        //NOTE: У Артема эта обработка отсутствует, но, имхо, она допустима
                        return answer ?? CreateErrorAnswerV3(bureau.ogrn!, "19", "Ответ не соответствует схеме", orderNumbers);

                    case HttpStatusCode.Accepted:
                    case HttpStatusCode.BadRequest:
                        var ticketValidation = ValidateAnswer(ms.ToArray(), bureau, @"xsd\3\qcb_result.xsd", redisMsg, ct);
                        if (ticketValidation.IsError)
                        {
                            _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, ticketValidation.Error);
                            redisMsg.SetError(ticketValidation.ErrorCode.ToString(), ticketValidation.ErrorMessage).SetResponseTime(DateTime.Now);
                            await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                            return CreateErrorAnswerV3(bureau.ogrn!, ticketValidation.ErrorCode.ToString(), ticketValidation.Error ?? "Ошибка валидации", orderNumbers);
                        }

                        _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                        var ticket = _xmlService.DeserializeV3<Результат>(ticketValidation.Body);
                        //NOTE: Возвратил пропавший код Артема. И безусловное приведение вместо сравнения, это ошибка. 
                        if (ticket?.Item is ТипОшибка ticketError)
                        {
                            _logger.LogDebug("{guid} {Bureau}: Error code {code} value {value}", guid, bureau.ogrn!, ticketError.Код, ticketError.Value);
                            redisMsg.SetError(ticketError.Код, ticketError.Value).SetResponseTime(DateTime.Now);

                            if (ticketError.Код != "12")
                            {
                                await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                                return CreateErrorAnswerV3(ticket?.ОГРН ?? bureau.ogrn!, ticketError.Код ?? "99", ticketError.Value ?? "Ошибка", orderNumbers);
                            }
                        }
                        else
                        {
                            //NOTE: Возвратил пропавший код Артема.
                            redisMsg.SetError("99", "Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа.").SetResponseTime(DateTime.Now);
                            _logger.LogError("Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа. {Bureau}", bureau.Name);
                            await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                            return CreateErrorAnswerV3(bureau.ogrn!, "99", "Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа.", orderNumbers);
                        }
                        await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                        break;

                    default:
                        //NOTE: Возвратил пропавший код Артема.
                        redisMsg.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{lastResponseText}").SetResponseTime(DateTime.Now);
                        await _storageService.ListSet(key: [RedisConstants.DlRequestV3Scope, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
                        break;
                }

                await Task.Delay(_qbchResponseDelayMs, ct);
            }
            //NOTE: Возвратил пропавший код Артема.
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau} address: {address}", bureau.Name, $"/dlanswer?id={responseId}");
                redisMsg.SetError("17", "Не удалось установить соединение.").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now);
                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка получения ответа от КБКИ: {bureau}  address: {address}", bureau.Name, $"/dlanswer?id={responseId}");
                redisMsg.SetError("99", $"Код ответа: {lastStatusCode} Message:{lastResponseText ?? string.Empty}").SetResponseCode(lastStatusCode).SetResponseTime(DateTime.Now);
                await _storageService.ListSet(key: [redisMsg.Name, guid, bureau.ogrn!, redisMsg.Name], value: JsonSerializer.Serialize(redisMsg));
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
                //NOTE: Вернул логирование
                case 4:
                    result.Error = "УЭП КБКИ некорректна";
                    result.ErrorCode = 4;
                    _logger.LogError("УЭП КБКИ некорректна {bureauName}.", bureau.Name);
                    break;
                case 7:
                    result.Error = "Некорректный формат ответа КБКИ";
                    result.ErrorCode = 7;
                    _logger.LogError("Некорректный формат ответа КБКИ {name}.", bureau.Name);
                    break;
                default:
                    result.Error = "Ошибка при проверке УЭП";
                    result.ErrorCode = 24;
                    _logger.LogError("Неопознанная ошибка криптографии {cryptoResult.ErrorCode}", cryptoResult.ErrorCode);
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
            //NOTE: Вернул логирование
            _logger.LogError("Ответ не соответствует схеме {bureauName}.", bureau.Name);
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
            //NOTE: Вернул логирование
            _logger.LogError("Ответ не соответствует схеме в бюро {bureauName}. XSD_Error:{xsd}", bureau.Name, xsdValidation?.Error);
        }

        return result;
    }

    private static ОтветНаЗапросСведений CreateErrorAnswerV3(string psrn, string code, string message, string[] orderNumbers)
    {
        //NOTE: Привел ответ в соответствие ответу Артема
        var rows = orderNumbers.Select(order =>
        {
            var kbki = new ОтветНаЗапросСведенийСведенияКБКИ
            {
                ОГРН = psrn,
                ПоСостояниюНа = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")
                //ИдентификаторОтвета = requestId,
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
            //ИдентификаторЗапроса = request.ИдентификаторЗапроса,
            //ИдентификаторОтвета = requestId,
            ОГРН = psrn,
            ТипОтвета = СправочникСпособыЗапроса.Item1,
            РежимЗапроса = СправочникРежимыЗапроса.Item1,
            //ДатаЗапроса = request.ДатаЗапроса.ToString("yyyy-MM-dd"),
            Сведения = rows
        };
    }

    private void FillObligationsSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, bool includeAmp, XElement? ampXml)
    {
        if (!includeAmp)
        {
            return;
        }

        if (ampXml == null || ampXml.Name == "ОбязательствНет")
        {
            kbki.ДобавитьПризнакОтсутствияОбязательств();
            return;
        }

        var amp = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИОбязательства>(ampXml);

        if (amp?.БКИ is { Length: > 0 })
        {
            kbki.ДобавитьОбязательства(amp);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияОбязательств();
    }
    private void FillSelfProhibitionSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, XElement? prohibitionXml, bool isInnVerified)
    {
        if (!isInnVerified)
        {
            kbki.ДобавитьПризнакНепредоставленияСведенийОЗапрете();
            return;
        }

        if (prohibitionXml == null || prohibitionXml.Name == "СведенийОЗапретеНет")
        {
            kbki.ДобавитьПризнакОтсутствияСведенийОЗапрете();
            return;
        }

        var prohibition = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИУсловияЗапрета>(prohibitionXml);

        if (prohibition?.Условие is { Length: > 0 })
        {
            kbki.ДобавитьУсловияЗапрета(prohibition);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияСведенийОЗапрете();
    }

    private void FillAntifraudSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, bool includeAntifraud, XElement? antifraudXml, bool isInnVerified)
    {
        if (!includeAntifraud)
        {
            return;
        }

        if (!isInnVerified)
        {
            kbki.ДобавитьПризнакНепредоставленияАнтифродСведений();
            return;
        }

        if (antifraudXml == null || antifraudXml.Name == "СведенийДляПредупрежденияНет")
        {
            kbki.ДобавитьПризнакОтсутствияАнтифродСведений();
            return;
        }

        var antifraud = _xmlService.DeserializeV3<ОтветНаЗапросСведенийСведенияКБКИСведенияДляПредупреждения>(antifraudXml);

        if (antifraud?.БКИ is { Length: > 0 })
        {
            kbki.ДобавитьСведенияДляПредупреждения(antifraud);
            return;
        }

        kbki.ДобавитьПризнакОтсутствияАнтифродСведений();
    }

    private static bool IsInnVerified(ТипИННФЛсПризнаком? inn) =>
        inn is not null &&
        !string.IsNullOrWhiteSpace(inn.Value) &&
        inn.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1;
}