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
        // Код 6: для "запрета" нужны ИНН и ПризнакПроверки=1.
        // Код 7: при отсутствии ИНН или ПризнакПроверки=0 не предоставляются "запрет" и "антифрод".
        // Код 8: те же правила, что и для кода 7.
        var subjectInn = requestItem.Субъект?.ИНН;
        var hasInn = subjectInn is not null && !string.IsNullOrWhiteSpace(subjectInn.Value);

        var isInnBlocked = infoCode switch
        {
            СправочникВидыСведенийV3.Item6 => !hasInn || subjectInn!.ПризнакПроверки != ТипИННФЛсПризнакомПризнакПроверкиV3.Item1,
            СправочникВидыСведенийV3.Item7 or СправочникВидыСведенийV3.Item8
                => !hasInn || subjectInn!.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверкиV3.Item0,
            _ => false
        };

        if (!isInnBlocked)
        {
            return;
        }

        if (ShouldApplyInnMatrix(infoCode))
            AddCode25(transaction, mode, orderNumber);

    }

    private static bool ShouldApplyInnMatrix(СправочникВидыСведенийV3 infoCode)
    {
        // Матрица кодов сведений 3.0:
        // 6 — запрет/снятие запрета
        // 7 — платежи + антифрод + запрет
        // 8 — антифрод + запрет
        return infoCode is СправочникВидыСведенийV3.Item6
            or СправочникВидыСведенийV3.Item7
            or СправочникВидыСведенийV3.Item8;
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
