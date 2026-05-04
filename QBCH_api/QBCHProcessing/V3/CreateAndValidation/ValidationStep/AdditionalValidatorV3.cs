using System.Text.RegularExpressions;
using System.Xml.Linq;
using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using СправочникДУЛV3 = QBCH.Lib.qcb_xml.v3_0.СправочникДУЛ;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;
using ТипЦельКодЦелиV3 = QBCH.Lib.qcb_xml.v3_0.ТипЦельКодЦели;
using ТипИПV3 = QBCH.Lib.qcb_xml.v3_0.ТипИП;
using ТипИностранныйПредпринимательV3 = QBCH.Lib.qcb_xml.v3_0.ТипИностранныйПредприниматель;
using QBCH.Lib.qcb_xml.v3_0;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;

/// <summary>
/// Дополнительные проверки API 3.0, не покрываемые XSD.
/// </summary>
public static class AdditionalValidatorV3
{
    private static readonly HashSet<ТипЦельКодЦелиV3> CreditTargets =
    [
        ТипЦельКодЦелиV3.Item1,
        ТипЦельКодЦелиV3.Item2,
        ТипЦельКодЦелиV3.Item3,
        ТипЦельКодЦелиV3.Item4,
        ТипЦельКодЦелиV3.Item5,
        ТипЦельКодЦелиV3.Item10,
        ТипЦельКодЦелиV3.Item111,
        ТипЦельКодЦелиV3.Item12,
        ТипЦельКодЦелиV3.Item13,
        ТипЦельКодЦелиV3.Item131,
        ТипЦельКодЦелиV3.Item14,
        ТипЦельКодЦелиV3.Item141,
        ТипЦельКодЦелиV3.Item15,
        ТипЦельКодЦелиV3.Item151
    ];

    public static QBCHProcessingTransaction AdditionalValidationV3(
        this QBCHProcessingTransaction transaction,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        ValidatePlaceOfBirthAbsence(transaction, requestV3.РежимЗапроса);
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        var requests = requestV3.Запрос ?? [];
        for (var i = 0; i < requests.Length; i++)
        {
            var requestItem = requests[i];
            var orderNumber = ParseOrderNumberOrPosition(requestItem.ПорядковыйНомер, i + 1);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item2 &&
                transaction.PackageValidationErrors.Any(x => x.Id == orderNumber))
            {
                continue;
            }

            ValidateRequest(transaction, requestV3.РежимЗапроса, requestItem, orderNumber);

            if (requestV3.РежимЗапроса == СправочникРежимыЗапросаV3.Item1 &&
                transaction.Status.Equals(QBCHProcessingStatus.Failure))
            {
                return transaction;
            }
        }

        return transaction;
    }

    private static void ValidateRequest(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        ValidateDul999(transaction, requestMode, requestItem, orderNumber);
        ValidateSubjectBirthDate(transaction, requestMode, requestItem, orderNumber);
        ValidateSubjectDocumentsIssueDate(transaction, requestMode, requestItem, orderNumber);
        ValidateLoanObligations(transaction, requestMode, requestItem, orderNumber);
        ValidateSnils(transaction, requestMode, requestItem, orderNumber);
    }

    private static void ValidateDul999(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        if (TryGetSourceDocument(requestItem, out var document) &&
            document?.КодДУЛ == СправочникДУЛV3.Item999 &&
            string.IsNullOrWhiteSpace(document.НаименованиеДУЛ))
        {
            AddError(transaction, requestMode, orderNumber,
                Error.Code15_InvalidRequestData("При значении \"КодДУЛ\" = 999 поле \"НаименованиеДУЛ\" обязательно к заполнению"));
        }
    }

    private static bool TryGetSourceDocument(ЗапросСведенийЗапросV3 requestItem, out ТипДУЛПредпринимателя? document)
    {
        document = requestItem.Источник?.Item switch
        {
            ТипИПV3 ip => ip.ДокументЛичности,
            ТипИностранныйПредпринимательV3 foreignIp => foreignIp.ДокументЛичности,
            _ => null
        };

        return document is not null;
    }

    private static void ValidateSubjectBirthDate(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        var birthDate = requestItem.Субъект?.ДатаРождения;
        if (birthDate is null)
        {
            return;
        }

        if (birthDate.Value.Date >= DateTime.Today)
        {
            AddError(transaction, requestMode, orderNumber,
                Error.Code15_InvalidRequestData($"Дата рождения {birthDate:dd.MM.yyyy} больше или равна текущей дате"));
        }
    }

    private static void ValidateSubjectDocumentsIssueDate(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        var birthDate = requestItem.Субъект?.ДатаРождения;
        if (birthDate is null)
        {
            return;
        }

        foreach (var document in requestItem.Субъект?.ДокументЛичности ?? [])
        {
            if (document.ДатаВыдачи.Date <= birthDate.Value.Date)
            {
                AddError(transaction, requestMode, orderNumber,
                    Error.Code15_InvalidRequestData($"Дата выдачи ДУЛ {document.ДатаВыдачи:dd.MM.yyyy} более ранняя или равна дате рождения {birthDate:dd.MM.yyyy}"));
                return;
            }
        }
    }

    private static void ValidateLoanObligations(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        var hasCreditTarget = (requestItem.Цель ?? [])
            .Select(x => x.КодЦели)
            .Any(x => CreditTargets.Contains(x));

        if (hasCreditTarget && requestItem.СуммаОбязательства is null)
        {
            AddError(transaction, requestMode, orderNumber,
                Error.Code15_InvalidRequestData("Для кредитных целей \"СуммаОбязательства\" обязательна к заполнению"));
        }
    }

    private static void ValidateSnils(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        var snils = requestItem.Субъект?.СНИЛС;
        if (string.IsNullOrWhiteSpace(snils))
        {
            return;
        }

        if (!Regex.IsMatch(snils, "^\\d{11}$"))
        {
            AddError(transaction, requestMode, orderNumber,
                Error.Code15_InvalidRequestData("Поле \"СНИЛС\" должно содержать 11 цифр без дефисов и разделителей"));
        }
    }

    private static void ValidatePlaceOfBirthAbsence(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode)
    {
        if (transaction.Attachment.RequestBody is null)
        {
            return;
        }

        using var stream = new MemoryStream(transaction.Attachment.RequestBody);
        var xml = XDocument.Load(stream, LoadOptions.None);

        var hasPlaceOfBirthElements = xml
            .Descendants()
            .Any(x => x.Name.LocalName.Equals("МестоРождения", StringComparison.OrdinalIgnoreCase));

        var hasPlaceOfBirthAttributes = xml
            .Descendants()
            .Attributes()
            .Any(x => x.Name.LocalName.Equals("МестоРождения", StringComparison.OrdinalIgnoreCase));

        if (hasPlaceOfBirthElements || hasPlaceOfBirthAttributes)
        {
            transaction.RiseCriticalError(
                Error.Code15_InvalidRequestData("Поля и элементы \"МестоРождения\" не допускаются в запросах API 3.0"));
        }
    }

    private static void AddError(
        QBCHProcessingTransaction transaction,
        СправочникРежимыЗапросаV3 requestMode,
        int orderNumber,
        Error error)
    {
        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, error);
            return;
        }

        transaction.RiseCriticalError(error);
    }

    private static int ParseOrderNumberOrPosition(string? orderNumberRaw, int position)
    {
        return int.TryParse(orderNumberRaw, out var parsedOrderNumber) && parsedOrderNumber > 0
            ? parsedOrderNumber
            : position;
    }
}
