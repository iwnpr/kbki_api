using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.DlRequestValidationMediatr.ValidationSteps;

/// <summary>
/// 
/// </summary>
public static class DlrequestDateValidaton
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
            transaction.RiseCriticalError(Error.Code23_InvalidRequestDate());
        }
        return transaction;
    }
}
