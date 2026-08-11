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
    public static QBCHProcessingTransactionV3 ValidateXml(this QBCHProcessingTransactionV3 transaction, IValidationServiceV3 validationService, IXmlServiceV3 xmlService)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        if (transaction.Attachment.RequestBody is null)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code2_EmptyRequestBody());
            return transaction;
        }

        using var xmlStream = new MemoryStream(transaction.Attachment.RequestBody);

        if (!validationService.ValidateXmlV3(xmlStream, transaction.ServiceName, out var xmlValidationResult))
        {
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
            transaction.RiseCriticalError(AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message));
            return transaction;
        }

        if (requestV3 is null)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code9_InvalidRequestByScheme("Не был десериализован в ЗапросСведений."));
            return transaction;
        }

        var clientRequest = transaction.ClentRequest;

        clientRequest.SetRequestId(requestV3.ИдентификаторЗапроса);
        clientRequest.SetRequest(requestV3);

        return transaction;
    }
}