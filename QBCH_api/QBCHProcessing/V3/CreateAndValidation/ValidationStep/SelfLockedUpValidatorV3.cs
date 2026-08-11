using СправочникВидыСведенийV3 = QBCH.Lib.qcb_xml.v3_0.СправочникВидыСведений;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using ТипИННФЛсПризнакомПризнакПроверкиV3 = QBCH.Lib.qcb_xml.v3_0.ТипИННФЛсПризнакомПризнакПроверки;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;
using qbch_lib.domain.errors;
using qbch_lib.domain.aggregate.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Проверки ИНН/ПризнакПроверки для самозапрета и антифрод сценариев API 3.0.
/// </summary>
public static class SelfLockedUpValidatorV3
{
    public static QBCHProcessingTransactionV3 ValidateInnAndSelfProhibitionV3(this QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
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

    private static void ValidateInnMatrix(QBCHProcessingTransactionV3 transaction, СправочникВидыСведенийV3 infoCode, СправочникРежимыЗапросаV3 mode, ЗапросСведенийЗапросV3 requestItem, int orderNumber)
    {
        // Матрица ИНН/ПризнакПроверки:
        // Код 6: для "запрета" нужны ИНН и ПризнакПроверки=1.
        // Код 7: при отсутствии ИНН или ПризнакПроверки=0 запрос не блокируется, в ответе не предоставляются "запрет" и "антифрод", но могут быть выданы платежи.
        // Код 8: для "запрета" и "антифрода" нужны ИНН и ПризнакПроверки=1.
        var subjectInn = requestItem.Субъект?.ИНН;
        var hasInn = subjectInn is not null && !string.IsNullOrWhiteSpace(subjectInn.Value);

        var isInnBlocked = infoCode switch
        {
            СправочникВидыСведенийV3.Item6 or СправочникВидыСведенийV3.Item8
                => !hasInn || subjectInn!.ПризнакПроверки != ТипИННФЛсПризнакомПризнакПроверкиV3.Item1,
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
            or СправочникВидыСведенийV3.Item8;
    }

    private static void AddCode25(QBCHProcessingTransactionV3 transaction, СправочникРежимыЗапросаV3 requestMode, int orderNumber)
    {
        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, AnswerErrorCode.Code25_SelfLockedUpError_V3());
            return;
        }

        transaction.RiseCriticalError(AnswerErrorCode.Code25_SelfLockedUpError_V3());
    }

    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }
}
