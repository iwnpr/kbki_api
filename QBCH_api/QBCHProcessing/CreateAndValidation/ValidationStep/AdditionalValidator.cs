using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
namespace QBCH_api.QBCHProcessing.ProcessingStep;

public static class AdditionalValidator
{
    /* 15. Запрос содержит ошибочные данные, не
     * выявляющиеся XSD схемой запроса, например,
     * дата выдачи ДУЛ более ранняя, чем дата
     * рождения. Описание ошибки должно включать
     * конкретную причину возникновения ошибки
     */
    public static QBCHProcessingTransaction AdditionalValidation(this QBCHProcessingTransaction transaction) //TODO single|package
    {
        //толь если  КодСведений=""3""
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
            if (transaction.ClentRequest.Request.КодСведений.Equals(СправочникВидыСведений.AmpSP3))
            {
                {
                    switch (transaction.ClentRequest.Request.РежимЗапроса)
                    {
                        case СправочникРежимыЗапроса.Single:
                            var root = transaction.ClentRequest.Request?.Запрос.FirstOrDefault();
                            Dul999Validation(transaction, root);
                            Dul999IpValidation(transaction, root);
                            SubjectBrithDateValidation(transaction, root);
                            SubjectDocumentValidation(transaction, root);
                            LoanObligations(transaction, root);
                            return transaction;
                        case СправочникРежимыЗапроса.Package:
                            var requestCollection = transaction.ClentRequest.Request?.Запрос;

                            requestCollection?.ForEach(r =>
                            {
                                if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                    Dul999IpValidation(transaction, r);
                                if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                    SubjectBrithDateValidation(transaction, r);
                                if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                    SubjectDocumentValidation(transaction, r);
                                if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                    LoanObligations(transaction, r);
                            });
                            return transaction;
                    }
                }
            }
        return transaction;
    }
    // Наименование ДУЛ при коде 999 для ИП
    private static QBCHProcessingTransaction Dul999Validation(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            if (запросСведений.Источник?.ИндивидуальныйПредприниматель?.ДокументЛичности != null &&
            запросСведений.Источник?.ИндивидуальныйПредприниматель?.ДокументЛичности.КодДУЛ == СправочникДУЛ.Item999 &&
            string.IsNullOrWhiteSpace(запросСведений.Источник?.ИндивидуальныйПредприниматель?.ДокументЛичности?.НаименованиеДУЛ ?? string.Empty))
            {

                var er = "При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                switch (transaction.ClentRequest.Request.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        break;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        break;
                }

            }
        }
        return transaction;
    }
    // Наименование ДУЛ при коде 999 для иностранного ИП
    private static QBCHProcessingTransaction Dul999IpValidation(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            if (запросСведений.Источник?.ИностранныйПредприниматель?.ДокументЛичности != null &&
            запросСведений.Источник?.ИностранныйПредприниматель?.ДокументЛичности.КодДУЛ == СправочникДУЛ.Item999 &&
            string.IsNullOrWhiteSpace(запросСведений.Источник?.ИностранныйПредприниматель?.ДокументЛичности?.НаименованиеДУЛ ?? string.Empty))
            {
                var er = "При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                switch (transaction.ClentRequest.Request.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        break;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        break;
                }
            }
        }
        return transaction;
    }
    // Проверка субъекта
    private static QBCHProcessingTransaction SubjectBrithDateValidation(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var subjectBrithDate = запросСведений.Субъект?.ДатаРождения;
            var er = $"Дата рождения {subjectBrithDate:dd.MM.yyyy} больше или равна текущей дате";
            if (subjectBrithDate >= DateTime.Today)
            {
                switch (transaction.ClentRequest.Request.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        break;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        break;
                }
            }
        }
        return transaction;
    }
    // Проверка документов
    private static QBCHProcessingTransaction SubjectDocumentValidation(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var subjectBrithDate = запросСведений.Субъект?.ДатаРождения;
            foreach (var item in запросСведений.Субъект.ДокументЛичности ?? [])
            {
                if (subjectBrithDate >= item.ДатаВыдачи)
                {
                    var er = $"Дата выдачи ДУЛ {item.ДатаВыдачи:dd.MM.yyyy} более ранняя или равна дате рождения {subjectBrithDate:dd.MM.yyyy}";
                    switch (transaction.ClentRequest.Request.РежимЗапроса)
                    {
                        case СправочникРежимыЗапроса.Single:
                            transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                            break;
                        case СправочникРежимыЗапроса.Package:
                            transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                            break;
                    }
                }
            }
        }
        return transaction;
    }
    // Проверка наличия суммы обязательства для целей кедитования
    private static QBCHProcessingTransaction LoanObligations(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            if (запросСведений.Цель != null)
            {
                var creditTargets = new[]
                {
                    ТипЦельКодЦели.Item1,
                    ТипЦельКодЦели.Item2,
                    ТипЦельКодЦели.Item3,
                    ТипЦельКодЦели.Item4,
                    ТипЦельКодЦели.Item5,
                    ТипЦельКодЦели.Item10,
                    ТипЦельКодЦели.Item11,
                    ТипЦельКодЦели.Item12,
                    ТипЦельКодЦели.Item13,
                    ТипЦельКодЦели.Item14,
                    ТипЦельКодЦели.Item15
                };
                bool HasCreditTarget = false;

                foreach (var item in запросСведений.Цель.Select(x => x.КодЦели) ?? [])
                {
                    if (creditTargets.Any(x => x == item))
                    {
                        HasCreditTarget = true;
                        break;
                    }
                }
                if (HasCreditTarget)
                {
                    if (запросСведений.СуммаОбязательства is null)
                    {
                        var er = "Для целей кредитования \"СуммаОбязательства\" обязательна к заполнению";
                        switch (transaction.ClentRequest.Request.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                break;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                break;
                        }
                    }
                }
            }
        }

        return transaction;
    }
}
