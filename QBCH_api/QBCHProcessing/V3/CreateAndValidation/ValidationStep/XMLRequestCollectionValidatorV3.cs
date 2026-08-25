using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using РежимЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Валидация коллекции блоков "Запрос" для API 3.0.
/// </summary>
public static class XMLRequestCollectionValidatorV3
{
    public static QBCHProcessingTransactionV3 ValidateXmlRequestCollectionV3(this QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        var requests = requestV3.Запрос ?? [];

        switch (requestV3.РежимЗапроса)
        {
            case РежимЗапросаV3.Item1:
                ValidateSingleMode(transaction, requests.Length, logger);
                break;
            case РежимЗапросаV3.Item2:
                ValidatePackageMode(transaction, requests.Select((request, index) => (request.ПорядковыйНомер, index + 1)).ToList(), logger);
                break;
        }

        return transaction;
    }

    private static void ValidateSingleMode(QBCHProcessingTransactionV3 transaction, int requestCount, ILogger logger)
    {
        if (requestCount != 1)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: одиночный режим, блоков={RequestCount}, ожидался 1. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestCount, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ValidatePackageMode(QBCHProcessingTransactionV3 transaction, List<(string? OrderNumberRaw, int Position)> requests, ILogger logger)
    {
        if (requests.Count == 0)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: пакетный режим без блоков Запрос. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
            return;
        }

        if (requests.Count > 10)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: пакетный режим, блоков={RequestCount}, допустимо не более 10. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requests.Count, error.Code, error.Message);

            transaction.RiseCriticalError(error);
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
            AddPackageErrorIfMissing(transaction, parsedOrders[0].OrderNumber, "Порядковые номера запросов должны начинаться с \"1\"", logger);
        }

        var duplicatedOrderNumbers = parsedOrders
            .GroupBy(x => x.OrderNumber)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();

        foreach (var duplicatedOrder in duplicatedOrderNumbers)
        {
            AddPackageErrorIfMissing(transaction, duplicatedOrder.OrderNumber, "Порядковый номер запроса в пакете должен быть уникальным", logger);
        }
    }

    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }

    private static void AddPackageErrorIfMissing(QBCHProcessingTransactionV3 transaction, int orderNumber, string message, ILogger logger)
    {
        if (transaction.PackageValidationErrors.Any(x => x.Id == orderNumber && x.error_code == 26))
        {
            return;
        }

        var error = AnswerErrorCode.Code99_OtherError(message);

        logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3 для запроса №{OrderNumber}. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
            transaction.Id, orderNumber, error.Code, error.Message);

        transaction.SetPacakgeValidationError(orderNumber, error);
    }
}