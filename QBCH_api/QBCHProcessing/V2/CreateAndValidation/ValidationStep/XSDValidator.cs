using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
using System.Xml.Serialization;
namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class XSDValidator
{
    /// <summary>
    /// 9. Запрос не соответствует XSD-схеме запроса. В описании ошибки должна быть включена информация о том, почему запрос не соответствует схеме
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="xmlValidate"></param>
    /// <param name="apiVersion"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateXML(this QBCHProcessingTransaction transaction,
        Func<MemoryStream, string, string, Result> xmlValidate,
        string apiVersion)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var xmlValidateResult = xmlValidate(new MemoryStream(transaction.Attachment.RequestBody!), transaction.ServiceName, apiVersion);
            if (!xmlValidateResult.IsSuccess)
            {
                var er = xmlValidateResult.Error;
                transaction.RiseCriticalError(new Error(er!.Code, er.Message));
            }
            else
            {
                if (transaction.Attachment.RequestBody is not null)
                {
                    using var memoryStream = new MemoryStream(transaction.Attachment.RequestBody);
                    var serializer = new XmlSerializer(typeof(ЗапросСведений));

                    try
                    {
                        if (serializer.Deserialize(memoryStream) is not ЗапросСведений deserializeResult)
                        {
                            transaction.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
                        }
                        else
                        {
                            transaction.ClentRequest.SetRequestId(deserializeResult.ИдентификаторЗапроса);
                            transaction.ClentRequest.SetRequest(deserializeResult);
                        }

                    }
                    catch
                    {
                        transaction.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
                    }
                }
            }
        }
        return transaction;
    }
}
