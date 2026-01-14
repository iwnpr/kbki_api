using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonUniqueRequesstIdValidator
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


    /// <summary>
    /// 11. Идентификатор запроса не уникален Идентификатор запроса ранее передавался данным абонентом в составе другого запроса такого же типа
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="isUniqueRequestId"></param>
    /// <returns></returns>
    public static async Task<QBCHProcessingTransaction> DlPutValidateUniqueRequesId(this QBCHProcessingTransaction transaction,
        Func<string, string, string, int?, Task<bool>> isUniqueRequestId)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            bool keyExist =
                await isUniqueRequestId(
                                        transaction.ClentRequest?.PutRequest?.ИдентификаторЗапроса!,
                                        transaction.ClentRequest?.PutRequest?.БКИ?.ОГРН,
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

