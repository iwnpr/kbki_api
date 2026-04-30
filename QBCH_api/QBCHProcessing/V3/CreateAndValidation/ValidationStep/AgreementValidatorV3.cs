using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using СправочникВидыСведенийV3 = QBCH.Lib.qcb_xml.v3_0.СправочникВидыСведений;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using СправочникСрокиСогласияV3 = QBCH.Lib.qcb_xml.v3_0.СправочникСрокиСогласия;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Валидация блока "Согласие" для API 3.0.
/// </summary>
public static class AgreementValidatorV3
{
    public static QBCHProcessingTransaction ValidateAgreementV3(
        this QBCHProcessingTransaction transaction,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        var requiresAgreement = RequiresAgreement(requestV3.КодСведений);
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

            ValidateRequestAgreement(transaction, requestV3, requestItem, requiresAgreement, orderNumber);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item1 &&
                transaction.Status.Equals(QBCHProcessingStatus.Failure))
            {
                return transaction;
            }
        }

        return transaction;
    }

    private static void ValidateRequestAgreement(
        QBCHProcessingTransaction transaction,
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
                AddError(transaction, requestV3.РежимЗапроса, orderNumber, Error.Code27_СonsentIsNull());
            }

            return;
        }

        if (agreement.ДатаВыдачи > DateTime.Today)
        {
            AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                Error.Code13_СonsentDenied($"Дата выдачи согласия {agreement.ДатаВыдачи:dd.MM.yyyy} больше текущей даты"));
            return;
        }

        switch (agreement.СрокДействия)
        {
            case СправочникСрокиСогласияV3.Item1:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(6).AddDays(1))
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        Error.Code13_СonsentDenied("Дата окончания действия согласия (дата выдачи + 6 месяцев) меньше текущей даты"));
                }

                return;

            case СправочникСрокиСогласияV3.Item2:
                if (DateTime.Today >= agreement.ДатаВыдачи.AddMonths(12).AddDays(1))
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        Error.Code13_СonsentDenied("Дата окончания действия согласия (дата выдачи + 12 месяцев) меньше текущей даты"));
                }

                return;

            case СправочникСрокиСогласияV3.Item3:
                if (requiresAgreement && agreement.Договор is null)
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        Error.Code15_InvalidRequestData("Элемент \"Договор\" обязателен, когда значение атрибута \"СрокДействия\" равно \"3\""));
                    return;
                }

                if (agreement.Договор is not null && agreement.Договор.Дата > DateTime.Today)
                {
                    AddError(transaction, requestV3.РежимЗапроса, orderNumber,
                        Error.Code13_СonsentDenied($"Дата договора {agreement.Договор.Дата:dd.MM.yyyy} больше текущей даты"));
                }

                return;
        }
    }

    private static void AddError(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        int orderNumber,
        Error error)
    {
        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, error);
            return;
        }

        transaction.RiseCriticalError(error);
    }

    private static bool RequiresAgreement(СправочникВидыСведенийV3 infoCode)
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
