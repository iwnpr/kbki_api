using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class AccessRightsValidator
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
