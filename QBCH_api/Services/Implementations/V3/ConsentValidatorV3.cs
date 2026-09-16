using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;
using СправочникВидыСведенийV3 = QBCH.Lib.qcb_xml.v3_0.СправочникВидыСведений;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using СправочникСрокиСогласияV3 = QBCH.Lib.qcb_xml.v3_0.СправочникСрокиСогласия;
using ТипИПV3 = QBCH.Lib.qcb_xml.v3_0.ТипИП;
using ТипИПБазовыйV3 = QBCH.Lib.qcb_xml.v3_0.ТипИПБазовый;
using ТипСогласиеV3 = QBCH.Lib.qcb_xml.v3_0.ТипСогласие;
using ТипЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ТипЮЛ;
using ТипЮЛБазовыйV3 = QBCH.Lib.qcb_xml.v3_0.ТипЮЛБазовый;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>
/// Валидация блока "Согласие" для API 3.0.
/// </summary>
public class ConsentValidatorV3(ILogger<ConsentValidatorV3> logger) : IConsentValidatorV3
{
    private readonly ILogger<ConsentValidatorV3> _logger = logger;

    public QBCHProcessingTransactionV3 ValidateConsentV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        var requiresConsent = RequiresConsent(requestV3.КодСведений);
        var requests = requestV3.Запрос ?? [];

        for (var i = 0; i < requests.Length; i++)
        {
            var requestItem = requests[i];
            var orderNumber = ValidationHelperV3.ParseOrderNumberOrPosition(requestItem.ПорядковыйНомер, i + 1);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item2 &&
                transaction.PackageValidationErrors.Any(x => x.Id == orderNumber))
            {
                continue;
            }

            ValidateRequestConsent(transaction, requestV3, requestItem, requiresConsent, orderNumber);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item1 &&
                transaction.Status.Equals(QBCHProcessingStatus.Failure))
            {
                return transaction;
            }
        }

        return transaction;
    }

    private void ValidateRequestConsent(
        QBCHProcessingTransactionV3 transaction,
        ЗапросСведенийV3 requestV3,
        ЗапросСведенийЗапросV3 requestItem,
        bool requiresAgreement,
        int orderNumber)
    {
        var agreement = requestItem.Согласие;

        if (agreement is null)
        {
            if (requiresAgreement)
            {
                AddError(transaction, requestV3.РежимЗапроса, orderNumber, AnswerErrorCode.Code27_СonsentIsNull());
            }

            return;
        }

        if (agreement.ДатаВыдачи > DateTime.Today)
        {
            AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied($"Дата выдачи согласия {agreement.ДатаВыдачи:dd.MM.yyyy} больше текущей даты"));

            return;
        }

        ValidateTransferringToAnotherPerson(transaction, requestV3.РежимЗапроса, requestItem, agreement, orderNumber);

        if (HasError(transaction, requestV3.РежимЗапроса, orderNumber))
        {
            return;
        }

        ValidateConsentPeriod(transaction, requestV3.РежимЗапроса, agreement, orderNumber);

        if (HasError(transaction, requestV3.РежимЗапроса, orderNumber))
        {
            return;
        }

        ValidateConsentTargets(transaction, requestV3.РежимЗапроса, requestItem, agreement, orderNumber);
    }

    /// <summary>
    /// Проверка срока действия согласия.
    /// </summary>
    private void ValidateConsentPeriod(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ТипСогласиеV3 agreement,
        int orderNumber)
    {
        switch (agreement.СрокДействия)
        {
            case СправочникСрокиСогласияV3.Item1:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(6).AddDays(1))
                {
                    AddError(transaction, requestMode, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied("Дата окончания действия согласия (дата выдачи + 6 месяцев) меньше текущей даты"));
                }

                return;

            case СправочникСрокиСогласияV3.Item2:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(12).AddDays(1))
                {
                    AddError(transaction, requestMode, orderNumber,
                       AnswerErrorCode.Code13_СonsentDenied("Дата окончания действия согласия (дата выдачи + 12 месяцев) меньше текущей даты"));
                }

                return;

            case СправочникСрокиСогласияV3.Item3:
                if (agreement.Договор is null)
                {
                    AddError(transaction, requestMode, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied("Элемент \"Договор\" обязателен, когда значение атрибута \"СрокДействия\" равно \"3\""));
                    return;
                }

                // Расширенная семантика кода 3 в 3.0:
                // - допускается договор, заключенный в период действия согласия;
                // - допускается договор, действующий на дату согласия (может быть заключен раньше даты согласия);
                // - допускаются случаи после расторжения при наличии вступившего в силу решения суда.
                // Поэтому проверка "дата договора >= дата согласия" здесь не применяется.
                if (agreement.Договор.Дата > DateTime.Today)
                {
                    AddError(transaction, requestMode, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied($"Дата договора {agreement.Договор.Дата:dd.MM.yyyy} больше текущей даты"));
                }

                return;
        }
    }

    /// <summary>
    /// Проверка целей запроса на соответствие целям, на которые субъект выдал согласие.
    /// Цель, указанная в блоке "Запрос" и отсутствующая в блоке "Согласие" (в том числе когда
    /// в согласии не указано ни одной цели), означает, что субъект не давал согласия на эту цель, — код ошибки 13.
    /// </summary>
    private void ValidateConsentTargets(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        ТипСогласиеV3 agreement,
        int orderNumber)
    {
        var requestTargets = requestItem.Цель ?? [];
        var consentTargets = agreement.Цель ?? [];

        // Если у цели 99 нет описания
        if (requestTargets.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code15_InvalidRequestData("Код цели запроса со значением \"99\" не содержит описания."));
            return;
        }

        // Если в согласии у цели 99 нет описания
        if (consentTargets.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code15_InvalidRequestData("Код цели согласия со значением \"99\" не содержит описания."));
            return;
        }

        // В блоке "Согласие" не указано ни одной цели: согласие не покрывает ни одну цель запроса
        if (requestTargets.Length > 0 && consentTargets.Length == 0)
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied("В блоке «Согласие» не указано ни одной цели"));
            return;
        }

        // Проверка кодов цели запроса: одна или несколько целей запроса отсутствуют в согласии
        var missingTargets = requestTargets
            .Where(target => consentTargets.All(consentTarget => consentTarget.КодЦели != target.КодЦели))
            .Select(target => target.GetTargetCode())
            .Distinct()
            .ToArray();

        if (missingTargets.Length > 0)
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied(
                    $"Cогласие не включает в себя всех целей запроса: одна или несколько целей, указанных в блоке «Запрос», отсутствует в блоке «Согласие» (КодЦели: {string.Join(", ", missingTargets)})"));
        }
    }

    /// <summary>
    /// Проверка соответствия реквизитов источника и лица, которому выдано согласие.
    /// При наличии атрибута "ОснованиеПередачи" реквизиты должны различаться (согласие передано другому лицу),
    /// при его отсутствии — совпадать (согласие выдано самому источнику).
    /// </summary>
    private void ValidateTransferringToAnotherPerson(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        ТипСогласиеV3 agreement,
        int orderNumber)
    {
        var (innAgreement, ogrnAgreement) = ExtractRequisites(agreement.Выдано?.Item);
        var (innSource, ogrnSource) = ExtractRequisites(requestItem.Источник?.Item);

        if (string.IsNullOrWhiteSpace(innAgreement))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied("В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие."));
            return;
        }

        if (string.IsNullOrWhiteSpace(ogrnAgreement))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied("Отсутствуют реквизиты лица, которому было выдано согласие."));
            return;
        }

        var compareInn = innAgreement == innSource;
        var compareOgrn = ogrnAgreement == ogrnSource;

        if (agreement.ОснованиеПередачиSpecified)
        {
            // Есть основание передачи — реквизиты источника и получателя согласия не должны совпадать.
            if (compareInn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                    $"При наличии в согласии атрибута \"ОснованиеПередачи\" ИНН ({innAgreement}) лица, которому было выдано согласие, не должен совпадать с ИНН ({innSource}) источника."));
                return;
            }

            if (compareOgrn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                   $"При наличии в согласии атрибута \"ОснованиеПередачи\" ОГРН лица ({ogrnAgreement}), которому было выдано согласие, не должен совпадать с ОГРН источника ({ogrnSource})."));
                return;
            }
        }
        else
        {
            // Основания передачи нет — согласие выдано самому источнику, реквизиты должны совпадать.
            if (!compareInn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                    $"ИНН лица ({innAgreement}), которому было выдано согласие, должен совпадать с ИНН источника ({innSource})."));
                return;
            }

            if (!compareOgrn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                    $"ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, должен совпадать с ОГРН ({ogrnSource}) источника."));
                return;
            }
        }
    }

    /// <summary>
    /// Извлекает ИНН и ОГРН из элемента источника/блока "Выдано" (российские ЮЛ и ИП).
    /// Для иностранных лиц реквизиты ИНН/ОГРН отсутствуют.
    /// </summary>
    private static (string? inn, string? ogrn) ExtractRequisites(object? item) => item switch
    {
        ТипЮЛV3 ul => (ul.ИНН, ul.ОГРН),
        ТипЮЛБазовыйV3 ul => (ul.ИНН, ul.ОГРН),
        ТипИПV3 ip => (ip.ИННИП, ip.ОГРНИП),
        ТипИПБазовыйV3 ip => (ip.ИННИП, ip.ОГРНИП),
        _ => (null, null),
    };

    private static bool HasError(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        int orderNumber)
    {
        return requestMode == СправочникРежимыЗапросаV3.Item2
            ? transaction.PackageValidationErrors.Any(x => x.Id == orderNumber)
            : transaction.Status.Equals(QBCHProcessingStatus.Failure);
    }

    private void AddError(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        int orderNumber,
        AnswerErrorCode error)
    {
        _logger.LogError("Не пройдена проверка согласия субъекта dlrequest. режим={RequestMode}. transactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                    requestMode, transaction.Id, error.Code, error.Message);

        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, error);
            return;
        }

        transaction.RiseCriticalError(error);
    }

    private static bool RequiresConsent(СправочникВидыСведенийV3 infoCode)
    {
        // Матрица кодов сведений 3.0:
        // 6 — запрет/снятие запрета (согласие не требуется)
        // 7 — платежи + антифрод + запрет (согласие требуется)
        // 8 — антифрод + запрет (согласие требуется)
        return infoCode is СправочникВидыСведенийV3.Item7 or СправочникВидыСведенийV3.Item8;
    }
}
