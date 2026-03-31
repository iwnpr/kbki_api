using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.Enums;
namespace QBCH_api.QBCHProcessing.CreateAndValidation.ValidationStep;

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
                    {
                        var er = "Количество блоков \"Запрос\" не соответствует режиму запроса";
                        transaction.RiseCriticalError(Error.Code26_WrongBlockCount(er));
                    }
                    break;
                case СправочникРежимыЗапроса.Package:
                    {
                        if (requestCollection?.Count > 10)
                        {
                            var er = "Количество блоков \"Запрос\" не соответствует режиму запроса";
                            transaction.RiseCriticalError(Error.Code26_WrongBlockCount(er));
                        }
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
                var er = "Порядкове номера запросов должны начинаться c \"1\"";
                transaction.RiseCriticalError(Error.Code26_WrongBlockCount(er));
            }

            if (!requestIdsCollection.Count.Equals(requestIdsCollection.Distinct().Count()))
            {
                var doubleId = transaction.ClentRequest?.Request?.Запрос
                    .GroupBy(x => x.ПорядковыйНомер)
                    .Where(i => i.Count() > 1)
                    .SelectMany(i => i)
                    .ToList();

                var er = $"Порядковый номер запроса в пакете должен быть уникальным, повторяющиеся значения: {doubleId!.First().ПорядковыйНомер}";
                transaction.RiseCriticalError(Error.Code26_WrongBlockCount(er));
            }

        }
        return transaction;
    }
}
