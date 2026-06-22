using Cache_lib.Interfaces;
using Crypto_lib.Service;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using qbch_lib;
using qbch_lib.domain.errors;
using QBCH_lib.domain.aggregate;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using СправочникСпособыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникСпособыЗапроса;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation;

/// <summary>
/// Отдельный диспетчер start-to-finish валидации для API 3.0.
/// </summary>
public static class QBCHValidationDispatcherV3
{
    public static async Task<QBCHProcessingTransaction> ValidateV3(
        this QBCHProcessingTransaction transaction,
        IValidationServiceV3 validationService,
        ICryptoService cryptoService,
        IXmlServiceV3 xmlService,
        IRepositoryV3 repository,
        IKeyValueStorageService cacheService,
        CancellationToken cancellationToken)
    {
        // method
        ValidateRequestMethodV3(transaction);

        // body
        ValidateRequestBodyV3(transaction);

        // sign
        ProcessSignV3(transaction, cryptoService, validationService);

        // xsd
        transaction.ValidateXmlV3(validationService, xmlService);

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        // abonent
        await ValidateAbonentV3(transaction);

        // packet
        transaction.ValidateXmlRequestCollectionV3(requestV3);

        // rights
        await ValidateRightsV3(transaction, repository, cancellationToken);

        // one-window
        ValidateOneWindowV3(transaction);

        // antifraud one-window compatibility
        ValidateAntifraudOneWindowCompatibilityV3(transaction, requestV3);

        // unique request id
        await ValidateUniqueRequestIdV3(transaction, cacheService, requestV3);

        // request date
        ValidateRequestDateV3(transaction, validationService, requestV3);

        // additional validation
        AdditionalValidationV3(transaction, requestV3);

        // agreement
        ValidateAgreementV3(transaction, requestV3);

        // inn/self-prohibition
        ValidateInnAndSelfProhibitionV3(transaction, requestV3);

        transaction.ValidationComplete();
        return transaction;
    }

    private static void ValidateRequestMethodV3(QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !string.Equals(transaction.ClentRequest.RequestMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            transaction.RiseCriticalError(AnswerErrorCode.Code1_WrongRequestMethod());
    }

    private static void ValidateRequestBodyV3(QBCHProcessingTransaction transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && (transaction.Attachment.SignedRequestBody is null || transaction.Attachment.SignedRequestBody.Length == 0))
            transaction.RiseCriticalError(AnswerErrorCode.Code2_EmptyRequestBody());
    }

    private static void ValidateAntifraudOneWindowCompatibilityV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
            return;

        if (requestV3.КодСведений == СправочникВидыСведений.Item8 && requestV3.ТипЗапроса == СправочникСпособыЗапросаV3.Item2)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code99_OtherError("Комбинация КодСведений=\"8\" и ТипЗапроса=\"2\" недопустима"));
        }
    }

    private static void ProcessSignV3(
       QBCHProcessingTransaction transaction,
       ICryptoService cryptoService,
       IValidationServiceV3 validationService)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return;
        }

        //NOTE: Как я понял CryptoService ValidateMsg включает в себя все проверки ValidateCertificate
        //if (!validationService.ValidateCertificateV3(transaction.ClentRequest.Certificate, out var certValidationResult))
        //{
        //    transaction.RiseCriticalError(new AnswerErrorCode(certValidationResult!.ErrorCode, certValidationResult.Error ?? "Ошибка проверки сертификата"));
        //    return;
        //}

        var signValidationResult = cryptoService.ValidateMsg(
            transaction.Attachment.SignedRequestBody!,
            transaction.ClentRequest.Certificate);

        if (!signValidationResult.IsSuccess)
        {
            transaction.RiseCriticalError(new AnswerErrorCode(signValidationResult.Error!.Code, signValidationResult.Error.Message));
            return;
        }

        transaction.Attachment.SetRequestBody(signValidationResult.Value.Body);
        transaction.Attachment.SetSignCertificateData(
            signValidationResult.Value.SignThumbprint,
            signValidationResult.Value.SignINN,
            signValidationResult.Value.SignOGRN);
        transaction.ClentRequest.SetRequestCertificateData(
            signValidationResult.Value.RequestThumbprint,
            signValidationResult.Value.RequestINN,
            signValidationResult.Value.RequestOGRN);

        if (!validationService.ValidateEncodingV3(transaction.Attachment.RequestBody!, out var encodingValidationResult))
        {
            transaction.RiseCriticalError(new AnswerErrorCode(encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка"));
        }
    }


    private static async Task ValidateAbonentV3(QBCHProcessingTransaction transaction)
    {
        //Это проверка сравнения полей ИНН и ОГРН из сертификата с ИНН и ОГРН из запроса, а не из базы
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
            return;

        var requestINN = transaction.ClentRequest?.RequestINN;
        var requestOGRN = transaction.ClentRequest?.RequestOGRN;
        var abonentINN = transaction.ClentRequest?.Request?.Абонент?.Requisites?.inn;
        var abonentOGRN = transaction.ClentRequest?.Request?.Абонент?.Requisites?.ogrn;

        // ИНН и ОГРН из сертификата сравнивается с ИНН и ОГРН в запросе
        if (requestINN != abonentINN || requestOGRN != abonentOGRN)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code10_RequestAndAbonentDataNotMach(abonentINN, requestINN, abonentOGRN, requestOGRN));
        }
    }

    private static async Task ValidateRightsV3(QBCHProcessingTransaction transaction, IRepositoryV3 repository, CancellationToken cancellationToken)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !await repository.IsPermissionGrantedV3(transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, cancellationToken))
            transaction.RiseCriticalError(AnswerErrorCode.Code22_AccessDenied());
    }

    private static void ValidateOneWindowV3(QBCHProcessingTransaction transaction)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
            return;

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        if (requestV3?.ТипЗапроса != СправочникСпособыЗапросаV3.Item2)
            return;

        var requestOgrn = GetAbonentRequisitesV3(requestV3).ogrn;
        var hasOneWindowPermission = transaction.Requisites.All(x => x.ogrn != requestOgrn);

        if (!hasOneWindowPermission)
            transaction.RiseCriticalError(AnswerErrorCode.Code14_SingleWindowDenied());
    }

    private static async Task ValidateUniqueRequestIdV3(
        QBCHProcessingTransaction transaction,
        IKeyValueStorageService cacheService,
        ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return;
        }

        var requestOgrn = GetAbonentRequisitesV3(requestV3).ogrn;
        var isUniqueRequest = await cacheService.IsUniqueRequestId(requestV3.ИдентификаторЗапроса, requestOgrn ?? string.Empty, RedisConstants.DlRequestV3Scope);

        if (!isUniqueRequest)
        {
            transaction.RiseCriticalError(AnswerErrorCode.Code11_RequestIdIsNotUnique());
        }
    }

    private static void ValidateRequestDateV3(QBCHProcessingTransaction transaction, IValidationServiceV3 validationService, ЗапросСведенийV3? requestV3)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && requestV3 is not null &&
            !validationService.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult))
        {
            transaction.RiseCriticalError(new AnswerErrorCode(dateValidationResult!.ErrorCode, dateValidationResult.Error ?? "Дата запроса указана некорректно"));
        }
    }

    private static void AdditionalValidationV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        transaction.AdditionalValidationV3(requestV3);
    }

    private static void ValidateAgreementV3(QBCHProcessingTransaction transaction, ЗапросСведенийV3? requestV3)
    {
        transaction.ValidateConsentV3(requestV3);
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