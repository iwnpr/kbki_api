using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CommonTypes;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;

/// <summary>
/// 
/// </summary>
public static class CommonRequestMethodValidaton
{
    /// <summary>
    /// Метод передачи запроса  не соответствует требуемому. Проверяется тип метода который пришел в api
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateRequestMethod(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.ClentRequest.RequestMethod!.Equals(HttpMethod.Post.ToString()))
        {
            transaction.RiseCriticalError(Error.Code1_WrongRequestMethod());
            return transaction;
        }
        return transaction;
    }
}
