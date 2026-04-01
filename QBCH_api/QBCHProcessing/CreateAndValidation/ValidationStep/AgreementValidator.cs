using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_request;
namespace QBCH_api.QBCHProcessing.CreateAndValidation.ValidationStep;

/// <summary>
/// 
/// </summary>
public static class AgreementValidator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction ValidateAgreement(this QBCHProcessingTransaction transaction) //TODO single|package
    {
        //толь если  КодСведений=""3""
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            if (transaction.ClentRequest.Request?.КодСведений == СправочникВидыСведений.AmpSP3)
            {
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        var root = transaction.ClentRequest.Request?.Запрос.FirstOrDefault();
                        transaction.СonsentNotNullValidate(root);
                        transaction.СonsentDateNotNullValidate(root);
                        transaction.СonsentDateLongerCurrentOne(root);
                        transaction.MonthsFromRegistration(root);
                        transaction.TransferringToAnotherPerson(root);
                        transaction.RequestTargetValidation(root);

                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        var requestCollection = transaction.ClentRequest.Request?.Запрос;
                        requestCollection?.ForEach(r =>
                        {
                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.СonsentNotNullValidate(r);

                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.СonsentDateNotNullValidate(r);

                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.СonsentDateLongerCurrentOne(r);

                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.MonthsFromRegistration(r);

                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.TransferringToAnotherPerson(r);

                            if (!transaction.PackageValidationErrors.Any(x => x.Id == r.ПорядковыйНомер))
                                transaction.RequestTargetValidation(r);
                        });
                        return transaction;
                }
            }
        }
        return transaction;
    }
    /// <summary>
    /// Cогласие присутствует
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="запросСведений"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction СonsentNotNullValidate(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос? запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var agreement = запросСведений?.Согласие ?? null;
            if (agreement is null)
            {
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code27_СonsentIsNull());
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений!.ПорядковыйНомер, Error.Code27_СonsentIsNull());
                        return transaction;
                }
            }
        }
        return transaction;
    }
    /// <summary>
    /// Дата выдачи согласия не заполнена
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="запросСведений"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction СonsentDateNotNullValidate(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var AgreementDate = запросСведений.Согласие?.ДатаВыдачи;
            if (!AgreementDate.HasValue)
            {
                var er = "Отсутствует действующее согласие Субъекта: Отсутствует дата согласия";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code13_СonsentDenied(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code13_СonsentDenied(er));
                        return transaction;
                }
            }
        }
        return transaction;
    }
    /// <summary>
    /// Дата выдачи согласия больше текущей
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="запросСведений"></param>
    /// <returns></returns>
    private static QBCHProcessingTransaction СonsentDateLongerCurrentOne(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var AgreementDate = запросСведений.Согласие?.ДатаВыдачи;
            if (AgreementDate > DateTime.Today)
            {
                var er = $"Отсутствует действующее согласие Субъекта: Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше текущей.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        return transaction;
                }
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
    private static QBCHProcessingTransaction MonthsFromRegistration(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            var DurationOfAgreement = запросСведений.Согласие.СрокДействия;
            var AgreementDate = запросСведений.Согласие?.ДатаВыдачи;
            var dateOfContract = запросСведений.Согласие?.Договор?.Дата;

            switch (DurationOfAgreement)
            {
                // 6 Месяцев со дня оформления
                case СправочникСрокиСогласия.I1:
                    if (DateTime.Today >= AgreementDate!.Value.AddMonths(6).AddDays(1))
                    {
                        var er = "Отсутствует действующее согласие Субъекта: Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code13_СonsentDenied(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code13_СonsentDenied(er));
                                return transaction;
                        }
                    }
                    break;

                // 12 месяцев со дня офрмления
                case СправочникСрокиСогласия.I2:
                    if (DateTime.Today >= AgreementDate.Value.AddMonths(12).AddDays(1))
                    {
                        var er = "Отсутствует действующее согласие Субъекта: Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code13_СonsentDenied(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code13_СonsentDenied(er));
                                return transaction;
                        }
                    }
                    break;

                /* В течение срока действия согласия с субъектом кредитной истории были
                * заключены договор займа(кредита), договор лизинга, договор залога, договор
                * поручительства, выдана независимая гарантия
                */
                case СправочникСрокиСогласия.I3:
                    // Если код 3 то договор обязателен
                    if (запросСведений.Согласие?.Договор is null)
                    {
                        var er = "Отсутствует действующее согласие Субъекта: Элемент \"Договор\" обязателен т.к. значение атрибута \"СрокДействия\"=\"3\"";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }

                    if (AgreementDate > dateOfContract)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше даты договора {dateOfContract:dd.MM.yyyy}";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }

                    if (dateOfContract > DateTime.Today)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: Дата договора {dateOfContract:dd.MM.yyyy} больше текущей";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }
                    break;
                default:
                    switch (transaction.ClentRequest.Request?.РежимЗапроса)
                    {
                        case СправочникРежимыЗапроса.Single:
                            var er = $"Отсутствует действующее согласие Субъекта: Дата договора {dateOfContract:dd.MM.yyyy} больше текущей";
                            transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                            return transaction;
                        case СправочникРежимыЗапроса.Package:
                            er = $"Отсутствует действующее согласие Субъекта: Дата договора {dateOfContract:dd.MM.yyyy} больше текущей";
                            transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                            return transaction;
                    }
                    break;
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
    private static QBCHProcessingTransaction TransferringToAnotherPerson(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            // Наличие оснвоания для передачи согласия другому лицу
            bool HasBasement = запросСведений.Согласие?.ОснованиеПередачиSpecified ?? false;

            var ОснованиеПередачи = запросСведений.Согласие?.ОснованиеПередачи;

            // ИНН из согласия
            string? innAgreement =
                запросСведений.Согласие?.Выдано?.ЮридическоеЛицо?.ИНН ??
                запросСведений.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ИНН ??
                запросСведений.Согласие?.Выдано?.ИностранноеЮЛ?.ИНН ??
                запросСведений.Согласие?.Выдано?.ИностранныйПредприниматель?.ИНН;

            // ОзапросСведенийогласия
            string? ogrnAgreement =
                запросСведений.Согласие?.Выдано?.ЮридическоеЛицо?.ОГРН ??
                запросСведений.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ОГРН ??
                запросСведений.Согласие?.Выдано?.ИностранноеЮЛ?.ОГРН ??
                запросСведений.Согласие?.Выдано?.ИностранныйПредприниматель?.ОГРН;

            if (string.IsNullOrWhiteSpace(innAgreement))
            {
                var er = "В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        return transaction;
                }
            }
            if (string.IsNullOrWhiteSpace(ogrnAgreement))
            {
                var er = "Отсутствуют реквизиты лица, которому было выдано согласие.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        return transaction;
                }
            }

            // ИНН источника
            string? innSource =
               запросСведений.Источник?.ЮридическоеЛицо?.ИНН ??
               запросСведений.Источник?.ИндивидуальныйПредприниматель?.ИНН ??
               запросСведений.Источник?.ИностранныйПредприниматель?.ИНН ??
               запросСведений.Источник?.ИностранноеЮЛ?.ИНН;

            // ОГРН Источника
            string? ogrnSource =
                запросСведений.Источник?.ЮридическоеЛицо?.ОГРН ??
                запросСведений.Источник?.ИндивидуальныйПредприниматель?.ОГРН ??
                запросСведений.Источник?.ИностранныйПредприниматель?.ОГРН ??
                запросСведений.Источник?.ИностранноеЮЛ?.ОГРН;

            bool ComapreINN = innAgreement == innSource;
            bool CompareOGRN = ogrnAgreement == ogrnSource;

            switch (HasBasement)
            {
                // Есть основание
                case true:
                    // ИНН совпадает - ошибка
                    if (ComapreINN)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: При наличии в согласии атрибута \"ОснованиеПередачи\" ИНН ({innAgreement}) лица, которому было выдано согласие, не должен совпадать с ИНН ({innSource}) источника.";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }
                    // ОГРН совпадает - ошибка
                    if (CompareOGRN)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: При наличии в согласии атрибута \"ОснованиеПередачи\" ОГРН лица ({ogrnAgreement}), которому было выдано согласие, не должен совпадать с ОГРН источника ({ogrnSource}).";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }
                    break;
                case false:
                    // ИНН не совпадает - ошибка
                    if (!ComapreINN)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: ИНН лица ({innAgreement}), которому было выдано согласие, должен совпадать с ИНН источника ({innSource}).";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }
                    // ОГРН не совпадает - ошибка
                    if (!CompareOGRN)
                    {
                        var er = $"Отсутствует действующее согласие Субъекта: ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, должен совпадать с ОГРН ({ogrnSource}) источника.";
                        switch (transaction.ClentRequest.Request?.РежимЗапроса)
                        {
                            case СправочникРежимыЗапроса.Single:
                                transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                                return transaction;
                            case СправочникРежимыЗапроса.Package:
                                transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                                return transaction;
                        }
                    }
                    break;
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
    private static QBCHProcessingTransaction RequestTargetValidation(this QBCHProcessingTransaction transaction, ЗапросСведенийЗапрос запросСведений)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            // Если у цели 99 нет описания
            if (запросСведений.Цель?.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                var er = $"Запрос содержит некорректные данные: Код цели запроса со значением \"99\" не содержит описания.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        return transaction;
                }
            }

            // Если в согласии у цели 99 нет описания
            if (запросСведений?.Согласие?.Цель?.Any(x => x.КодЦели == ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                var er = $"Запрос содержит некорректные данные: Код цели согласия со значением \"99\" не содержит описания.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code15_InvalidRequestData(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code15_InvalidRequestData(er));
                        return transaction;
                }
            }
        }

        //  Проверка кодов цели запроса Одна или несколько целей запроса отсутствует в согласии
        for (int i = 0; i < запросСведений?.Цель?.Count; i++)
        {
            if (!запросСведений.Согласие?.Цель?.Any(x => x.КодЦели == запросСведений.Цель[i].КодЦели) ?? false)
            {
                var er = "Одна или несколько целей, указанных в блоке «Запрос» отсутствует.";
                switch (transaction.ClentRequest.Request?.РежимЗапроса)
                {
                    case СправочникРежимыЗапроса.Single:
                        transaction.RiseCriticalError(Error.Code13_СonsentDenied(er));
                        return transaction;
                    case СправочникРежимыЗапроса.Package:
                        transaction.SetPacakgeValidationError(запросСведений.ПорядковыйНомер, Error.Code13_СonsentDenied(er));
                        return transaction;
                }
            }
        }
        return transaction;
    }
}
