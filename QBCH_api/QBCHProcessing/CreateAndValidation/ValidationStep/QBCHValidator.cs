using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.Enums;
namespace QBCH_api.QBCHProcessing.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class QBCHValidator
{
    /// <summary>
    /// 14. Взаимодействие с абонентом в режиме «одно окно» не предусмотрено договором
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateQBCH(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && transaction.ClentRequest.Request?.ТипЗапроса == СправочникСпособыЗапроса.All)
        {
            var requestOgrn = transaction.ClentRequest.Request?.Абонент?.Requisites?.ogrn;
            bool valiadteQBCH = transaction.Requisites.All(x => x.ogrn != requestOgrn);
            if (!valiadteQBCH)
            {
                transaction.RiseCriticalError(Error.Code14_SingleWindowDenied());
            }
        }
        return transaction;
    }
}
