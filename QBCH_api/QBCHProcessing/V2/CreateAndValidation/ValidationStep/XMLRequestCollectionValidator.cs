using qbch_lib.domain.errors;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.Enums;
namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class XMLRequestCollectionValidator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateXMLRequestCollection(this QBCHProcessingTransaction transaction)
    {
        transaction.ValidateXMLRequestCollectionSize();
        transaction.ValidateXMLRequestCollectionIdIsUniqAndStartWichOne();
        return transaction;
    }

    /// <summary>
    /// Валидция дллинны массива блоков
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction ValidateXMLRequestCollectionSize(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var requestCollection = transaction.ClentRequest?.Request?.Запрос;
            switch (transaction.ClentRequest?.Request?.РежимЗапроса)
            {

                case СправочникРежимыЗапроса.Single:
                    if (requestCollection?.Count != 1)
                        transaction.RiseCriticalError(AnswerErrorCode.Code26_WrongBlockCount());
                    break;
                case СправочникРежимыЗапроса.Package:
                    {
                        if (requestCollection?.Count > 10)
                            transaction.RiseCriticalError(AnswerErrorCode.Code26_WrongBlockCount());
                        break;
                    }
            }
        }
        return transaction;
    }

    /// <summary>
    /// Проверка уникальноси порядкового номера запроса и начала последовательности с 1 для пакетного запроса
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction ValidateXMLRequestCollectionIdIsUniqAndStartWichOne(this QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && transaction.ClentRequest.Request?.РежимЗапроса == СправочникРежимыЗапроса.Package)
        {
            var requestIdsCollection =
                transaction.ClentRequest?.Request?.Запрос.Select(i => i.ПорядковыйНомер).ToList();

            if (requestIdsCollection?.First() != 1)
            {
                var errorMessage = "Порядкове номера запросов должны начинаться c \"1\"";
                transaction.RiseCriticalError(AnswerErrorCode.Code99_OtherError(errorMessage));
            }

            if (!requestIdsCollection.Count.Equals(requestIdsCollection.Distinct().Count()))
            {
                var doubleId = transaction.ClentRequest?.Request?.Запрос
                    .GroupBy(x => x.ПорядковыйНомер)
                    .Where(i => i.Count() > 1)
                    .SelectMany(i => i)
                    .ToList();

                var errorMessage = $"Порядковый номер запроса в пакете должен быть уникальным, повторяющиеся значения: {doubleId!.First().ПорядковыйНомер}";
                transaction.RiseCriticalError(AnswerErrorCode.Code99_OtherError(errorMessage));
            }
        }
        return transaction;
    }
}
