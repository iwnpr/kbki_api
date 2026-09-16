using Cache_lib.Interfaces;
using Crypto_lib.Service;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using qbch_lib;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.errors;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
using СправочникСпособыЗапросаV3 = QBCH.Lib.qcb_xml.v3_0.СправочникСпособыЗапроса;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>
/// Отдельный диспетчер start-to-finish валидации для API 3.0.
/// </summary>
public class QBCHValidationDispatcherV3(
    IValidationServiceV3 validationService,
    ICryptoService cryptoService,
    IRepositoryV3 repository,
    IKeyValueStorageService cacheService,
    IXSDValidatorV3 xsdValidator,
    IAdditionalValidatorV3 additionalValidator,
    IConsentValidatorV3 consentValidator,
    ISelfLockedUpValidatorV3 selfLockedUpValidator,
    IXmlRequestCollectionValidatorV3 xmlRequestCollectionValidator,
    ILogger<QBCHValidationDispatcherV3> logger) : IQBCHValidationDispatcherV3
{
    private readonly IValidationServiceV3 _validationService = validationService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IRepositoryV3 _repository = repository;
    private readonly IKeyValueStorageService _cacheService = cacheService;
    private readonly IXSDValidatorV3 _xsdValidator = xsdValidator;
    private readonly IAdditionalValidatorV3 _additionalValidator = additionalValidator;
    private readonly IConsentValidatorV3 _consentValidator = consentValidator;
    private readonly ISelfLockedUpValidatorV3 _selfLockedUpValidator = selfLockedUpValidator;
    private readonly IXmlRequestCollectionValidatorV3 _xmlRequestCollectionValidator = xmlRequestCollectionValidator;
    private readonly ILogger<QBCHValidationDispatcherV3> _logger = logger;

    /// <summary>
    /// Выполняет полную валидацию транзакции dlrequest.
    /// При критической ошибке транзакция помечается как <see cref="QBCHProcessingStatus.Failure"/>
    /// </summary>
    /// <param name="transaction">Обрабатываемая транзакция, накапливающая результаты валидации.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных проверок (права доступа, уникальность идентификатора запроса).</param>
    /// <returns>Транзакция с проставленным статусом и, при наличии, списком ошибок валидации (критических и пакетных).</returns>
    public async Task<QBCHProcessingTransactionV3> ValidateV3(QBCHProcessingTransactionV3 transaction, CancellationToken cancellationToken)
    {
        // method
        ValidateRequestMethodV3(transaction);

        // body
        ValidateRequestBodyV3(transaction);

        // sign
        ProcessSignV3(transaction);

        // xsd
        _xsdValidator.ValidateXml(transaction);

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        // abonent
        await ValidateAbonentV3(transaction);

        // packet
        _xmlRequestCollectionValidator.ValidateXmlRequestCollectionV3(transaction, requestV3);

        // rights
        await ValidateRightsV3(transaction, cancellationToken);

        // one-window
        ValidateOneWindowV3(transaction);

        // antifraud one-window compatibility
        ValidateAntifraudOneWindowCompatibilityV3(transaction, requestV3);

        // unique request id
        await ValidateUniqueRequestIdV3(transaction, requestV3);

        // request date
        ValidateRequestDateV3(transaction, requestV3);

        // additional validation
        _additionalValidator.AdditionalValidationV3(transaction, requestV3);

        // agreement
        _consentValidator.ValidateConsentV3(transaction, requestV3);

        // inn/self-prohibition
        _selfLockedUpValidator.ValidateInnAndSelfProhibitionV3(transaction, requestV3);

        transaction.ValidationComplete();
        return transaction;
    }

    private void ValidateRequestMethodV3(QBCHProcessingTransactionV3 transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !string.Equals(transaction.ClentRequest.RequestMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            var error = AnswerErrorCode.Code1_WrongRequestMethod();

            _logger.LogError("Не пройдена проверка метода запроса dlrequest v3: получен {RequestMethod}, ожидался POST. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
               transaction.ClentRequest.RequestMethod, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ValidateRequestBodyV3(QBCHProcessingTransactionV3 transaction)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && (transaction.Attachment.SignedRequestBody is null || transaction.Attachment.SignedRequestBody.Length == 0))
        {
            var error = AnswerErrorCode.Code2_EmptyRequestBody();

            _logger.LogError("Не пройдена проверка тела запроса dlrequest v3: тело запроса пустое. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ValidateAntifraudOneWindowCompatibilityV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
            return;

        if (requestV3.КодСведений == СправочникВидыСведений.Item8 && requestV3.ТипЗапроса == СправочникСпособыЗапросаV3.Item2)
        {
            var error = AnswerErrorCode.Code99_OtherError("Комбинация КодСведений=\"8\" и ТипЗапроса=\"2\" недопустима");

            _logger.LogError("Не пройдена проверка совместимости антифрода и одного окна dlrequest v3: КодСведений={КодСведений}, ТипЗапроса={ТипЗапроса}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                requestV3.КодСведений, requestV3.ТипЗапроса, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ProcessSignV3(QBCHProcessingTransactionV3 transaction)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            return;
        }

        var signValidationResult = _cryptoService.ValidateMsg(
            transaction.Attachment.SignedRequestBody!,
            transaction.ClentRequest.Certificate);

        if (!signValidationResult.IsSuccess)
        {
            _logger.LogError("Не пройдена проверка УЭП dlrequest v3: сертификат={Thumbprint}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.ClentRequest.Certificate?.Thumbprint, transaction.Id, signValidationResult.Error!.Code, signValidationResult.Error.Message);

            transaction.RiseCriticalError(new AnswerErrorCode(signValidationResult.Error!.Code, signValidationResult.Error.Message));
            return;
        }

        transaction.Attachment.SetRequestBody(signValidationResult.Value.Body);
        transaction.Attachment.SetSignCertificateData(
            signValidationResult.Value.SignThumbprint,
            signValidationResult.Value.SignINN,
            signValidationResult.Value.SignOGRN);
        transaction.ClentRequest.SetRequestCertificateData(
            signValidationResult.Value.RequestINN,
            signValidationResult.Value.RequestOGRN);

        if (!_validationService.ValidateEncodingV3(transaction.Attachment.RequestBody!, out var encodingValidationResult))
        {
            _logger.LogError("Не пройдена проверка кодировки dlrequest v3: тело запроса не является корректным UTF-8. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка");

            transaction.RiseCriticalError(new AnswerErrorCode(encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка"));
        }
    }


    private async Task ValidateAbonentV3(QBCHProcessingTransactionV3 transaction)
    {
        //Это проверка сравнения полей ИНН и ОГРН из сертификата с ИНН и ОГРН из запроса, а не из базы
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure))
            return;

        var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();

        if (requestV3 is null)
        {
            var noRequestError = AnswerErrorCode.Code99_OtherError("Отсутствуют данные запроса");

            _logger.LogError("Не пройдена проверка реквизитов абонента dlrequest v3: отсутствуют данные запроса. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.Id, noRequestError.Code, noRequestError.Message);

            transaction.RiseCriticalError(noRequestError);
            return;
        }

        var requestINN = transaction.ClentRequest?.RequestINN;
        var requestOGRN = transaction.ClentRequest?.RequestOGRN;

        var (abonentINN, abonentOGRN) = GetAbonentRequisitesV3(requestV3);

        // ИНН и ОГРН из сертификата сравнивается с ИНН и ОГРН в запросе
        if (requestINN != abonentINN || requestOGRN != abonentOGRN)
        {
            var error = AnswerErrorCode.Code10_RequestAndAbonentDataNotMach(abonentINN, requestINN, abonentOGRN, requestOGRN);

            _logger.LogError("Не пройдена проверка реквизитов абонента dlrequest v3: ИНН сертификата={RequestINN}, ИНН запроса={AbonentINN}, ОГРН сертификата={RequestOGRN}, ОГРН запроса={AbonentOGRN}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                requestINN, abonentINN, requestOGRN, abonentOGRN, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private async Task ValidateRightsV3(QBCHProcessingTransactionV3 transaction, CancellationToken cancellationToken)
    {
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && !await _repository.IsPermissionGrantedV3(transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, cancellationToken))
        {
            var error = AnswerErrorCode.Code22_AccessDenied();

            _logger.LogError("Не пройдена проверка прав доступа dlrequest v3: сертификат={Thumbprint}, сервис={QbchService}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ValidateOneWindowV3(QBCHProcessingTransactionV3 transaction)
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

            _logger.LogError("Не пройдена проверка одного окна dlrequest v3: взаимодействие в режиме «одно окно» не предусмотрено договором с абонентом ОГРН={RequestOGRN}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                            requestOgrn, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private async Task ValidateUniqueRequestIdV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
    {
        if (transaction.Status.Equals(QBCHProcessingStatus.Failure) || requestV3 is null)
        {
            return;
        }

        var requestOgrn = GetAbonentRequisitesV3(requestV3).ogrn;
        var isUniqueRequest = await _cacheService.IsUniqueRequestId(requestV3.ИдентификаторЗапроса, requestOgrn ?? string.Empty, RedisConstants.DlRequestV3Scope);

        if (!isUniqueRequest)
        {
            var error = AnswerErrorCode.Code11_RequestIdIsNotUnique();

            _logger.LogError("Не пройдена проверка уникальности идентификатора запроса dlrequest v3: ИдентификаторЗапроса={RequestId}, ОГРН абонента={RequestOGRN}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                requestV3.ИдентификаторЗапроса, requestOgrn, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }
    }

    private void ValidateRequestDateV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3)
    {
        _logger.LogDebug("Начало проверки даты запроса. requestDate={requestDate}, method={methodName}", requestV3?.ДатаЗапроса, nameof(ValidateRequestDateV3));

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && requestV3 is not null && !_validationService.ValidateRequestDateV3(requestV3.ДатаЗапроса, out var dateValidationResult))
        {
            var error = new AnswerErrorCode(dateValidationResult!.ErrorCode, dateValidationResult.Error ?? "Дата запроса указана некорректно");

            _logger.LogError("Не пройдена проверка даты запроса: ДатаЗапроса={RequestDate}. TransactionId={TransactionId}, code={QbchErrorCode}: {QbchErrorMessage}",
                requestV3.ДатаЗапроса, transaction.Id, error.Code, error.Message);

            transaction.RiseCriticalError(error);
        }

        _logger.LogDebug("Дата запроса корректна. requestDate={requestDate}, method={methodName}", requestV3?.ДатаЗапроса, nameof(ValidateRequestDateV3));
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