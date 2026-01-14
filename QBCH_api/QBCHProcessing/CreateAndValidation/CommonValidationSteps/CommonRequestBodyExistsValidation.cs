using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonRequestBodyExistsValidation
{
    /// <summary>
    /// Запрос не содержит данных. Проверяется длинна массива контента
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateRequestBodyLength(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            if (transaction.Attachment.SignedRequestBody is null || transaction.Attachment.SignedRequestBody!.Length < 1)
            {
                transaction.RiseCriticalError(Error.Code2_EmptyRequestBody());
                return transaction;
            }
        }
        return transaction;
    }
}
