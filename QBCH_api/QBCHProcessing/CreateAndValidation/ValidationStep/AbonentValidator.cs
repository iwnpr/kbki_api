using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using System.Xml.Linq;
namespace QBCH_api.QBCHProcessing.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class AbonentValidator
{
    /// <summary>
    /// Сравнение реквизитов запроса с данными в сертификате.
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="getInnOgrnByThumbprint"></param>
    /// <returns></returns>
    public static async Task<QBCHProcessingTransaction> AbonentValidation(this QBCHProcessingTransaction transaction, Func<string, Task<XElement>> getInnOgrnByThumbprint)
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
}
