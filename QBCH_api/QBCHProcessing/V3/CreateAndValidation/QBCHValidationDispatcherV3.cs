using Cache_lib.Interfaces;
using Crypto_lib.Service;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.ValidationStep;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using qbch_lib;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
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
    public static async Task<QBCHProcessingTransactionV3> ValidateV3(
        this QBCHProcessingTransactionV3 transaction,
        IValidationServiceV3 validationService,
        ICryptoService cryptoService,
        IXmlServiceV3 xmlService,
        ILogger logger,
        IRepositoryV3 repository,
        IKeyValueStorageService cacheService,
        CancellationToken cancellationToken)
    {
        // method
        ValidateRequestMethodV3(transaction, logger);

        // body
        ValidateRequestBodyV3(transaction, logger);

        // sign
        ProcessSignV3(transaction, cryptoService, validationService, logger);

        // xsd
        transaction.ValidateXml(validationService, xmlService, logger);

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        // abonent
        await ValidateAbonentV3(transaction, logger);

        // packet
        transaction.ValidateXmlRequestCollectionV3(requestV3, logger);

        // rights
        await ValidateRightsV3(transaction, repository, logger, cancellationToken);

        // one-window
        ValidateOneWindowV3(transaction, logger);

        // antifraud one-window compatibility
        ValidateAntifraudOneWindowCompatibilityV3(transaction, requestV3, logger);

        // unique request id
        await ValidateUniqueRequestIdV3(transaction, cacheService, requestV3, logger);

        // request date
        ValidateRequestDateV3(transaction, validationService, requestV3, logger);

        // additional validation
        AdditionalValidationV3(transaction, requestV3, logger);

        // agreement
        ValidateAgreementV3(transaction, requestV3, logger);

        // inn/self-prohibition
        ValidateInnAndSelfProhibitionV3(transaction, requestV3, logger);

        transaction.ValidationComplete();
        return transaction;
    }

    private static void ValidateRequestMethodV3(QBCHProcessingTransactionV3 transaction, ILogger logger)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !string.Equals(transaction.ClentRequest.RequestMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            var error = AnswerErrorCode.Code1_WrongRequestMethod();

            logger.LogError("Не пройдена проверка метода запроса dlrequest v3: получен {RequestMethod}, ожидался POST. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, transaction.ClentRequest.RequestMethod, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ValidateRequestBodyV3(QBCHProcessingTransactionV3 transaction, ILogger logger)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && (transaction.Attachment.SignedRequestBody is null || transaction.Attachment.SignedRequestBody.Length == 0))
        {
            var error = AnswerErrorCode.Code2_EmptyRequestBody();

            logger.LogError("Не пройдена проверка тела запроса dlrequest v3: тело запроса пустое. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ValidateAntifraudOneWindowCompatibilityV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
            return;

        if (requestV3.КодСведений == СправочникВидыСведений.Item8 && requestV3.ТипЗапроса == СправочникСпособыЗапросаV3.Item2)
        {
            var error = AnswerErrorCode.Code99_OtherError("Комбинация КодСведений=\"8\" и ТипЗапроса=\"2\" недопустима");

            logger.LogError("Не пройдена проверка совместимости антифрода и одного окна dlrequest v3: КодСведений={КодСведений}, ТипЗапроса={ТипЗапроса}. transactionId: {TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestV3.КодСведений, requestV3.ТипЗапроса, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ProcessSignV3(QBCHProcessingTransactionV3 transaction, ICryptoService cryptoService, IValidationServiceV3 validationService, ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return;
        }

        var signValidationResult = cryptoService.ValidateMsg(
            transaction.Attachment.SignedRequestBody!,
            transaction.ClentRequest.Certificate);

        if (!signValidationResult.IsSuccess)
        {
            logger.LogError("Не пройдена проверка УЭП dlrequest v3: сертификат={Thumbprint}. transactionId: {TransactionId}. code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, transaction.ClentRequest.Certificate?.Thumbprint, signValidationResult.Error!.Code, signValidationResult.Error.Message);
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
            logger.LogError("Не пройдена проверка кодировки dlrequest v3. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка");
        }
    }


    private static async Task ValidateAbonentV3(QBCHProcessingTransactionV3 transaction, ILogger logger)
    {
        //Это проверка сравнения полей ИНН и ОГРН из сертификата с ИНН и ОГРН из запроса, а не из базы
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
            return;

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        if (requestV3 is null)
        {
            var noRequestError = AnswerErrorCode.Code99_OtherError("Отсутствуют данные запроса");

            logger.LogError("Не пройдена проверка реквизитов абонента dlrequest v3: отсутствуют данные запроса. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, noRequestError.Code, noRequestError.Message);

            transaction.RiseCriticalError(noRequestError);
        }

        var requestINN = transaction.ClentRequest?.RequestINN;
        var requestOGRN = transaction.ClentRequest?.RequestOGRN;

        var (abonentINN, abonentOGRN) = GetAbonentRequisitesV3(requestV3);

        // ИНН и ОГРН из сертификата сравнивается с ИНН и ОГРН в запросе
        if (requestINN != abonentINN || requestOGRN != abonentOGRN)
        {
            var error = AnswerErrorCode.Code10_RequestAndAbonentDataNotMach(abonentINN, requestINN, abonentOGRN, requestOGRN);

            logger.LogError("Не пройдена проверка реквизитов абонента dlrequest v3: ИНН сертификата={RequestINN}, ИНН запроса={AbonentINN}, ОГРН сертификата={RequestOGRN}, ОГРН запроса={AbonentOGRN}. transactionId: {TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestINN, abonentINN, requestOGRN, abonentOGRN, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static async Task ValidateRightsV3(QBCHProcessingTransactionV3 transaction, IRepositoryV3 repository, ILogger logger, CancellationToken cancellationToken)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !await repository.IsPermissionGrantedV3(transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, cancellationToken))
        {
            var error = AnswerErrorCode.Code22_AccessDenied();

            logger.LogWarning(
                "{TransactionId} Не пройдена проверка прав доступа dlrequest v3: сертификат={Thumbprint}, сервис={QbchService}. code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ValidateOneWindowV3(QBCHProcessingTransactionV3 transaction, ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
            return;

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        if (requestV3?.ТипЗапроса != СправочникСпособыЗапросаV3.Item2)
            return;

        var requestOgrn = GetAbonentRequisitesV3(requestV3).ogrn;
        var hasOneWindowPermission = transaction.Requisites.All(x => x.ogrn != requestOgrn);

        if (!hasOneWindowPermission)
        {
            var error = AnswerErrorCode.Code14_SingleWindowDenied();

            logger.LogError("Не пройдена проверка одного окна dlrequest v3: ОГРН абонента={RequestOGRN}. transationId={TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestOgrn, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static async Task ValidateUniqueRequestIdV3(
        QBCHProcessingTransactionV3 transaction,
        IKeyValueStorageService cacheService,
        ЗапросСведенийV3? requestV3,
        ILogger logger)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return;
        }

        var requestOgrn = GetAbonentRequisitesV3(requestV3).ogrn;
        var isUniqueRequest = await cacheService.IsUniqueRequestId(requestV3.ИдентификаторЗапроса, requestOgrn ?? string.Empty, RedisConstants.DlRequestV3Scope);

        if (!isUniqueRequest)
        {
            var error = AnswerErrorCode.Code11_RequestIdIsNotUnique();

            logger.LogError("Не пройдена проверка уникальности идентификатора запроса dlrequest v3: ИдентификаторЗапроса={RequestId}, ОГРН абонента={RequestOGRN}.  transationId={TransactionId}  code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestV3.ИдентификаторЗапроса, requestOgrn, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void ValidateRequestDateV3(QBCHProcessingTransactionV3 transaction, IValidationServiceV3 validationService, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && requestV3 is not null &&
            !validationService.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult))
        {
            var error = new AnswerErrorCode(dateValidationResult!.ErrorCode, dateValidationResult.Error ?? "Дата запроса указана некорректно");

            logger.LogError("Не пройдена проверка даты запроса dlrequest v3: ДатаЗапроса={RequestDate}. transationId={TransactionId} code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, requestV3.ДатаЗапроса, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private static void AdditionalValidationV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        transaction.AdditionalValidationV3(requestV3, logger);
    }

    private static void ValidateAgreementV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        transaction.ValidateConsentV3(requestV3, logger);
    }

    private static void ValidateInnAndSelfProhibitionV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3, ILogger logger)
    {
        transaction.ValidateInnAndSelfProhibitionV3(requestV3, logger);
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