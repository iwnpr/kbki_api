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
using System.Xml.Linq;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCHService_lib.Services.Implementations.V3;

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
    private readonly int _qbchResponseTimeoutMs = config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs", 10000);

    public async Task<QBCHTaskResult> AmpFromDBv3(QBCHProcessingTransaction processing)
    {
        await _redisCache.AddHash("dlrequest", $"{processing.Id}:{_ourBureauPsrn}", "task_start_date_time", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"));

        var package = processing.GetRequest<ЗапросСведений>();
        if (package is null)
        {
            return new QBCHTaskResult(_ourBureauPsrn);
        }

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
            var getCreditHistoryFlagTask = _qbchDb.GetCreditHistoryPresenceFlagV3(subjectKeys, timeLeft);

            var includeAmp = package.КодСведений == СправочникВидыСведений.Item7;
            var includeAntifraud = package.КодСведений is СправочникВидыСведений.Item7 or СправочникВидыСведений.Item8;
            var getAmpTask = includeAmp ? _qbchDb.GetCalculationOfAmpV3(subjectKeys, timeLeft) : null;
            var getAntifraudTask = includeAntifraud ? _qbchDb.GetAntifraudV3(subjectKeys, timeLeft) : null;

            var pendingTasks = new List<Task<XElement?>> { getSelfProhibitionTask, getCreditHistoryFlagTask };
            if (getAmpTask is not null) pendingTasks.Add(getAmpTask);
            if (getAntifraudTask is not null) pendingTasks.Add(getAntifraudTask);
            await Task.WhenAll(pendingTasks);

            FillObligationsSection(kbki, includeAmp, getAmpTask?.Result);
            FillSelfProhibitionSection(kbki, getSelfProhibitionTask.Result, requestItem.Субъект?.ИНН);
            FillAntifraudSection(kbki, includeAntifraud, getAntifraudTask?.Result, requestItem.Субъект?.ИНН);
            FillCreditHistoryPresence(kbki, getCreditHistoryFlagTask.Result);

            response.КБКИ = [kbki];
            responseRows.Add(response);
        }

        answer.Сведения = responseRows.ToArray();
        return new QBCHTaskResult(_ourBureauPsrn, answer3: answer);
    }

    public Task<QBCHTaskResult> AmpRequestv3(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau)
    {
        throw new NotImplementedException();
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

    private void FillAntifraudSection(ОтветНаЗапросСведенийСведенияКБКИ kbki, bool includeAntifraud, XElement? antifraudXml, ТипИННФЛсПризнаком? inn)
    {
        if (!includeAntifraud)
        {
            return;
        }

        if (!IsInnVerified(inn))
        {
            kbki.ДобавитьПризнакНепредоставленияАнтифродСведений();
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

    private static void FillCreditHistoryPresence(ОтветНаЗапросСведенийСведенияКБКИ kbki, XElement? creditHistoryPresenceFlagXml)
    {
        var value = creditHistoryPresenceFlagXml?
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "ПризнакНаличияКИ")
            ?.Value?
            .Trim();

        if (value == "1")
        {
            kbki.ПризнакНаличияКИ = ОтветНаЗапросСведенийСведенияКБКИПризнакНаличияКИ.Item1;
            kbki.ПризнакНаличияКИSpecified = true;
            return;
        }

        if (value == "0")
        {
            kbki.ПризнакНаличияКИ = ОтветНаЗапросСведенийСведенияКБКИПризнакНаличияКИ.Item0;
            kbki.ПризнакНаличияКИSpecified = true;
        }
    }

    private static XDocument? ToDocument(XElement? xml) => xml is null ? null : new XDocument(xml);

    private static bool IsInnVerified(ТипИННФЛсПризнаком? inn) =>
        inn is not null &&
        !string.IsNullOrWhiteSpace(inn.Value) &&
        inn.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1;
}