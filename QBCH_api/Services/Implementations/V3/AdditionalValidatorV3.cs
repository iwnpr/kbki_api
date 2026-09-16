using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using System.Xml.Linq;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using ЗапросСведенийЗапросV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийЗапрос;
using СправочникДУЛV3 = QBCH.Lib.qcb_xml.v3_0.СправочникДУЛ;
using СправочникРежимыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникРежимыЗапроса;
using ТипИностранныйПредпринимательV3 = QBCH.Lib.qcb_xml.v3_0.ТипИностранныйПредприниматель;
using ТипИПV3 = QBCH.Lib.qcb_xml.v3_0.ТипИП;
using ТипЦельКодЦелиV3 = QBCH.Lib.qcb_xml.v3_0.ТипЦельКодЦели;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>
/// Дополнительные проверки API 3.0, не покрываемые XSD.
/// </summary>
public class AdditionalValidatorV3(ILogger<AdditionalValidatorV3> logger) : IAdditionalValidatorV3
{
    private readonly ILogger<AdditionalValidatorV3> _logger = logger;

    private static readonly HashSet<ТипЦельКодЦелиV3> CreditTargets =
    [
        ТипЦельКодЦелиV3.Item1,
        ТипЦельКодЦелиV3.Item11,
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
        ТипЦельКодЦелиV3.Item15
    ];

    public QBCHProcessingTransactionV3 AdditionalValidationV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return transaction;
        }

        ValidatePlaceOfBirthAbsence(transaction);
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return transaction;
        }

        var requests = requestV3.Запрос ?? [];
        for (var i = 0; i < requests.Length; i++)
        {
            var requestItem = requests[i];
            var orderNumber = ValidationHelperV3.ParseOrderNumberOrPosition(requestItem.ПорядковыйНомер, i + 1);

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

    private void ValidateRequest(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        ValidateDul999(transaction, requestMode, requestItem, orderNumber);
        ValidateSubjectBirthDate(transaction, requestMode, requestItem, orderNumber);
        ValidateSubjectDocumentsIssueDate(transaction, requestMode, requestItem, orderNumber);
        ValidateLoanObligations(transaction, requestMode, requestItem, orderNumber);
    }

    private void ValidateDul999(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        if (TryGetSourceDocument(requestItem, out var document) &&
            document?.КодДУЛ == СправочникДУЛV3.Item999 &&
            string.IsNullOrWhiteSpace(document.НаименованиеДУЛ))
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code15_InvalidRequestData("При значении \"КодДУЛ\" = 999 поле \"НаименованиеДУЛ\" обязательно к заполнению"));
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

    private void ValidateSubjectBirthDate(
        QBCHProcessingTransactionV3 transaction,
        СправочникРежимыЗапросаV3 requestMode,
        ЗапросСведенийЗапросV3 requestItem,
        int orderNumber)
    {
        var birthDate = requestItem.Субъект?.ДатаРождения;

        if (birthDate is null)
            return;

        if (birthDate.Value.Date >= DateTime.Today)
        {
            AddError(transaction, requestMode, orderNumber,
                AnswerErrorCode.Code15_InvalidRequestData($"Дата рождения {birthDate:dd.MM.yyyy} больше или равна текущей дате"));
        }
    }

    private void ValidateSubjectDocumentsIssueDate(
        QBCHProcessingTransactionV3 transaction,
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
                    AnswerErrorCode.Code15_InvalidRequestData($"Дата выдачи ДУЛ {document.ДатаВыдачи:dd.MM.yyyy} более ранняя или равна дате рождения {birthDate:dd.MM.yyyy}"));

                return;
            }
        }
    }

    private void ValidateLoanObligations(
        QBCHProcessingTransactionV3 transaction,
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
                AnswerErrorCode.Code15_InvalidRequestData("Для кредитных целей \"СуммаОбязательства\" обязательна к заполнению"));
        }
    }

    private void ValidatePlaceOfBirthAbsence(QBCHProcessingTransactionV3 transaction)
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
            var error = AnswerErrorCode.Code15_InvalidRequestData("Поля и элементы \"МестоРождения\" не допускаются в запросах API 3.0");

            _logger.LogError("Не пройдена проверка отсутствия МестоРождения dlrequest v3: в запросе присутствуют поля или элементы «МестоРождения». TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                           transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void AddError(QBCHProcessingTransactionV3 transaction, СправочникРежимыЗапросаV3 requestMode, int orderNumber, AnswerErrorCode error)
    {
        _logger.LogError("Не пройдена дополнительная проверка dlrequest v3 для запроса №{OrderNumber}, режим={RequestMode}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
            orderNumber, requestMode, transaction.Id, error.Code, error.Message);

        if (requestMode == СправочникРежимыЗапросаV3.Item2)
        {
            transaction.SetPacakgeValidationError(orderNumber, error);
            return;
        }

        transaction.RiseCriticalError(error);
    }
}
