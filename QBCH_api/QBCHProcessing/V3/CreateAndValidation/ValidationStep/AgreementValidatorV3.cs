using QBCH.Lib.qcb_xml.v3_0;
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

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Валидация блока "Согласие" для API 3.0.
/// </summary>
public static class ConsentValidatorV3
{
    public static QBCHProcessingTransactionV3 ValidateConsentV3(this QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
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
            var orderNumber = ParseOrderNumberOrPosition(requestItem.ПорядковыйНомер, i + 1);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item2 &&
                transaction.PackageValidationErrors.Any(x => x.Id == orderNumber))
            {
                continue;
            }

            ValidateRequestConsent(transaction, requestV3, requestItem, requiresConsent, orderNumber, logger);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item1 &&
                transaction.Status.Equals(QBCHProcessingStatus.Failure))
            {
                return transaction;
            }
        }

        return transaction;
    }

    private static void ValidateRequestConsent(
        QBCHProcessingTransactionV3 transaction,
        ЗапросСведенийV3 requestV3,
        ЗапросСведенийЗапросV3 requestItem,
        bool requiresAgreement,
        int orderNumber,
        ILogger logger)
    {
        var agreement = requestItem.Согласие;

        if (agreement is null)
        {
            if (requiresAgreement)
            {
                AddError(transaction, requestV3.РежимЗапроса, orderNumber, AnswerErrorCode.Code27_СonsentIsNull(), logger);
            }

            return;
        }

        if (agreement.ДатаВыдачи > DateTime.Today)
        {
            AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied($"Отсутствует действующее согласие Субъекта: Дата выдачи согласия {agreement.ДатаВыдачи:dd.MM.yyyy} больше текущей даты"), logger);

            return;
        }

        ValidateTransferringToAnotherPerson(transaction, requestV3.РежимЗапроса, requestItem, agreement, orderNumber, logger);

        if (HasError(transaction, requestV3.РежимЗапроса, orderNumber))
        {
            return;
        }

        switch (agreement.СрокДействия)
        {
            case СправочникСрокиСогласияV3.Item1:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(6).AddDays(1))
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied("Отсутствует действующее согласие Субъекта: Дата окончания действия согласия (дата выдачи + 6 месяцев) меньше текущей даты"), logger);
                }

                return;

            case СправочникСрокиСогласияV3.Item2:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(12).AddDays(1))
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                       AnswerErrorCode.Code13_СonsentDenied("Отсутствует действующее согласие Субъекта: Дата окончания действия согласия (дата выдачи + 12 месяцев) меньше текущей даты"), logger);
                }

                return;

            case СправочникСрокиСогласияV3.Item3:
                if (agreement.Договор is null)
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied("Отсутствует действующее согласие Субъекта: Элемент \"Договор\" обязателен, когда значение атрибута \"СрокДействия\" равно \"3\""), logger);
                    return;
                }

                // Расширенная семантика кода 3 в 3.0:
                // - допускается договор, заключенный в период действия согласия;
                // - допускается договор, действующий на дату согласия (может быть заключен раньше даты согласия);
                // - допускаются случаи после расторжения при наличии вступившего в силу решения суда.
                // Поэтому проверка "дата договора >= дата согласия" здесь не применяется.
                if (agreement.Договор is not null && agreement.Договор.Дата > DateTime.Today)
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        AnswerErrorCode.Code13_СonsentDenied($"Отсутствует действующее согласие Субъекта: Дата договора {agreement.Договор.Дата:dd.MM.yyyy} больше текущей даты"), logger);
                }

                return;
        }

        // Если у цели 99 нет описания
        if (requestItem.Цель?.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
        {
            AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                AnswerErrorCode.Code15_InvalidRequestData($"Запрос содержит некорректные данные: Код цели запроса со значением \"99\" не содержит описания."), logger);
            return;
        }

        // Если в согласии у цели 99 нет описания
        if (requestItem?.Согласие?.Цель?.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
        {
            AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied($"Отсутствует действующее согласие Субъекта: Запрос содержит некорректные данные: Код цели согласия со значением \"99\" не содержит описания."), logger);
            return;
        }

        //  Проверка кодов цели запроса Одна или несколько целей запроса отсутствует в согласии
        for (int i = 0; i < requestItem?.Цель?.Count(); i++)
        {
            if (!requestItem?.Согласие?.Цель?.Any(x => x.КодЦели == requestItem?.Цель[i].КодЦели) ?? false)
            {
                AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                    AnswerErrorCode.Code13_СonsentDenied($"Отсутствует действующее согласие Субъекта: Одна или несколько целей, указанных в блоке «Запрос» отсутствует."), logger);
            }
        }
    }

    /// <summary>
    /// Проверка соответствия реквизитов источника и лица, которому выдано согласие.
    /// При наличии атрибута "ОснованиеПередачи" реквизиты должны различаться (согласие передано другому лицу),
    /// при его отсутствии — совпадать (согласие выдано самому источнику).
    /// </summary>
    private static void ValidateTransferringToAnotherPerson(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        ТипСогласиеV3 agreement,
        int orderNumber,
        ILogger logger)
    {
        var (innAgreement, ogrnAgreement) = ExtractRequisites(agreement.Выдано?.Item);
        var (innSource, ogrnSource) = ExtractRequisites(requestItem.Источник?.Item);

        if (string.IsNullOrWhiteSpace(innAgreement))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied("Отсутствует действующее согласие Субъекта: В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие."), logger);
            return;
        }

        if (string.IsNullOrWhiteSpace(ogrnAgreement))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code13_СonsentDenied("Отсутствует действующее согласие Субъекта: Отсутствуют реквизиты лица, которому было выдано согласие."), logger);
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
                    $"Отсутствует действующее согласие Субъекта: При наличии в согласии атрибута \"ОснованиеПередачи\" ИНН ({innAgreement}) лица, которому было выдано согласие, не должен совпадать с ИНН ({innSource}) источника."), logger);
                return;
            }

            if (compareOgrn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                   $"Отсутствует действующее согласие Субъекта: При наличии в согласии атрибута \"ОснованиеПередачи\" ОГРН лица ({ogrnAgreement}), которому было выдано согласие, не должен совпадать с ОГРН источника ({ogrnSource})."), logger);
                return;
            }
        }
        else
        {
            // Основания передачи нет — согласие выдано самому источнику, реквизиты должны совпадать.
            if (!compareInn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                    $"Отсутствует действующее согласие Субъекта: ИНН лица ({innAgreement}), которому было выдано согласие, должен совпадать с ИНН источника ({innSource})."), logger);
                return;
            }

            if (!compareOgrn)
            {
                AddError(transaction, requestMode, orderNumber, AnswerErrorCode.Code13_СonsentDenied(
                    $"Отсутствует действующее согласие Субъекта: ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, должен совпадать с ОГРН ({ogrnSource}) источника."), logger);
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

    private static void AddError(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        int orderNumber,
        AnswerErrorCode error,
        ILogger logger)
    {
        logger.LogError("Не пройдена проверка согласия субъекта dlrequest v3 для запроса {OrderNumber}, режим={RequestMode}. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
            transaction.Id, orderNumber, requestMode, error.Code, error.Message);

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
    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }
}
