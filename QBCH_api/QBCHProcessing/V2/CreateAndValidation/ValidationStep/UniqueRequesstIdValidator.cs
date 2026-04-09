using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class UniqueRequesstIdValidator
{
    /// <summary>
    /// 11. Идентификатор запроса не уникален Идентификатор запроса ранее передавался данным абонентом в составе другого запроса такого же типа
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="isUniqueRequestId"></param>
    /// <returns></returns>
    public static async Task<QBCHProcessingTransaction> ValidateUniqueRequestId(this QBCHProcessingTransaction transaction,
        Func<string, string, string, int?, Task<bool>> isUniqueRequestId)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            bool keyExist =
                await isUniqueRequestId(
                                        transaction.ClentRequest?.Request?.ИдентификаторЗапроса!,
                                        transaction.ClentRequest?.Request?.Абонент?.Requisites?.ogrn!,
                                        transaction.ServiceName,
                                        null
                );

            if (!keyExist)
            {
                transaction.RiseCriticalError(Error.Code11_RequestIdIsNotUnique());
            }
        }
        return transaction;
    }
}

