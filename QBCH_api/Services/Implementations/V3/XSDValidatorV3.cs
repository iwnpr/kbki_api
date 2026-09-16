using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>
/// XSD-валидация и десериализация dlrequest
/// </summary>
public class XSDValidatorV3(IValidationServiceV3 validationService, IXmlServiceV3 xmlService, ILogger<XSDValidatorV3> logger) : IXSDValidatorV3
{
    private readonly IValidationServiceV3 _validationService = validationService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ILogger<XSDValidatorV3> _logger = logger;

    public QBCHProcessingTransactionV3 ValidateXml(QBCHProcessingTransactionV3 transaction)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        if (transaction.Attachment.RequestBody is null)
        {
            var emptyBodyError = AnswerErrorCode.Code2_EmptyRequestBody();

            _logger.LogError("Не пройдена XSD-проверка dlrequest v3: тело запроса после снятия подписи пустое. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, emptyBodyError.Code, emptyBodyError.Message);

            transaction.RiseCriticalError(emptyBodyError);
            return transaction;
        }

        using var xmlStream = new MemoryStream(transaction.Attachment.RequestBody);

        if (!_validationService.ValidateXmlV3(xmlStream, transaction.ServiceName, out var xmlValidationResult))
        {
            _logger.LogError("Не пройдена XSD-проверка dlrequest v3: запрос не соответствует схеме. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, xmlValidationResult!.ErrorCode, xmlValidationResult.Error);

            transaction.RiseCriticalError(new AnswerErrorCode(xmlValidationResult!.ErrorCode, xmlValidationResult.Error));
            return transaction;
        }

        ЗапросСведений? requestV3;

        try
        {
            requestV3 = _xmlService.DeserializeV3<ЗапросСведений>(transaction.Attachment.RequestBody);
        }
        catch (Exception ex)
        {
            var deserializeError = AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message);

            _logger.LogError(ex, "Не пройдена XSD-проверка dlrequest v3: ошибка десериализации запроса. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
               transaction.Id, deserializeError.Code, deserializeError.Message);

            transaction.RiseCriticalError(deserializeError);
            return transaction;
        }

        if (requestV3 is null)
        {
            var emptyRequestError = AnswerErrorCode.Code9_InvalidRequestByScheme("Не был десериализован в ЗапросСведений.");

            _logger.LogError("Не пройдена XSD-проверка dlrequest v3: запрос не был десериализован в ЗапросСведений. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
               transaction.Id, emptyRequestError.Code, emptyRequestError.Message);

            transaction.RiseCriticalError(emptyRequestError);
            return transaction;
        }

        var clientRequest = transaction.ClentRequest;

        clientRequest.SetRequestId(requestV3.ИдентификаторЗапроса);
        clientRequest.SetRequest(requestV3);

        return transaction;
    }
}
