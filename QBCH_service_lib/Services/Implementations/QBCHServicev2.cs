using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_answer;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
using QBCH_lib.qcb_xml.v2_0.qcb_result;
using QBCHService_lib.Models;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace QBCHService_lib.Services.Implementations
{
    public partial class QBCHService
    {
        /// <summary>
        /// Сведения о платежах из нашей БД
        /// </summary>
        /// <param name="processing"></param>
        /// <returns></returns>
        public async Task<QBCHTaskResult> AmpFromDBv2(QBCHProcessingTransaction processing)
        {
            await _redisCache.AddHash("dlrequest", $"{processing.Id}:{_OurBureauPSRN}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            var package = processing.ClentRequest.Request;

            // Готовим шаблон ответа
            ОтветНаЗапросСведений answer = new()
            {
                Версия = "2.0",
                ИдентификаторЗапроса = package!.ИдентификаторЗапроса,
                ИдентификаторОтвета = processing.Id.ToString(),
                ОГРН = _OurBureauPSRN,
                ТипОтвета = package.ТипЗапроса,
                РежимЗапроса = package.РежимЗапроса,
                ДатаЗапроса = package.ДатаЗапроса.ToString()
            };

            var timeLeft = _QBCHResponseTimeoutMs * processing.ClentRequest.Request?.Запрос.Count - processing.TimeElapsedForValidation.ElapsedMilliseconds;

            _logger.LogDebug("{guid} {Bureau}: Таймаут для запросов {TimeLeft} ms", processing.Id, _OurBureauPSRN, timeLeft);

            // Сбор данных по всем запросам
            foreach (var запрос in processing.ClentRequest.Request?.Запрос ?? [])
            {
                var error = processing.PackageValidationErrors.FirstOrDefault(x => x.Id == запрос.ПорядковыйНомер);
                Сведения response = new()
                {
                    ПорядковыйНомер = запрос.ПорядковыйНомер,
                    ТитульнаяЧасть = запрос.Субъект
                };

                // Если в блоке была ошибка, не запрашиваем сведения и суем ее сразу в ответ
                if (error is not null)
                {
                    response.КБКИ.Add(new()
                    {
                        ОГРН = _OurBureauPSRN,
                        ПоСостояниюНа = DateTime.Now,
                        Ошибка = new()
                        {
                            Код = error.error_code.ToString(),
                            Value = error.error_message,
                        }
                    });
                    answer.Сведения.Add(response);
                    continue;
                }

                // Готовим шаблон для отправки в функцию поиска субъектов
                ЗапросСведений template = new()
                {
                    Абонент = package.Абонент,
                    ДатаЗапроса = package.ДатаЗапроса,
                    Версия = package.Версия,
                    ИдентификаторЗапроса = package.ИдентификаторЗапроса,
                    КодСведений = package.КодСведений,
                    РежимЗапроса = package.РежимЗапроса,
                    ТипЗапроса = package.ТипЗапроса,
                    Запрос = [запрос]
                };
                Обязательства? ampFromDB = null;
                СубъектНеНайден? noSubject = null;
                ОбязательствНет? noAmp = null;
                УсловияЗапрета? selfProhibition = null;
                СведенийОЗапретеНет? noSelfProhibition = null;
                СведенияОЗапретеНеПредоставляются? selfProhibitionDenied = null;

                // Превращаем в xml
                var xml = _xmlService.SerializeAsString(template);
                var timer = Stopwatch.StartNew();

                // Получаем список SubjectId по титульной части xml
                List<long> subjectKeys = await _qbchDb.GetSearchAllSubjects(xml, timeLeft);
                
                timer.Stop();
                timeLeft -= timer.ElapsedMilliseconds;
                _logger.LogDebug("{guid} {Bureau}: Search all subjects time {elapsed}", processing.Id, _OurBureauPSRN, timer.Elapsed);

                bool subjectFound = subjectKeys.Count != 0;
                bool isITNChecked = запрос.Субъект.ИНН?.ПризнакПроверки is not null && запрос.Субъект.ИНН.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1;
                bool isAPMrequest = package.КодСведений == СправочникВидыСведений.AmpSP3;

                // Заполняем данными по условию
                if (subjectFound)
                {
                    var SPTask = _qbchDb.GetSelfProhibition(subjectKeys, timeLeft);
                    var AMPTask = _qbchDb.GetCalculationOfAmp(subjectKeys, timeLeft);

                    if (isAPMrequest)
                        await Task.WhenAll([SPTask, AMPTask]);
                    else
                        await SPTask;

                    timer.Stop();
                    _logger.LogDebug("{guid} {Bureau}: TASKS:{elapsed}", processing.Id, _OurBureauPSRN, timer.Elapsed);

                    // Если ИНН проверен, добавляем самозапрет
                    if (isITNChecked)
                    {
                        var xSelfProhibition = SPTask.Result;
                        selfProhibition = _xmlService.Deserialize<УсловияЗапрета>(xSelfProhibition);
                        noSelfProhibition = _xmlService.Deserialize<СведенийОЗапретеНет>(xSelfProhibition);
                        selfProhibitionDenied = _xmlService.Deserialize<СведенияОЗапретеНеПредоставляются>(xSelfProhibition);

                        if (selfProhibition is null && noSelfProhibition is null && selfProhibitionDenied is null)
                            noSelfProhibition = new();
                    }
                    else
                    {
                        selfProhibitionDenied = new();
                    }

                    // Если тип запроса ССП + СЗ запрашиваем ССП
                    if (isAPMrequest)
                    {
                        var xAmpDB = AMPTask.Result;
                        ampFromDB = _xmlService.Deserialize<Обязательства>(xAmpDB);
                        noAmp = _xmlService.Deserialize<ОбязательствНет>(xAmpDB);

                        if (ampFromDB is null && noAmp is null)
                            noAmp = new ОбязательствНет();
                    }
                }
                else
                {
                    noSubject = new();
                }

                response.КБКИ.Add(new()
                {
                    ОГРН = _OurBureauPSRN,
                    ПоСостояниюНа = DateTime.Now,
                    СубъектНеНайден = noSubject,
                    ОбязательствНет = noAmp,
                    Обязательства = ampFromDB,
                    СведенийОЗапретеНет = noSelfProhibition,
                    СведенияОЗапретеНеПредоставляются = selfProhibitionDenied,
                    УсловияЗапрета = selfProhibition,
                    ИдентификаторОтвета = processing.Id.ToString()
                });

                answer.Сведения.Add(response);
            }


            //await _redisCache.AddHash("dlrequest", $"{processing.Id}:{_OurBureauPSRN}", "ResponseTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));
            return new(_OurBureauPSRN, answer2: answer);
        }

        /// <summary>
        /// Запрос ССП/Самозапрета
        /// </summary>
        /// <param name="processing"></param>
        /// <param name="client"></param>
        /// <param name="bureau"></param>
        /// <returns></returns>
        public async Task<QBCHTaskResult> AmpRequestv2(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau)
        {
            var guid = processing.Id.ToString();
            var request = processing.ClentRequest.Request;
            var ПорядковыеНомера = request?.Запрос.Select(x => x.ПорядковыйНомер).ToArray() ?? [];
            var startTime = DateTime.Now;
            await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "task_start_date_time", startTime.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

            // Заменяем реквизиты абонента и считываем тип запроса.
            request!.ИдентификаторЗапроса = guid;
            request.Абонент = new()
            {
                ЮридическоеЛицо = new()
                {
                    ИНН = _OurBureauITN,
                    ОГРН = _OurBureauPSRN
                }
            };
            request.ТипЗапроса = СправочникСпособыЗапроса.OurBureau;

            // Добавляем файл с запросом в HttpContent
            var dlrequestBytes = _xmlService.SerializeAsByte(request);
            var signedDlrequestBytes = _cryptoService.SignMsg(dlrequestBytes);
            ByteArrayContent dlrequestContent = new(signedDlrequestBytes);
            HttpResponseMessage? responseMessage = null;

            // Токен ограниченный таймаутом для перезапроса метода dlrequest.
            CancellationTokenSource ticketCts = new();
            // Время ожидания проверки подписи в ответах метода dlrequest.
            CancellationTokenSource ticketCPCts = new();
            ticketCts.CancelAfter(TimeSpan.FromMilliseconds(_QBCHTicketTimeoutMs));
            ticketCPCts.CancelAfter(TimeSpan.FromMilliseconds(_QBCHTicketTimeoutMs - 1000));

            // Таймер отсчета времени оставшегося для перезапросов dlanswer.
            var ticketTime = Stopwatch.StartNew();

            // Ответ на запрос метода dlrequest.
            ОтветНаЗапросСведений? dlrequestResult = null;
            QBCHResult? validationresult = null;
            DlRequestRedisMessage DLRequestRedisMessage = DlRequestRedisMessage.Create(DateTime.Now, signedDlrequestBytes, dlrequestBytes);
            Результат? Результат = null;

            /* dlrequest в КБКИ - запрос тикета или ответа.
             * Перезапрос с таймаутом в 4 секунды.
             * Задача ограничена CancellationToken по таймауту.
             * По окончанию таймаута возвращается ошибка => "Время ожидания ответа истекло"
             */
            try
            {
                dlrequestResult = await Task.Run(async Task<ОтветНаЗапросСведений?>? () =>
                {
                    while (true)
                    {
                        try
                        {
                            DLRequestRedisMessage = DlRequestRedisMessage.Create(DateTime.Now, signedDlrequestBytes, dlrequestBytes);

                            _logger.LogDebug("{guid} {Bureau}: dlrequest send {dt}", guid, bureau.ogrn!, DateTime.Now);
                            responseMessage = await client.PostAsync("dlrequest", dlrequestContent, ticketCts.Token);
                            using MemoryStream ms = new();
                            DLRequestRedisMessage.SetResponseCode(responseMessage.StatusCode);
                            await responseMessage.Content.CopyToAsync(ms);
                            _logger.LogDebug("{guid} {Bureau}: Status {Status}", guid, bureau.ogrn!, (int?)responseMessage.StatusCode);

                            switch (responseMessage.StatusCode)
                            {
                                // Ответ вернулся => завершаем метод, возвращаем овтет, всем спасибо всем пока
                                case HttpStatusCode.OK:
                                    validationresult = ValidateAnswer(ms.ToArray(), bureau, @"xsd\2\qcb_answer.xsd", guid, ticketCPCts.Token, DLRequestRedisMessage);
                                    if (validationresult.IsError)
                                    {
                                        _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, validationresult.Error);
                                        DLRequestRedisMessage.SetError(validationresult.ErrorCode.ToString(), validationresult.Error!).SetResponseTime(DateTime.Now);
                                        await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, validationresult.ErrorCode.ToString(), validationresult.Error!, ПорядковыеНомера);
                                        break;
                                    }
                                    _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);

                                    DLRequestRedisMessage.SetSignedResponse(ms.ToArray()).SetResponseXml(validationresult.Body).SetResponseTime(DateTime.Now);
                                    dlrequestResult = _xmlService.Deserialize<ОтветНаЗапросСведений>(validationresult.Body);
                                    await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                    await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "response_id", dlrequestResult?.ИдентификаторОтвета ?? "-");
                                    break;

                                // Вернулась ошибка => завершаем метод, возвращаем овтет, всем спасибо всем пока
                                case HttpStatusCode.BadRequest:
                                    validationresult = ValidateAnswer(ms.ToArray(), bureau, @"xsd\2\qcb_result.xsd", guid, ticketCPCts.Token, DLRequestRedisMessage);
                                    if (validationresult.IsError)
                                    {
                                        _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, validationresult.Error);
                                        DLRequestRedisMessage.SetError(validationresult.ErrorCode.ToString(), validationresult.Error!).SetResponseTime(DateTime.Now);
                                        await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, validationresult.ErrorCode.ToString(), validationresult.Error!, ПорядковыеНомера);
                                        break;
                                    }
                                    _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);

                                    Результат = _xmlService.Deserialize<Результат>(validationresult.Body);
                                    if (Результат?.РезультатДанные is РезультатОшибка ошибка)
                                    {
                                        DLRequestRedisMessage.SetError(ошибка.Код ?? "-", ошибка.Value ?? "-").SetResponseTime(DateTime.Now);
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(Результат?.ОГРН ?? bureau.ogrn!, ошибка, ПорядковыеНомера);
                                    }
                                    else
                                    {
                                        DLRequestRedisMessage.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(Результат?.ОГРН ?? bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", ПорядковыеНомера);
                                    }

                                    await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                    break;

                                // Вернулся тикет => выходим из цикла, идем стучаться в dlanswer с идентификатором, который вернулся в ответе
                                case HttpStatusCode.Accepted:
                                    validationresult = ValidateAnswer(ms.ToArray(), bureau, @"xsd\2\qcb_result.xsd", guid, ticketCPCts.Token, DLRequestRedisMessage);
                                    if (validationresult.IsError)
                                    {
                                        _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, validationresult.Error);
                                        DLRequestRedisMessage.SetError(validationresult.ErrorCode.ToString(), validationresult.Error!).SetResponseTime(DateTime.Now);
                                        await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, validationresult.ErrorCode.ToString(), validationresult.Error!, ПорядковыеНомера);
                                        break;
                                    }

                                    _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                                    Результат = _xmlService.Deserialize<Результат>(validationresult!.Body)!;

                                    if (Результат.РезультатДанные is РезультатИдентификаторОтвета)
                                    {
                                        _logger.LogDebug("{guid} {Bureau}: Ticket", guid, bureau.ogrn);
                                        DLRequestRedisMessage.SetSignedResponse(ms.ToArray()).SetResponseXml(validationresult.Body).SetResponseTime(DateTime.Now);
                                    }
                                    else
                                    {
                                        _logger.LogDebug("{guid} {Bureau}: Непредвиденные данные в ответе КБКИ", guid, bureau.ogrn);
                                        DLRequestRedisMessage.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                                        dlrequestResult = ОтветНаЗапросСведений.CreateError(Результат?.ОГРН ?? bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", ПорядковыеНомера);
                                    }

                                    await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                    return null;

                                // Вернулся хрен пойми какой код => Пишем данные в БД для информации, перезапрашиваем dlrequest
                                default:
                                    DLRequestRedisMessage.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{await responseMessage.Content.ReadAsStringAsync()}").SetResponseTime(DateTime.Now);
                                    await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                                    break;
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau}  address: {address}", bureau.Name, "/dlrequest");
                            DLRequestRedisMessage.SetError("17", "Не удалось установить соединение.").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                            await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "Ошибка получения ответа от КБКИ: {bureau}  address: {address}", bureau.Name, "/dlrequest");
                            DLRequestRedisMessage.SetError("99", $"Код ответа: {responseMessage?.StatusCode} Message:{(responseMessage is not null ? await responseMessage.Content.ReadAsStringAsync() : string.Empty)}").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                            await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                        }

                        if (dlrequestResult is not null)
                            return dlrequestResult;

                        await Task.Delay(_QBCHTicketDelayMs);
                    }
                }).WaitAsync(ticketCts.Token);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Запрос {guid} в бюро {bureauName} по адресу {baseAddress} был отменен по истечению таймаута {timeout}.", guid, bureau.Name, "/dlrequest", _QBCHTicketTimeoutMs);
                DLRequestRedisMessage.SetError("18", "Время ожидания ответа истекло.").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                await _redisCache.ListSet(key: [DLRequestRedisMessage.Name, guid, bureau.ogrn!, DLRequestRedisMessage.Name], value: JsonSerializer.Serialize(DLRequestRedisMessage));
                dlrequestResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло.", ПорядковыеНомера);
            }

            _logger.LogDebug("{guid} {Bureau}: dlrequest response {dt}", guid, bureau.ogrn!, DateTime.Now);

            /* dlrequestAnswer пустой, только в случае когда код ответа 202 и внутри валидный xml.
             * Если dlrequestAnswer не пустой, значит это либо таймаут, либо ответ, либо ошибка.
             */
            if (dlrequestResult is not null)
                return new(bureau.ogrn!, answer2: dlrequestResult);

            // Токен ограниченный таймаутом для перезапроса ответа по тикету.
            CancellationTokenSource resendCts = new();
            // Токен ограниченный таймаутом для проверки подписи в ответе.
            CancellationTokenSource resendCpCts = new();

            _logger.LogDebug("{guid} {Bureau}: РежимЗапроса {req}", guid, bureau.ogrn!, processing.ClentRequest.Request?.РежимЗапроса);

            /* Таймауты для пакетных и непакетных запросов отличаются.
             * Таймауты для пакета считаются как n запросов * 10 секунд.
             */
            var timeLeftMs = _QBCHResponseTimeoutMs * processing.ClentRequest.Request!.Запрос.Count - ticketTime.ElapsedMilliseconds - processing.TimeElapsedForValidation.ElapsedMilliseconds;
            resendCts.CancelAfter(TimeSpan.FromMilliseconds(timeLeftMs));
            resendCpCts.CancelAfter(TimeSpan.FromMilliseconds(timeLeftMs - 1000));

            _logger.LogDebug("{guid} {Bureau}: Таймаут для запросов {TimeLeft} ms", guid, bureau.ogrn!, timeLeftMs);

             DlAnswerRedisMessage DLAnswerRedisMessage = DlAnswerRedisMessage.Create();

            // Ответ на запрос метода dlanswer
            ОтветНаЗапросСведений? dlanswerResult = null;
            string? responseId = string.Empty;

            /* Отправка запроса в dlanswer.
             */
            try
            {
                dlanswerResult = await Task.Run(async Task<ОтветНаЗапросСведений?>? () =>
                {
                    if (Результат?.РезультатДанные is РезультатИдентификаторОтвета результатИдентификаторОтвета)
                    {
                        responseId = результатИдентификаторОтвета.Value;
                        await _redisCache.AddHash("dlrequest", $"{guid}:{bureau.ogrn}", "response_id", responseId ?? "-");
                        dlanswerResult = await ResendDlanswer(responseId!, client, bureau, request.ИдентификаторЗапроса!, resendCts.Token, ПорядковыеНомера);
                    }
                    else
                    {
                        DLAnswerRedisMessage.SetError("99", "Непредвиденные данные в ответе КБКИ").SetResponseTime(DateTime.Now);
                        dlanswerResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, "99", "Непредвиденные данные в ответе КБКИ", ПорядковыеНомера);
                        await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                    }

                    return dlanswerResult;
                }).WaitAsync(resendCts.Token);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Таймаут запроса в бюро {bureauName} по адресу {baseAddress}.", bureau.Name, $"/dlanswer?id={responseId}");
                DLAnswerRedisMessage = DlAnswerRedisMessage.Create();
                DLAnswerRedisMessage.SetError("18", "Время ожидания ответа истекло.").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                dlanswerResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, "18", "Время ожидания ответа истекло.", ПорядковыеНомера);
                await _redisCache.ListSet(key: [DLAnswerRedisMessage.Name, guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
            }

            _logger.LogDebug("{guid} {Bureau}: dlanswer response {dt}", guid, bureau.ogrn!, DateTime.Now);

            return new(bureau.ogrn!, answer2: dlanswerResult);
        }

        /// <summary>
        /// Перезапрос данных по тикету
        /// </summary>
        /// <param name="responseId"></param>
        /// <param name="client"></param>
        /// <param name="bureau"></param>
        /// <param name="guid"></param>
        /// <param name="ct"></param>
        /// <param name="ПорядковыеНомера"></param>
        /// <returns></returns>
        private async Task<ОтветНаЗапросСведений> ResendDlanswer(string responseId, HttpClient client, QBCHRequisite bureau, string guid, CancellationToken ct, int[] ПорядковыеНомера)
        {
            HttpResponseMessage? responseMessage = null;
            Результат? ticket;
            QBCHResult validationresult;
            ОтветНаЗапросСведений? dlanswerResult = null;
            DlAnswerRedisMessage? DLAnswerRedisMessage = DlAnswerRedisMessage.Create();

            while (true)
            {
                try
                {
                    DLAnswerRedisMessage = DlAnswerRedisMessage.Create();

                    _logger.LogDebug("{guid} {Bureau}: dlanswer send {dt}", guid, bureau.ogrn!, DateTime.Now);
                    responseMessage = await client.GetAsync($"dlanswer?id={responseId}", ct);
                    using var ms = new MemoryStream();
                    await responseMessage.Content.CopyToAsync(ms);
                    DLAnswerRedisMessage.SetResponseCode(responseMessage.StatusCode).SetResponseTime(DateTime.Now);
                    _logger.LogDebug("{guid} {Bureau}: Status {Status}", guid, bureau.ogrn!, (int?)responseMessage.StatusCode);

                    switch (responseMessage.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            validationresult = ValidateAnswer(ms.ToArray(), bureau, @"xsd\2\qcb_answer.xsd", guid, ct, DLAnswerRedisMessage);
                            if (validationresult.IsError)
                            {
                                _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, validationresult.Error);
                                DLAnswerRedisMessage.SetError(validationresult.ErrorCode.ToString(), validationresult.ErrorMessage).SetResponseTime(DateTime.Now);
                                await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                                return ОтветНаЗапросСведений.CreateError(bureau.ogrn!, validationresult.ErrorCode.ToString(), validationresult.Error!, ПорядковыеНомера);
                            }

                            _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                            await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                            return _xmlService.Deserialize<ОтветНаЗапросСведений>(validationresult.Body);
                        // Такого быть не должно - это ошибка!
                        case HttpStatusCode.Accepted:
                        case HttpStatusCode.BadRequest:
                            validationresult = ValidateAnswer(ms.ToArray(), bureau, @"xsd\2\qcb_result.xsd", guid, ct, DLAnswerRedisMessage);
                            if (validationresult.IsError)
                            {
                                _logger.LogDebug("{guid} {Bureau}: InvalidAnswer {err}", guid, bureau.ogrn!, validationresult.Error);
                                DLAnswerRedisMessage.SetError(validationresult.ErrorCode.ToString(), validationresult.ErrorMessage).SetResponseTime(DateTime.Now);
                                await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                                return ОтветНаЗапросСведений.CreateError(bureau.ogrn!, validationresult.ErrorCode.ToString(), validationresult.Error!, ПорядковыеНомера);
                            }

                            _logger.LogDebug("{guid} {Bureau}: Valid xml", guid, bureau.ogrn);
                            ticket = _xmlService.Deserialize<Результат>(validationresult.Body);

                            if (ticket?.РезультатДанные is РезультатОшибка error)
                            {
                                _logger.LogDebug("{guid} {Bureau}: Error code {code} value {value}", guid, bureau.ogrn!, error.Код, error.Value);
                                DLAnswerRedisMessage.SetError(error.Код, error.Value).SetResponseTime(DateTime.Now);

                                // Если код ошибки не 12 то мы получили конкретный error, который нужно транслировать клиенту
                                if (error.Код != "12")
                                {
                                    await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                                    dlanswerResult = ОтветНаЗапросСведений.CreateError(ticket?.ОГРН ?? bureau.ogrn!, error, ПорядковыеНомера);
                                    break;
                                }
                            }
                            else
                            {
                                DLAnswerRedisMessage.SetError("99", "Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа.").SetResponseTime(DateTime.Now);
                                _logger.LogError("Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа. {Bureau}", bureau.Name);
                                await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                                return ОтветНаЗапросСведений.CreateError(bureau.ogrn!, "99", "Данные, полученные от КБКИ, не соответствуют указанному HTTP-коду ответа.", ПорядковыеНомера);
                            }

                            await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                            break;
                        default:
                            DLAnswerRedisMessage.SetError("99", $"Код ответа: {responseMessage.StatusCode} Message:{await responseMessage.Content.ReadAsStringAsync()}").SetResponseTime(DateTime.Now);
                            await _redisCache.ListSet(key: ["dlrequest", guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                            break;
                    }

                    if (dlanswerResult is not null)
                        return dlanswerResult;

                    await Task.Delay(_QBCHResponseDelayMs, ct);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Не удалось установить соединение. КБКИ: {bureau}  address: {address}", bureau.Name, $"/dlanswer?id={responseId}");
                    DLAnswerRedisMessage.SetError("17", "Не удалось установить соединение.").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                    await _redisCache.ListSet(key: [DLAnswerRedisMessage.Name, guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
                    //dlanswerResult = ОтветНаЗапросСведений.CreateError(bureau.ogrn!, "17", "Не удалось установить соединение.", ПорядковыеНомера);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка получения ответа от КБКИ: {bureau}  address: {address}", bureau.Name, $"/dlanswer?id={responseId}");
                    DLAnswerRedisMessage.SetError("99", $"Код ответа: {responseMessage?.StatusCode} Message:{(responseMessage is not null ? await responseMessage.Content.ReadAsStringAsync() : string.Empty)}").SetResponseCode(responseMessage?.StatusCode).SetResponseTime(DateTime.Now);
                    await _redisCache.ListSet(key: [DLAnswerRedisMessage.Name, guid, bureau.ogrn!, DLAnswerRedisMessage.Name], value: JsonSerializer.Serialize(DLAnswerRedisMessage));
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
        /// <param name="ct"></param>
        /// <param name="redisMessage"></param>
        /// <returns></returns>
        private QBCHResult ValidateAnswer(byte[] body, QBCHRequisite bureau, string schemaName, string guid, CancellationToken? ct = null, BaseRedisMessage? redisMessage = null)
        {
            QBCHResult result = new();
            redisMessage?.SetSignedResponse(body);

            if (!_cryptoService.ValidateMsg(body, out var cryptoResult, ct: ct))
            {
                switch (cryptoResult.ErrorCode)
                {
                    case 4:
                        result.Error = "УЭП КБКИ некорректна";
                        result.ErrorCode = 4;
                        result.IsError = true;
                        _logger.LogError("УЭП КБКИ некорректна {bureauName}.", bureau.Name);
                        return result;
                    case 7:
                        result.Error = "Некорректный формат ответа КБКИ";
                        result.ErrorCode = 7;
                        result.IsError = true;
                        _logger.LogError("Некорректный формат ответа КБКИ {name}.", bureau.Name);
                        return result;
                    case 24:
                        result.Error = "Ошибка при проверке УЭП";
                        result.ErrorCode = 24;
                        result.IsError = true;
                        _logger.LogError("Некорректный формат ответа КБКИ {name}.", bureau.Name);
                        return result;
                    default:
                        result.Error = "Ошибка при проверке УЭП";
                        result.ErrorCode = 24;
                        result.IsError = true;
                        _logger.LogError("Неопознанная ошибка криптографии {cryptoResult.ErrorCode}", bureau.Name);
                        return result;
                }
            }

            if (cryptoResult.Body is null)
            {
                result.Error = "Ответ не соответствует схеме {bureauName}.";
                result.ErrorCode = 19;
                result.IsError = true;
                _logger.LogError("Ответ не соответствует схеме {bureauName}.", bureau.Name);
                return result;
            }
            else
            {
                result.Body = cryptoResult.Body;
                redisMessage?.SetResponseXml(cryptoResult.Body);
            }

            var xsdValidation = _xmlService.ValidateXml(new MemoryStream(cryptoResult.Body), [schemaName, @"xsd\2\qcb_common.xsd"]);

            if (xsdValidation != null && !string.IsNullOrWhiteSpace(xsdValidation.Error))
            {
                result.Error = $"Ответ не соответствует схеме: {xsdValidation.Error}.";
                result.ErrorCode = 19;
                result.IsError = true;
                _logger.LogError("Ответ не соответствует схеме в бюро {bureauName}. XSD_Error:{xsd}", bureau.Name, xsdValidation?.Error);
                return result;
            }

            return result;
        }
    }
}