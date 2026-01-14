using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonAccessRightsValidator
{
    /// <summary>
    /// Проверка прав доступа
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="isPermissionGranted"></param>
    /// <returns></returns>    
    public static async Task<QBCHProcessingTransaction> ValidateAccessRights(this QBCHProcessingTransaction transaction,
       Func<string?, string?, CancellationToken?, Task<bool>> isPermissionGranted)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            try
            {
                if (!await isPermissionGranted(transaction.ClentRequest?.Certificate?.Thumbprint, transaction.ServiceName, null))
                {
                    transaction.RiseCriticalError(Error.Code22_AccessDenied());
                }
            }
            catch
            {
                if (transaction.ClentRequest?.Certificate?.Thumbprint is null)
                {
                    transaction.RiseCriticalError(Error.Code22_AccessDenied());
                }
            }
        }
        return transaction;
    }
}
