using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using СправочникВидыСведенийV3 = QBCH.Lib.qcb_xml.v3_0.СправочникВидыСведений;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using ТипИННФЛсПризнакомПризнакПроверкиV3 = QBCH.Lib.qcb_xml.v3_0.ТипИННФЛсПризнакомПризнакПроверки;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Проверки ИНН/ПризнакПроверки для самозапрета и антифрод сценариев API 3.0.
/// </summary>
public static class SelfLockedUpValidatorV3
{
    public static QBCHProcessingTransaction ValidateInnAndSelfProhibitionV3(this QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
            return transaction;

        var mode = requestV3.РежимЗапроса;
        var requests = requestV3.Запрос ?? [];

        for (var i = 0; i < requests.Length; i++)
        {
            var requestItem = requests[i];
            var orderNumber = ParseOrderNumberOrPosition(requestItem.ПорядковыйНомер, i + 1);

            if (mode == СправочникРежимыЗапросаV3.Item2 && transaction.PackageValidationErrors.Any(x => x.Id == orderNumber))
                continue;

            ValidateInnMatrix(transaction, requestV3.КодСведений, mode, requestItem, orderNumber);

            if (mode == СправочникРежимыЗапросаV3.Item1 &&
                transaction.Status.Equals(QBCHProcessingStatus.Failure))
                return transaction;
            
        }

        return transaction;
    }

    private static void ValidateInnMatrix(QBCHProcessingTransaction transaction, СправочникВидыСведенийV3 infoCode, СправочникРежимыЗапросаV3 mode, ЗапросСведенийЗапросV3 requestItem, int orderNumber)
    {
        // Матрица ИНН/ПризнакПроверки:
        // Код 6: блокируется "запрет".
        // Код 7: блокируются "запрет" и "антифрод".
        // Код 8: блокируются "запрет" и "антифрод без платежной части".
        // Техническое условие для блокировки: ИНН отсутствует или ПризнакПроверки != 1.
        var subjectInn = requestItem.Субъект?.ИНН;
        var isInnBlocked = subjectInn is null ||
                           string.IsNullOrWhiteSpace(subjectInn.Value) ||
                           subjectInn.ПризнакПроверки != ТипИННФЛсПризнакомПризнакПроверкиV3.Item1;

        if (!isInnBlocked)
        {
            return;
        }

        switch (infoCode)
        {
            case СправочникВидыСведенийV3.Item6:
                AddCode25(transaction, mode, orderNumber);
                return;

            case СправочникВидыСведенийV3.Item7:
                AddCode25(transaction, mode, orderNumber);
                return;

            case СправочникВидыСведенийV3.Item8:
                AddCode25(transaction, mode, orderNumber);
                return;
        }
    }

    private static void AddCode25(QBCHProcessingTransaction transaction, СправочникРежимыЗапросаV3 requestMode, int orderNumber)
    {
        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, Error.Code25_SelfLockedUpError());
            return;
        }

        transaction.RiseCriticalError(Error.Code25_SelfLockedUpError());
    }

    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }
}
