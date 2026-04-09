using Cache_lib.Interfaces;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.V3;

/// <summary>
/// Отдельный диспетчер валидации для API 3.0.
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
        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !string.Equals(transaction.ClentRequest.RequestMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            transaction.RiseCriticalError(Error.Code1_WrongRequestMethod());
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            (transaction.Attachment.RequestBody is null || transaction.Attachment.RequestBody.Length == 0))
        {
            transaction.RiseCriticalError(Error.Code2_EmptyRequestBody());
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !validationService.ValidateCertificateV3(transaction.ClentRequest.Certificate, out var certValidationResult))
        {
            transaction.RiseCriticalError(new Error(certValidationResult!.ErrorCode, certValidationResult.Error ?? "Ошибка проверки сертификата"));
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            transaction.Attachment.RequestBody is not null &&
            !validationService.ValidateEncodingV3(transaction.Attachment.RequestBody, out var encodingValidationResult))
        {
            transaction.RiseCriticalError(new Error(encodingValidationResult!.ErrorCode, encodingValidationResult.Error ?? "Неподдерживаемая кодировка"));
        }

        ЗапросСведенийV3? v3Request = null;

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure))
        {
            using var xmlStream = new MemoryStream(transaction.Attachment.RequestBody!);
            if (!validationService.ValidateXmlV3(xmlStream, transaction.ServiceName, out var xmlValidationResult))
            {
                transaction.RiseCriticalError(new Error(xmlValidationResult!.ErrorCode, xmlValidationResult.Error ?? "Запрос не соответствует схеме"));
            }
            else
            {
                v3Request = xmlService.DeserializeV3<ЗапросСведенийV3>(transaction.Attachment.RequestBody);
                if (v3Request is null)
                {
                    transaction.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
                }
                else
                {
                    transaction.ClentRequest.SetRequestId(v3Request.ИдентификаторЗапроса);
                    transaction.ClentRequest.SetRequest(v3Request);

                    var (inn, ogrn) = GetAbonentRequisitesV3(v3Request);
                    transaction.ClentRequest.SetRequestCertificateData(
                        requestThumbprint: transaction.ClentRequest.Certificate?.Thumbprint,
                        requestInn: inn,
                        requestOgrn: ogrn);
                }
            }
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            !await repository.IsPermissionGrantedv2(transaction.ClentRequest.Certificate?.Thumbprint, transaction.ServiceName, cancellationToken))
        {
            transaction.RiseCriticalError(Error.Code22_AccessDenied());
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) &&
            v3Request is not null &&
            !validationService.ValidateRequestDateV3(v3Request.ДатаЗапроса, out var dateValidationResult))
        {
            transaction.RiseCriticalError(new Error(dateValidationResult!.ErrorCode, dateValidationResult.Error ?? "Дата запроса указана некорректно"));
        }

        if (!transaction.Status.Equals(QBCHProcessingStatus.Failure) && v3Request is not null)
        {
            var (_, ogrn) = GetAbonentRequisitesV3(v3Request);
            var uniqueScope = $"{transaction.ServiceName}:v{apiVersion}";
            var isUniqueRequest = await cacheService.IsUniqueRequestId(v3Request.ИдентификаторЗапроса, ogrn ?? string.Empty, uniqueScope);

            if (!isUniqueRequest)
            {
                transaction.RiseCriticalError(Error.Code11_RequestIdIsNotUnique());
            }
        }

        transaction.ValidationComplete();
        return transaction;
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