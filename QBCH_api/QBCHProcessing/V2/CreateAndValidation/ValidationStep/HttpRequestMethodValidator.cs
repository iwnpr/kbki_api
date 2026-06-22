using qbch_lib.domain.errors;
using QBCH_lib.domain.aggregate;

namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class HttpRequestMethodValidator
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
            transaction.RiseCriticalError(AnswerErrorCode.Code1_WrongRequestMethod());
            return transaction;
        }
        return transaction;
    }
}
