using System.Xml.Serialization;
using Domain;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;
using Domain.QBCHModels.qcb_xml.v2_0.qcb_put;
using Domain.QBCHModels.qcb_xml.v2_0.qcb_request;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonXSDValidator
{
    /// <summary>
    /// 9. Запрос не соответствует XSD-схеме запроса. В описании ошибки должна быть включена информация о том, почему запрос не соответствует схеме
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="xmlValidate"></param>
    /// <param name="apiVersion"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction DlRequestValidateXML(this QBCHProcessingTransaction transaction,
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

    /// <summary>
    /// 9. Запрос не соответствует XSD-схеме запроса. В описании ошибки должна быть включена информация о том, почему запрос не соответствует схеме
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="xmlValidate"></param>
    /// <param name="apiVersion"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateXMLDlPut(this QBCHProcessingTransaction transaction,
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
                    var serializer = new XmlSerializer(typeof(ПредставлениеСведенийОПлатежах));

                    try
                    {
                        if (serializer.Deserialize(memoryStream) is not ПредставлениеСведенийОПлатежах deserializeResult)
                        {
                            transaction.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
                        }
                        else
                        {
                            transaction.ClentRequest.SetRequestId(deserializeResult.ИдентификаторЗапроса);
                            transaction.ClentRequest.SetRequestDlPut(deserializeResult);
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
