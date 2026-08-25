using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// XSD-валидация и десериализация dlrequest
/// </summary>
public static class XSDValidator
{
    public static QBCHProcessingTransactionV3 ValidateXml(this QBCHProcessingTransactionV3 transaction, IValidationServiceV3 validationService, IXmlServiceV3 xmlService, ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        if (transaction.Attachment.RequestBody is null)
        {
            var emptyBodyError = AnswerErrorCode.Code2_EmptyRequestBody();

            logger.LogError("Не пройдена XSD-проверка dlrequest v3: тело запроса после снятия подписи пустое. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, emptyBodyError.Code, emptyBodyError.Message);

            transaction.RiseCriticalError(emptyBodyError);
            return transaction;
        }

        using var xmlStream = new MemoryStream(transaction.Attachment.RequestBody);

        if (!validationService.ValidateXmlV3(xmlStream, transaction.ServiceName, out var xmlValidationResult))
        {
            logger.LogError("Не пройдена XSD-проверка dlrequest v3: запрос не соответствует схеме. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, xmlValidationResult!.ErrorCode, xmlValidationResult.Error);

            transaction.RiseCriticalError(new AnswerErrorCode(xmlValidationResult!.ErrorCode, xmlValidationResult.Error));
            return transaction;
        }

        ЗапросСведений? requestV3;

        try
        {
            requestV3 = xmlService.DeserializeV3<ЗапросСведений>(transaction.Attachment.RequestBody);
        }
        catch(Exception ex)
        {
            var deserializeError = AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message);

            logger.LogError(ex, "Не пройдена XSD-проверка dlrequest v3: ошибка десериализации запроса. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, deserializeError.Code, deserializeError.Message);

            transaction.RiseCriticalError(deserializeError);
            return transaction;
        }

        if (requestV3 is null)
        {
            var emptyRequestError = AnswerErrorCode.Code9_InvalidRequestByScheme("Не был десериализован в ЗапросСведений.");

            logger.LogError("Не пройдена XSD-проверка dlrequest v3: запрос не был десериализован в ЗапросСведений. transactionId: {TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
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