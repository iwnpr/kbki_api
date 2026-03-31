using Cache_lib.Interfaces;
using Crypto_lib.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.CommonTypes;
using QBCH_lib.qcb_xml.v1_3.Enums;
using QBCH_lib.qcb_xml.v1_3.qcb_answer;
using QBCH_lib.qcb_xml.v1_3.qcb_request;
using QBCH_lib.qcb_xml.v1_3.qcb_result;
using QBCHService_lib.Models;
using QBCHService_lib.Services.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using XmlService_lib.Services.Interfaces;

namespace QBCHService_lib.Services.Implementations;

/// <summary>
/// Сервис ССП
/// </summary>
public partial class QBCHService : IQBCHService
{
    private readonly ICryptoService _cryptoService;
    private readonly IXmlService _xmlService;
    private readonly ILogger<QBCHService> _logger;
    private readonly IRepository _qbchDb;
    private readonly ICacheService _redisCache;
    private readonly IConfiguration _config;
    private readonly string? _OurBureauPSRN;
    private readonly string? _OurBureauITN;
    private readonly int _QBCHTicketTimeoutMs;
    private readonly int _QBCHTicketDelayMs;
    private readonly int _QBCHResponseTimeoutMs;
    private readonly int _QBCHResponseDelayMs;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="cryptoService">Сервис криптографии</param>
    /// <param name="xmlService">Сервис xml</param>
    /// <param name="logger"></param>
    /// <param name="qbchDb"></param>
    /// <param name="redisCache"></param>
    /// <param name="config"></param>
    public QBCHService(ICryptoService cryptoService, IXmlService xmlService, ILogger<QBCHService> logger, IRepository qbchDb, ICacheService redisCache, IConfiguration config)
    {
        _cryptoService = cryptoService;
        _xmlService = xmlService;
        _logger = logger;
        _qbchDb = qbchDb;
        _redisCache = redisCache;
        _config = config;
        _OurBureauPSRN = _config.GetValue<string>("Bureau:PSRN");
        _OurBureauITN = _config.GetValue<string>("Bureau:ITN");
        _QBCHTicketTimeoutMs = _config.GetValue<int>("APIConfiguration:QBCHTicketTimeoutMs");
        _QBCHTicketDelayMs = _config.GetValue<int>("APIConfiguration:QBCHTicketDelayMs");
        _QBCHResponseTimeoutMs = _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs");
        _QBCHResponseDelayMs = _config.GetValue<int>("APIConfiguration:QBCHResponseDelayMs");
    }

    /// <summary>
    /// Сведения о платежах из нашей БД
    /// </summary>
    /// <param name="request">Запрос в формет xml</param>
    /// <param name="guid"></param>
    /// <returns></returns>
    public async Task<QBCHTaskResult> AmpFromDB(ЗапросСведенийОПлатежах request, string guid)
    {
        await _redisCache.AddHash("dlrequest", $"{guid}:{_OurBureauPSRN}", "StartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        // Проверка субъектов не найден
        var xRequest = await _xmlService.SerializeAsXDocumentAsync(request);

        await _redisCache.AddHash("dlrequest", $"{guid}:{_OurBureauPSRN}", "RequestXml", xRequest.ToString());
        await _redisCache.AddHash("dlrequest", $"{guid}:{_OurBureauPSRN}", "DlRequestStartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        List<long> subjectKeys = await _qbchDb.GetSearchAllSubjects_old(xRequest.ToString());
        bool subjectHas = subjectKeys.Count != 0;
        QBCH_lib.qcb_xml.v1_3.qcb_answer.Обязательства? ampFromDB = null;
        QBCH_lib.qcb_xml.v1_3.qcb_answer.СубъектНеНайден? noSubject = null;
        QBCH_lib.qcb_xml.v1_3.qcb_answer.ОбязательствНет? noAmp = null;

        if (!subjectHas)
        {
            noSubject = new();
        }
        else if (subjectHas)
        {
            var xAmpDB = await _qbchDb.GetCalculationOfAmp_old(subjectKeys);
            ampFromDB = _xmlService.Deserialize<QBCH_lib.qcb_xml.v1_3.qcb_answer.Обязательства>(xAmpDB);
            noAmp = _xmlService.Deserialize<QBCH_lib.qcb_xml.v1_3.qcb_answer.ОбязательствНет>(xAmpDB);

            if (ampFromDB is null && noAmp is null)
            {
                noAmp = new QBCH_lib.qcb_xml.v1_3.qcb_answer.ОбязательствНет();
            }
        }


        await _redisCache.AddHash("dlrequest", $"{guid}:{_OurBureauPSRN}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        return new(_OurBureauPSRN, new СведенияОПлатежах
        {
            Версия = "1.2",
            ИдентификаторЗапроса = request.ИдентификаторЗапроса,
            ИдентификаторОтвета = guid,
            ОГРН = _OurBureauPSRN,
            ТипОтвета = request.ТипЗапроса == ЗапросСведенийОПлатежахТипЗапроса.OurBureau ? "1" : "2",
            ТитульнаяЧасть = request.Запрос?.Субъект,
            КБКИ =
            [
                new()
                {
                    ОГРН = _OurBureauPSRN,
                    ПоСостояниюНа = DateTime.Now,
                    СубъектНеНайден = noSubject,
                    ОбязательствНет = noAmp,
                    Обязательства = ampFromDB,
                    ИдентификаторОтвета = guid
                }
            ]
        });
    }

    /// <summary>
    /// Запрос ССП
    /// </summary>
    /// <param name="guid">Id запроса</param>
    /// <param name="request">Запрос</param>
    /// <param name="client"></param>
    /// <param name="resendTimeout"></param>
    /// <param name="bureau"></param>
    /// <param name="IsOldVersion"></param>
    /// <returns></returns>
    public async Task<QBCHTaskResult> AmpRequest(string guid, ЗапросСведенийОПлатежах request, HttpClient client, long resendTimeout, QBCHRequisite bureau, bool IsOldVersion = false)
    {
        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "StartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        // Заменяем реквизиты абонента и считываем тип запроса.
        request.ИдентификаторЗапроса = guid;
        request.Абонент = new()
        {
            ЮридическоеЛицо = new()
            {
                ИНН = _OurBureauITN,
                ОГРН = _OurBureauPSRN
            }
        };
        request.ТипЗапроса = ЗапросСведенийОПлатежахТипЗапроса.OurBureau;

        var xmlBytes = _xmlService.SerializeAsByte(request);
        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "RequestXml", Encoding.UTF8.GetString(xmlBytes));
        var signdXmlBytes = _cryptoService.SignMsg(xmlBytes);

        // Добавляем файл с запросом в запрос
        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "SignedRequest", signdXmlBytes);
        var content = new ByteArrayContent(signdXmlBytes);

        // Отправляем запрос
        HttpResponseMessage? responseMessage = null;

        using var ms = new MemoryStream();

        // Токен ограниченный таймаутом для перезапроса тикетов.
        var ticketCts = new CancellationTokenSource();
        ticketCts.CancelAfter(TimeSpan.FromMilliseconds(_QBCHTicketTimeoutMs));
        var ticketCPCts = new CancellationTokenSource();
        ticketCPCts.CancelAfter(TimeSpan.FromMilliseconds(_QBCHTicketTimeoutMs - 1000));

        СведенияОПлатежах? resendResult = null;
        СведенияОПлатежах? ticketAnswer = null;
        var ticketTime = Stopwatch.StartNew();

        /* dlrequest в КБКИ - запрос тикета или ответа.
         * Перезапрос с таймаутом в 4 секунды.
         * Задача ограничена CancellationToken по таймауту.
         * По окончанию таймаута возвращается ошибка => "Время ожидания ответа истекло"
         */
        try
        {
            ticketAnswer = await Task.Run(async Task<СведенияОПлатежах?>? () =>
            {
                int tickerResendCount = 0;
                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "DlRequestStartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                while (true)
                {
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "TicketResendCount", tickerResendCount++.ToString());
                    try
                    {
                        _logger.LogInformation("Send request for ticket {BureaName}", bureau.Name);
                        responseMessage = await client.PostAsync("dlrequest", content, ticketCts.Token);
                        await responseMessage.Content.CopyToAsync(ms);

                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "SignedQBCHTicket", ms.ToArray());

                        // Проверка кода ответа на сообщение
                        if (responseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            if (!ValidateAnswer(ms.ToArray(), bureau, [@"xsd\1.3\qcb_answer.xsd", @"xsd\1.3\qcb_common.xsd"], guid, out var vaildationresult, ticketCPCts.Token))
                            {
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", vaildationresult.ErrorCode.ToString());
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", vaildationresult.Error!);
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                                return СведенияОПлатежах.CreateError(bureau.ogrn!, vaildationresult.ErrorCode.ToString(), vaildationresult.Error!);
                            }
                            return _xmlService.Deserialize<СведенияОПлатежах>(vaildationresult.Body);
                        }
                        if (responseMessage.StatusCode == HttpStatusCode.Accepted)
                            break;
                        else if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                        {
                            if (!ValidateAnswer(ms.ToArray(), bureau, [@"xsd\1.3\qcb_result.xsd", @"xsd\1.3\qcb_common.xsd"], guid, out var vaildationresult, ticketCPCts.Token))
                            {
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", vaildationresult.ErrorCode.ToString());
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", vaildationresult.Error!);
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                                return СведенияОПлатежах.CreateError(bureau.ogrn!, vaildationresult.ErrorCode.ToString(), vaildationresult.Error!);
                            }

                            var ticket = _xmlService.Deserialize<Результат>(vaildationresult.Body);
                            if (ticket?.Item is Ошибка error)
                            {
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", error.Код ?? "-");
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", error.Value ?? "-");
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                                return СведенияОПлатежах.CreateError(ticket?.ОГРН ?? bureau.ogrn!, error);
                            }
                        }
                        else
                        {
                            await Task.Delay(_QBCHTicketDelayMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ticketCts.IsCancellationRequested)
                        {
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "18");
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", ex.Message);
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                            return СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло");
                        }
                        _logger.LogWarning("Ex:{Exception}. Ошибка запроса в бюро {bureauName} по адресу {baseAddress}. Переотправка через 1 секунду.", ex.Message, bureau.Name, "dlrequest");

                    }
                }
                return null;
            }).WaitAsync(ticketCts.Token);
        }
        // Ошибка таймаута для запросов тикета
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Превышено время ожидания ответа от КБКИ {bureauName} по адресу {baseAddress}", bureau.Name, "/dlrequest");
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "18");
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", ex.Message);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            return new(bureau.ogrn, СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло"));
        }
        // Ошибка соединения, иные ошибки
        catch (Exception ex)
        {
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "17");
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", ex.Message);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            _logger.LogCritical(ex, "Ошибка запроса в бюро {bureauName} по адресу {baseAddress}.", bureau.Name, "/dlrequest");
            return new(bureau.ogrn, СведенияОПлатежах.CreateError(bureau.ogrn!, "17", "Не удалось установить соединение"));
        }

        // Ответ по тикету не пустой
        if (ticketAnswer is not null)
        {
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "SignedTicketResponse", ms.ToArray());
            return new(bureau.ogrn, ticketAnswer);
        }

        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "TicketResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
        await (await responseMessage!.Content.ReadAsStreamAsync()).CopyToAsync(ms);
        string? responseId = string.Empty;


        // Токен ограниченный таймаутом для перезапроса ответа по тикету.
        var resendCts = new CancellationTokenSource();
        var resendCpCts = new CancellationTokenSource();


        resendCts.CancelAfter(TimeSpan.FromMilliseconds(resendTimeout - ticketTime.ElapsedMilliseconds));
        resendCpCts.CancelAfter(TimeSpan.FromMilliseconds(resendTimeout - ticketTime.ElapsedMilliseconds - 1000));

        /* Переотправка запроса в случае получения
         * тикета в ответ на запрос dlrequest.
         * Ограничение по времени на задачу задано
         * с помощью таймаута в CancellationToken.
         */
        try
        {
            var ticketResend = await Task.Run(async Task<СведенияОПлатежах?>? () =>
            {
                // Пришел ответ с СМП
                if (responseMessage.StatusCode == HttpStatusCode.Accepted)
                {
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "DlAnswerStartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

                    // Если получен тикет, перезапрашиваем до получения ответа или ошибки
                    if (!ValidateAnswer(ms.ToArray(), bureau, [@"xsd\1.3\qcb_result.xsd", @"xsd\1.3\qcb_common.xsd"], guid, out var vaildationresult, resendCpCts.Token))
                    {
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", vaildationresult.ErrorCode.ToString());
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", vaildationresult.Error ?? "-");
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        return СведенияОПлатежах.CreateError(bureau.ogrn!, vaildationresult.ErrorCode.ToString(), vaildationresult.Error!);
                    }

                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "TicketBody", vaildationresult.Body);

                    var ticket = _xmlService.Deserialize<Результат>(vaildationresult.Body);

                    if (ticket?.Item is РезультатИдентификаторОтвета response)
                    {
                        responseId = response.Value;

                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "TicketId", responseId ?? "-");

                        try
                        {
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResendStartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                            resendResult = await ResendRequest(responseId!, client, bureau, request.ИдентификаторЗапроса!, resendCts.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Запрос был отменен");
                        }
                    }
                    else if (ticket?.Item is Ошибка error)
                    {
                        if (error.Value == "Ответ не готов")
                        {
                            try
                            {
                                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResendStartTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                                resendResult = await ResendRequest(responseId, client, bureau, request.ИдентификаторЗапроса!, resendCts.Token);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Запрос был отменен");
                            }
                        }
                        // КБКИ вернул ошибку, пишем ее в ответ
                        else
                        {
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                            resendResult = СведенияОПлатежах.CreateError(ticket?.ОГРН ?? bureau.ogrn!, error);
                        }
                    }
                }

                if (resendResult is not null)
                {
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                    return resendResult;
                }
                else
                {
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "18");
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", "Время ожидания ответа истекло");
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                    return СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло");
                }

            }).WaitAsync(resendCts.Token);
            if (ticketResend is not null)
            {
                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                return new(bureau.ogrn, ticketResend);
            }
        }
        // Ошибка таймаута для получения ответа с перезаросами
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Время ожидания ответа истекло в бюро {bureauName} по адресу {baseAddress}. ResponseId {responseId}", bureau.Name, "/dlanswer", responseId);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", ex.Message);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            return new(bureau.ogrn, СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Http exception");
        }
        // Ошибка соединения, иные ошибки
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка запроса в бюро {bureauName} по адресу {baseAddress}. ResponseId {responseId}", bureau.Name, "/dlanswer", responseId);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", ex.Message);
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            return new(bureau.ogrn, СведенияОПлатежах.CreateError(bureau.ogrn!, "17", "Не удалось установить соединение"));
        }

        _logger.LogWarning("Время ожидания ответа истекло в бюро {bureauName} по адресу {baseAddress}. ResponseId {responseId}", bureau.Name, "/dlanswer", responseId);
        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "18");
        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", "Время ожидания ответа истекло");
        return new(bureau.ogrn, СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло"));
    }

    /// <summary>
    /// Перезапрос данных по тикету
    /// </summary>
    /// <param name="responseId"></param>
    /// <param name="client"></param>
    /// <param name="bureau"></param>
    /// <param name="guid"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<СведенияОПлатежах?> ResendRequest(string responseId, HttpClient client, QBCHRequisite bureau, string guid, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        HttpResponseMessage responseMessage;
        Результат? ticket;

        int resend = 0;

        while (true)
        {
            try
            {
                // Таймаут для перезапросов
                if (ct.IsCancellationRequested)
                {
                    _logger.LogError("Превышено время ожидания ответа (с учетом переотправки) от Бюро {bureauName} по адресу {baseAddress}", bureau.Name, $"/dlanswer?id={responseId}");
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", "18");
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", "Время ожидания ответа истекло");
                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                    return СведенияОПлатежах.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло");
                }

                await ms.FlushAsync(ct);
                ms.Position = 0;
                await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "AnswerResendCount", resend++.ToString());
                responseMessage = await client.GetAsync($"dlanswer?id={responseId}", ct);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    // Если ответ получен, возвращаем результат из метода
                    await (await responseMessage.Content.ReadAsStreamAsync(ct)).CopyToAsync(ms, ct);

                    if (!ValidateAnswer(ms.ToArray(), bureau, [@"xsd\1.3\qcb_answer.xsd", @"xsd\1.3\qcb_common.xsd"], guid, out var vaildationresult, ct))
                    {
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", vaildationresult.ErrorCode.ToString());
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", vaildationresult.Error!);
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        return СведенияОПлатежах.CreateError(bureau.ogrn!, vaildationresult.ErrorCode.ToString(), vaildationresult.Error!);
                    }

                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                    return _xmlService.Deserialize<СведенияОПлатежах>(vaildationresult.Body);
                }
                else if (responseMessage.StatusCode == HttpStatusCode.Accepted || responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Если получаем тикет
                    await (await responseMessage.Content.ReadAsStreamAsync(ct)).CopyToAsync(ms, ct);

                    if (!ValidateAnswer(ms.ToArray(), bureau, [@"xsd\1.3\qcb_result.xsd", @"xsd\1.3\qcb_common.xsd"], guid, out var vaildationresult, ct))
                    {
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", vaildationresult.ErrorCode.ToString());
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", vaildationresult.Error!);
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                        return СведенияОПлатежах.CreateError(bureau.ogrn!, vaildationresult.ErrorCode.ToString(), vaildationresult.Error!);
                    }
                    ticket = _xmlService.Deserialize<Результат>(vaildationresult.Body);

                    await Task.Delay(_QBCHResponseDelayMs, ct);
                    if (ticket?.Item is Ошибка error)
                    {
                        if (error.Value == "Ответ не готов")
                        {
                            _logger.LogInformation("Запрос в бюро {bureauName} по адресу {baseAddress}. Переотправка через 1 секунду.", bureau.Name, $"/dlanswer?id={responseId}");
                        }
                        else
                        {
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorCode", error.Код ?? "-");
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ErrorMessage", error.Value ?? "-");
                            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
                            return СведенияОПлатежах.CreateError(ticket?.ОГРН ?? bureau.ogrn!, error);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Http exception");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запроса в бюро {bureauName} по адресу {baseAddress}. Переотправка через 1 секунду.", bureau.Name, $"/dlanswer?id={responseId}");
            }
        }
    }

    /// <summary>
    /// Валдиация ответа + Запись подписанных и неподписанных байтов в RedisMessageDTO
    /// </summary>
    /// <param name="body"></param>
    /// <param name="bureau"></param>
    /// <param name="schemaName"></param>
    /// <param name="guid"></param>
    /// <param name="result"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private bool ValidateAnswer(byte[] body, QBCHRequisite bureau, string[] schemaName, string guid, [NotNullWhen(false)] out QBCHResult result, CancellationToken? ct = null)
    {
        result = new();
        _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "SignedQBCHResponse", body).ConfigureAwait(true);

        if (!_cryptoService.ValidateMsg(body, out var cryptoResult, ct: ct))
        {
            switch (cryptoResult.ErrorCode)
            {
                case 4:
                    result.Error = "УЭП КБКИ некорректна";
                    result.ErrorCode = 4;
                    _logger.LogError("УЭП КБКИ некорректна {bureauName}.", bureau.Name);
                    return false;
                case 7:
                    result.Error = "Некорректный формат ответа КБКИ";
                    result.ErrorCode = 7;
                    _logger.LogError("Некорректный формат ответа КБКИ {name}.", bureau.Name);
                    return false;
                case 24:
                    result.Error = "Ошибка при проверке УЭП";
                    result.ErrorCode = 24;
                    _logger.LogError("Некорректный формат ответа КБКИ {name}.", bureau.Name);
                    return false;
                default:
                    throw new Exception($"Неопознанная ошибка криптографии {cryptoResult.ErrorCode}");
            }
        }

        if (cryptoResult.Body is null)
        {
            result.Error = "Ответ не соответствует схеме {bureauName}.";
            result.ErrorCode = 19;
            _logger.LogError("Ответ не соответствует схеме {bureauName}.", bureau.Name);
            return false;
        }
        else
        {
            result.Body = cryptoResult.Body;
        }

        var xsdValidation = _xmlService.ValidateXml(new MemoryStream(cryptoResult.Body), schemaName);

        if (xsdValidation != null && !string.IsNullOrWhiteSpace(xsdValidation.Error))
        {
            result.Error = $"Ответ не соответствует схеме: {xsdValidation.Error}.";
            result.ErrorCode = 19;
            _logger.LogError("Ответ не соответствует схеме в бюро {bureauName}. XSD_Error:{xsd}", bureau.Name, xsdValidation?.Error);
            return false;
        }

        return true;
    }
}
