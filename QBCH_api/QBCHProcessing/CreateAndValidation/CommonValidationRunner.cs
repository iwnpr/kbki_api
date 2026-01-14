using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Asp.Versioning;
using Domain;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.CryptoModels;
using QBCH_api.QBCHProcessing.CreateAndValidation.CommonValidationSteps;
using QBCH_api.QBCHProcessing.CreateAndValidation.DlRequestValidationMediatr.ValidationSteps;

namespace QBCH_api.QBCHProcessing.CreateAndValidation;

/// <summary>
/// 
/// </summary>
public static class CommonValidationRunner
{
    /// <summary>
    /// Пошаговый вызов валидации агрегата созданного в dlrequest при запросе
    /// </summary>
    /// <param name="transaction">Агрегат</param>
    /// <param name="cryptoValidate">Валидация криптографии</param>
    /// <param name="xmlValidate">Валидация xsd/xml</param>
    /// <param name="apiVersion">Версия апи</param>
    /// <param name="getInnOgrnByThumbprint">Получение огрн/инн по отпечатку серта</param>
    /// <param name="isPermissionGranted">Проверка прав доступа на запрос</param>
    /// <param name="isUniqueRequestId">Проверка уникальности идентификатора запроса</param>
    /// <returns>Результат валидации</returns>
    public static QBCHProcessingTransaction ValidateDlRequest(this QBCHProcessingTransaction transaction,
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
                .CommonProcessSign(cryptoValidate) //4 - УЭП некорректна 5 - Истек срок сертификата УЭП 6 - УЭП не соответствует абоненту
                .DlRequestValidateXML(xmlValidate, apiVersion.ToString()) //9 - Запрос не соответствует схеме
                .DlrequestAbonentValidation(getInnOgrnByThumbprint).Result //10 - Реквизиты запроса не соответствуют абоненту
                .ValidateXMLRequestCollection() //26 - Количество блоков "Запрос" не соответствует режиму запроса
                .ValidateAccessRights(isPermissionGranted).Result //22 - Запрос не доступен для абонента
                .ValidateUniqueRequestId(isUniqueRequestId).Result //11 - Идентификатор запроса не уникален
                .ValidateRequestDate() //23 - Дата запроса указана некорректно
                .AdditionalValidation() //15 - Запрос содержит некорректные данные
                .ValidateAgreement() //13 - Отсутствует действующее согласие Субъекта 27 - Отсутствует согласие субъекта
                .SelfLockedUpValidate(); //25 - Сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН

        result.ValidationComplete(); //Смена статуса транзакции на провадилировано
        return result;
    }
}
