using System.Security.Cryptography.X509Certificates;
using Domain;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;
using Domain.QBCHModels.CryptoModels;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonSignCheck
{
    /// <summary>
    /// Обработка подписи файла
    /// Проверка подиси
    /// Снятие подписи
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cryptoValidate"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction CommonProcessSign(this QBCHProcessingTransaction transaction, Func<byte[], X509Certificate2?, byte[]?, Result<CryptoServiceResult>> cryptoValidate)
    {
        //Проверка УЭП
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var result = cryptoValidate(transaction.Attachment.SignedRequestBody!, transaction.ClentRequest.Certificate, null);
            if (result.IsSuccess)
            {
                transaction
                    .Attachment
                        .SetRequestBody(result.Value.Body);

                transaction
                    .Attachment
                        .SetSignCertificateData(result.Value?.SignThumbprint,
                                                result.Value?.SignINN,
                                                result.Value?.SignOGRN);
                transaction
                    .ClentRequest
                        .SetRequestCertificateData(result.Value?.RequestThumbprint,
                                                   result.Value?.RequestINN,
                                                   result.Value?.RequestOGRN);
            }
            else
            {
                transaction.RiseCriticalError(new Error(result.Error!.Code, result.Error.Message));
            }
        }
        return transaction;
    }
}
