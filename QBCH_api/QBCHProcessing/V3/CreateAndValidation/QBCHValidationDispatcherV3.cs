using Cache_lib.Interfaces;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation;

/// <summary>
/// Отдельный диспетчер start-to-finish валидации для API 3.0.
/// </summary>
public static class QBCHValidationDispatcherV3
{
    public static async Task<QBCHProcessingTransaction> ValidateV3(
        this QBCHProcessingTransaction transaction,
        IValidationServiceV3 validationService,
        IXmlServiceV3 xmlService,
        IRepositoryV3 repository,
        ICacheService cacheService,
        string apiVersion,
        CancellationToken cancellationToken)
    {
        // 1) method
        ValidateRequestMethodV3(transaction);

        // 2) body
        ValidateRequestBodyV3(transaction);

        // 3) sign
        ValidateSignatureEnvelopeV3(transaction, validationService);

        // 4) xsd
        transaction.ValidateXmlV3(validationService, xmlService);

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        // 5) abonent
        await ValidateAbonentV3(transaction, repository, requestV3);

        // 6) packet
        transaction.ValidateXmlRequestCollectionV3(requestV3);

        // 7) rights
        await ValidateRightsV3(transaction, repository, cancellationToken);

        // 8) one-window
        ValidateOneWindowV3(transaction);

        // 9) unique request id
        await ValidateUniqueRequestIdV3(transaction, cacheService, apiVersion, requestV3);

        // 10) request date
        ValidateRequestDateV3(transaction, validationService, requestV3);

        // 11) additional validation
        AdditionalValidationV3(transaction, requestV3);

        // 12) agreement
        ValidateAgreementV3(transaction, requestV3);

        // 13) inn/self-prohibition
        ValidateInnAndSelfProhibitionV3(transaction, requestV3);

        transaction.ValidationComplete();
        return transaction;
    }

    private static void ValidateRequestMethodV3(QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !string.Equals(transaction.ClentRequest.RequestMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            transaction.RiseCriticalError(Error.Code1_WrongRequestMethod());
        }
    }

    private static void ValidateRequestBodyV3(QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            (transaction.Attachment.RequestBody is null || transaction.Attachment.RequestBody.Length == 0))
        {
            transaction.RiseCriticalError(Error.Code2_EmptyRequestBody());
        }
    }

    private static void ValidateSignatureEnvelopeV3(QBCHProcessingTransaction transaction, IValidationServiceV3 validationService)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !validationService.ValidateCertificateV3(transaction.ClentRequest.Certificate, out var certValidationResult))
        {
            transaction.RiseCriticalError(new Error(certValidationResult!.ErrorCode, certValidationResult.Error ?? "Ошибка проверки сертификата"));
            return;
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            transaction.Attachment.RequestBody is not null &&
            !validationService.ValidateEncodingV3(transaction.Attachment.RequestBody, out var encodingValidationResult))
        {
            transaction.RiseCriticalError(new Error(encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка"));
        }
    }

    private static async Task ValidateAbonentV3(
        QBCHProcessingTransaction transaction,
        IRepositoryV3 repository,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return;
        }

        var (requestInn, requestOgrn) = GetAbonentRequisitesV3(requestV3);
        var dbRequisites = await repository.GetInnOgrnByThumbprintV3(transaction.ClentRequest.Certificate?.Thumbprint);
        var dbInn = dbRequisites?.Element("inn")?.Value;
        var dbOgrn = dbRequisites?.Element("ogrn")?.Value;

        transaction.ClentRequest.SetRequestCertificateData(transaction.ClentRequest.Certificate?.Thumbprint, dbInn, dbOgrn);

        if (!string.Equals(dbInn, requestInn, StringComparison.Ordinal) ||
            !string.Equals(dbOgrn, requestOgrn, StringComparison.Ordinal))
        {
            transaction.RiseCriticalError(Error.Code10_RequestAndAbonentDataNotMach(requestInn, dbInn, requestOgrn, dbOgrn));
        }
    }

    private static async Task ValidateRightsV3(QBCHProcessingTransaction transaction, IRepositoryV3 repository, CancellationToken cancellationToken)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !await repository.IsPermissionGrantedV3(transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, cancellationToken))
        {
            transaction.RiseCriticalError(Error.Code22_AccessDenied());
        }
    }

    private static void ValidateOneWindowV3(QBCHProcessingTransaction transaction)
    {
        _ = transaction;
    }

    private static async Task ValidateUniqueRequestIdV3(
        QBCHProcessingTransaction transaction,
        ICacheService cacheService,
        string apiVersion,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return;
        }

        var (_, requestOgrn) = GetAbonentRequisitesV3(requestV3);
        var uniqueScope = $"{transaction.ServiceName}:v{apiVersion}";
        var isUniqueRequest = await cacheService.IsUniqueRequestId(requestV3.ИдентификаторЗапроса, requestOgrn ?? string.Empty, uniqueScope);

        if (!isUniqueRequest)
        {
            transaction.RiseCriticalError(Error.Code11_RequestIdIsNotUnique());
        }
    }

    private static void ValidateRequestDateV3(QBCHProcessingTransaction transaction, IValidationServiceV3 validationService, ЗапросСведенийV3? requestV3)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && requestV3 is not null &&
            !validationService.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult))
        {
            transaction.RiseCriticalError(new Error(dateValidationResult!.ErrorCode, dateValidationResult.Error ?? "Дата запроса указана некорректно"));
        }
    }

    private static void AdditionalValidationV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        transaction.AdditionalValidationV3(requestV3);
    }

    private static void ValidateAgreementV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        transaction.ValidateAgreementV3(requestV3);
    }

    private static void ValidateInnAndSelfProhibitionV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        transaction.ValidateInnAndSelfProhibitionV3(requestV3);
    }

    private static (string? inn, string? ogrn) GetAbonentRequisitesV3(ЗапросСведенийV3 request)
    {
        return request.Абонент?.Item switch
        {
            АбонентИЮЛV3 юрЛицо => (юрЛицо.ИНН, юрЛицо.ОГРН),
            АбонентИПV3 ип => (ип.ИННИП, ип.ОГРНИП),
            АбонентИноV3 ино => (ино.НомерНП, ино.РегНомер),
            _ => (null, null)
        };
    }
}