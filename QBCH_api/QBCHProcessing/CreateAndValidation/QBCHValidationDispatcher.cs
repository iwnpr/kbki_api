using Asp.Versioning;
using Crypto_lib.Model;
using QBCH_api.QBCHProcessing.CreateAndValidation.ValidationStep;
using QBCH_api.QBCHProcessing.ProcessingStep;
using QBCH_lib.core;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace QBCH_api.QBCHProcessing.CreateAndValidation;

/// <summary>
/// 
/// </summary>
public static class QBCHValidationDispatcher
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cryptoValidate"></param>
    /// <param name="xmlValidate"></param>
    /// <param name="apiVersion"></param>
    /// <param name="getInnOgrnByThumbprint"></param>
    /// <param name="isPermissionGranted"></param>
    /// <param name="isUniqueRequestId"></param>
    /// <returns></returns>
    public static QBCHProcessingTransaction Validate(this QBCHProcessingTransaction transaction,
        Func<byte[], X509Certificate2?, byte[]?, Result<CryptoServiceResult>> cryptoValidate,
        Func<MemoryStream, string, string, Result> xmlValidate,
        ApiVersion apiVersion,
        Func<string?, Task<XElement>> getInnOgrnByThumbprint,
        Func<string?, string?, CancellationToken?, Task<bool>> isPermissionGranted,
        Func<string, string, string, int?, Task<bool>> isUniqueRequestId)
    {
        var result =
            transaction
                .ValidateRequestMethod() //1 - Метод передачи запроса не соответствует ожидаемому
                .ValidateRequestBodyLength() //2 - Запрос не содержит данных
                .ProcessSign(cryptoValidate) //4 - УЭП некорректна 5 - Истек срок сертификата УЭП 6 - УЭП не соответствует абоненту
                .ValidateXML(xmlValidate, apiVersion.ToString()) //9 - Запрос не соответствует схеме
                .AbonentValidation(getInnOgrnByThumbprint).Result //10 - Реквизиты запроса не соответствуют абоненту
                .ValidateXMLRequestCollection() //26 - Количество блоков "Запрос" не соответствует режиму запроса
                .ValidateAccessRights(isPermissionGranted).Result //22 - Запрос не доступен для абонента
                .ValidateQBCH() //14 - Взаимодействие с абонентом в режиме "одно окно" не предусмотрено договором
                .ValidateUniqueRequestId(isUniqueRequestId).Result //11 - Идентификатор запроса не уникален
                .ValidateRequestDate() //23 - Дата запроса указана некорректно
                .AdditionalValidation() //15 - Запрос содержит некорректные данные
                .ValidateAgreement() //13 - Отсутствует действующее согласие Субъекта 27 - Отсутствует согласие субъекта
                .SelfLockedUpValidate(); //25 - Сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН

        if (!result.Status.Equals(QBCHProcessingStatus.Failure) && result.GetRequest<ЗапросСведений>() is null)
        {
            result.RiseCriticalError(Error.Code9_InvalidRequestByScheme());
        }

        result.ValidationComplete(); //Смена статуса транзакции на провадилировано
        return result;
    }

}
