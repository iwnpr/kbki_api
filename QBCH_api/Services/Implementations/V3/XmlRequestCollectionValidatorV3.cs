using QBCH_api.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using РежимЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>
/// Валидация коллекции блоков "Запрос" для API 3.0.
/// </summary>
public class XmlRequestCollectionValidatorV3(ILogger<XmlRequestCollectionValidatorV3> logger) : IXmlRequestCollectionValidatorV3
{
    private readonly ILogger<XmlRequestCollectionValidatorV3> _logger = logger;

    public QBCHProcessingTransactionV3 ValidateXmlRequestCollectionV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
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

    private void ValidateSingleMode(QBCHProcessingTransactionV3 transaction, int requestCount)
    {
        if (requestCount != 1)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            _logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: одиночный режим, блоков={RequestCount}, ожидался 1. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
               requestCount, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ValidatePackageMode(QBCHProcessingTransactionV3 transaction, List<(string? OrderNumberRaw, int Position)> requests)
    {
        if (requests.Count == 0)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            _logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: пакетный режим без блоков Запрос. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
            return;
        }

        if (requests.Count > 10)
        {
            var error = AnswerErrorCode.Code26_WrongBlockCount();

            _logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3: пакетный режим, блоков={RequestCount}, допустимо не более 10. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
               requests.Count, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
            return;
        }

        var parsedOrders = requests
            .Select(request => new
            {
                request.OrderNumberRaw,
                request.Position,
                OrderNumber = ValidationHelperV3.ParseOrderNumberOrPosition(request.OrderNumberRaw, request.Position)
            })
            .ToList();

        if (parsedOrders[0].OrderNumber != 1)
        {
            AddPackageErrorIfMissing(transaction, parsedOrders[0].OrderNumber, "Порядковые номера запросов должны начинаться с \"1\"");
        }

        var duplicatedOrderNumbers = parsedOrders
            .GroupBy(x => x.OrderNumber)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToList();

        foreach (var duplicatedOrder in duplicatedOrderNumbers)
        {
            AddPackageErrorIfMissing(transaction, duplicatedOrder.OrderNumber, "Порядковый номер запроса в пакете должен быть уникальным");
        }
    }

    private void AddPackageErrorIfMissing(QBCHProcessingTransactionV3 transaction, int orderNumber, string message)
    {
        if (transaction.PackageValidationErrors.Any(x => x.Id == orderNumber && x.error_code == 26))
        {
            return;
        }

        var error = AnswerErrorCode.Code99_OtherError(message);

        _logger.LogError("Не пройдена проверка коллекции блоков Запрос dlrequest v3 для запроса №{OrderNumber}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
            orderNumber, transaction.Id, error.Code, error.Message);

        transaction.SetPacakgeValidationError(orderNumber, error);
    }
}
