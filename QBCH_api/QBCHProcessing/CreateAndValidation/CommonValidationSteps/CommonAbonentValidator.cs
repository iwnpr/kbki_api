using System.Xml.Linq;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonAbonentValidator
{
    /// <summary>
    /// Сравнение реквизитов запроса с данными в сертификате.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="getInnOgrnByThumbprint"></param>
    /// <returns></returns>
    public static async Task<QBCHProcessingTransaction> DlrequestAbonentValidation(this QBCHProcessingTransaction transaction, Func<string, Task<XElement>> getInnOgrnByThumbprint)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var requestINN = transaction.ClentRequest?.RequestINN;
            var requestOGRN = transaction.ClentRequest?.RequestOGRN;
            var abonentINN = transaction.ClentRequest?.Request?.Абонент?.Requisites?.inn;
            var abonentOGRN = transaction.ClentRequest?.Request?.Абонент?.Requisites?.ogrn;

            // ИНН и ОГРН из сертификата сравнивается с ИНН и ОГРН в запросе
            if (requestINN != abonentINN || requestOGRN != abonentOGRN)
            {
                transaction.RiseCriticalError(Error.Code10_RequestAndAbonentDataNotMach(abonentINN, requestINN, abonentOGRN, requestOGRN));
            }
        }
        return transaction;
    }

    /// <summary>
    /// Сравнение реквизитов запроса с данными в сертификате.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="getInnOgrnByThumbprint"></param>
    /// <returns></returns>
    public static async Task<QBCHProcessingTransaction> DlPutAbonentValidation(this QBCHProcessingTransaction transaction, Func<string, Task<XElement>> getInnOgrnByThumbprint)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var requestOGRN = transaction.ClentRequest?.RequestOGRN;
            var abonentOGRN = transaction.ClentRequest?.PutRequest?.БКИ?.ОГРН;

            // ИНН и ОГРН из сертификата сравнивается с ИНН и ОГРН в запросе
            if (requestOGRN != abonentOGRN)
            {
                transaction.RiseCriticalError(Error.Code10_CertificateAndBKIOGRNNotMach(abonentOGRN, requestOGRN));
            }
        }
        return transaction;
    }
}
