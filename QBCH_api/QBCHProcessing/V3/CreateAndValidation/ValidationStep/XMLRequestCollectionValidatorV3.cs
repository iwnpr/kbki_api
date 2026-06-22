using qbch_lib.domain.errors;
using QBCH_lib.domain.aggregate;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using РежимЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Валидация коллекции блоков "Запрос" для API 3.0.
/// </summary>
public static class XMLRequestCollectionValidatorV3
{
    public static QBCHProcessingTransaction ValidateXmlRequestCollectionV3(
        this QBCHProcessingTransaction transaction,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        var requests = requestV3.Запрос ?? [];

        switch (requestV3.РежимЗапроса)
        {
            case РежимЗапросаV3.Item1:
                ValidateSingleMode(transaction, requests.Length);
                break;
            case РежимЗапросаV3.Item2:
                ValidatePackageMode(transaction, requests.Select((request, index) => (request.ПорядковыйНомер, index + 1)).ToList());
                break;
        }

        return transaction;
    }

    private static void ValidateSingleMode(QBCHProcessingTransaction transaction, int requestCount)
    {
        if (requestCount != 1)
            transaction.RiseCriticalError(AnswerErrorCode.Code26_WrongBlockCount());
    }

    private static void ValidatePackageMode(QBCHProcessingTransaction transaction, List<(string? OrderNumberRaw, int Position)> requests)
    {
        if (requests.Count == 0)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code26_WrongBlockCount());
            return;
        }

        if (requests.Count > 10)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code26_WrongBlockCount());
            return;
        }

        var parsedOrders = requests
            .Select(request => new
            {
                request.OrderNumberRaw,
                request.Position,
                OrderNumber = ParseOrderNumberOrPosition(request.OrderNumberRaw, request.Position)
            })
            .ToList();

        if (parsedOrders[0].OrderNumber != 1)
        {
            AddPackageErrorIfMissing(transaction, parsedOrders[0].OrderNumber,
                "Порядковые номера запросов должны начинаться с \"1\"");
        }

        var duplicatedOrderNumbers = parsedOrders
            .GroupBy(x => x.OrderNumber)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();

        foreach (var duplicatedOrder in duplicatedOrderNumbers)
        {
            AddPackageErrorIfMissing(transaction, duplicatedOrder.OrderNumber,
                "Порядковый номер запроса в пакете должен быть уникальным");
        }

        for (var i = 1; i < parsedOrders.Count; i++)
        {
            var previousOrder = parsedOrders[i - 1].OrderNumber;
            var currentOrder = parsedOrders[i].OrderNumber;

            if (currentOrder != previousOrder + 1)
            {
                AddPackageErrorIfMissing(transaction, currentOrder,
                    "Порядковые номера запросов в пакете должны идти подряд без пропусков");
            }
        }
    }

    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }

    private static void AddPackageErrorIfMissing(QBCHProcessingTransaction transaction, int orderNumber, string message)
    {
        if (transaction.PackageValidationErrors.Any(x => x.Id == orderNumber && x.error_code == 26))
        {
            return;
        }

        transaction.SetPacakgeValidationError(orderNumber, AnswerErrorCode.Code99_OtherError(message));
    }
}