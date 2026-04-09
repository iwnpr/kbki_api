using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class RequestDateValidator
{
    /// <summary>
    /// 23. Дата запроса указана некорректно В атрибуте «Дата» блока «Запрос» запроса dlrequest указана дата, не являющаяся текущей
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateRequestDate(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && transaction.ClentRequest.Request?.ДатаЗапроса != DateTime.Today)
        {
            transaction.RiseCriticalError(Error.Code23_InvalidRerquestDate());
        }
        return transaction;
    }
}
