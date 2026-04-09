using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_request;


namespace QBCH_api.QBCHProcessing.V2.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class SelfLockedUpValidator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction SelfLockedUpValidate(this QBCHProcessingTransaction transaction)
    {
        transaction.SubjectInnIsNotNull(transaction.ClentRequest.Request);
        transaction.SubjectVerificationFlagEqualOne(transaction.ClentRequest.Request);
        return transaction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="запросСведений"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction SubjectInnIsNotNull(this QBCHProcessingTransaction transaction, ЗапросСведений запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            switch (transaction.ClentRequest.Request?.РежимЗапроса)
            {
                case СправочникРежимыЗапроса.Single:
                    var request = запросСведений.Запрос.FirstOrDefault();
                    if (request?.Субъект?.ИНН is null && запросСведений?.КодСведений == СправочникВидыСведений.SP6)
                    {
                        transaction.RiseCriticalError(Error.Code25_SelfLockedUpError());
                    }
                    return transaction;
                case СправочникРежимыЗапроса.Package:
                    var requestCollection = запросСведений.Запрос;
                    requestCollection.ForEach(x =>
                    {
                        if (x.Субъект?.ИНН is null && запросСведений?.КодСведений == СправочникВидыСведений.SP6)
                        {
                            transaction.SetPacakgeValidationError(x.ПорядковыйНомер, Error.Code25_SelfLockedUpError());
                        }
                    });
                    return transaction;
            }
        }
        return transaction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="запросСведений"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction SubjectVerificationFlagEqualOne(this QBCHProcessingTransaction transaction, ЗапросСведений запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            switch (transaction.ClentRequest.Request?.РежимЗапроса)
            {
                case СправочникРежимыЗапроса.Single:
                    var запрос = запросСведений.Запрос.FirstOrDefault();
                    var subjectInn = запрос?.Субъект?.ИНН;
                    if (запросСведений.КодСведений == СправочникВидыСведений.SP6 && subjectInn?.ПризнакПроверки != ТипИННФЛсПризнакомПризнакПроверки.Item1)
                    {
                        transaction.RiseCriticalError(Error.Code25_SelfLockedUpError());
                    }
                    return transaction;
                case СправочникРежимыЗапроса.Package:
                    var subjectInnsCollection = запросСведений.Запрос;
                    subjectInnsCollection.ForEach(i =>
                    {
                        if (запросСведений.КодСведений == СправочникВидыСведений.SP6 && i.Субъект?.ИНН?.ПризнакПроверки != ТипИННФЛсПризнакомПризнакПроверки.Item1)
                        {
                            transaction.SetPacakgeValidationError(i.ПорядковыйНомер, Error.Code25_SelfLockedUpError());
                        }
                    });
                    return transaction;
            }
        }
        return transaction;
    }
}
