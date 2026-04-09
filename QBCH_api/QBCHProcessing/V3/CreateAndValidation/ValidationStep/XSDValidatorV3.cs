using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.domain.aggregate.V3;
using XmlService_lib.Services.Interfaces.V3;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// XSD-валидация и десериализация dlrequest для API 3.0.
/// </summary>
public static class XSDValidatorV3
{
    public static QBCHProcessingTransaction ValidateXmlV3(
        this QBCHProcessingTransaction transaction,
        IValidationServiceV3 validationService,
        IXmlServiceV3 xmlService)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        if (transaction.Attachment.RequestBody is null)
        {
            transaction.RiseCriticalError(Error.Code2_EmptyRequestBody());
            return transaction;
        }

        using var xmlStream = new MemoryStream(transaction.Attachment.RequestBody);
        if (!validationService.ValidateXmlV3(xmlStream, transaction.ServiceName, out var xmlValidationResult))
        {
            transaction.RiseCriticalError(new Error(xmlValidationResult!.ErrorCode, xmlValidationResult.Error ?? "Запрос не соответствует схеме"));
            return transaction;
        }

        var requestV3 = xmlService.DeserializeV3<ЗапросСведенийV3>(transaction.Attachment.RequestBody);
        if (requestV3 is null)
        {
            transaction.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
            return transaction;
        }

        var transactionV3 = QBCHProcessingTransactionV3.From(transaction);
        var clientRequestV3 = transactionV3.GetClientRequestV3();

        clientRequestV3.SetRequestId(requestV3.ИдентификаторЗапроса);
        clientRequestV3.SetRequestV3(requestV3);

        return transaction;
    }
}