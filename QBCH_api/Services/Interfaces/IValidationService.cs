using Asp.Versioning;
using Crypto_lib.Model;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.qcb_request;
using QBCH_lib.qcb_xml.v3_0.qcb_request;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Services.Interfaces
{
    /// <summary>
    /// Сервис валидации
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Валидация сертификата
        /// </summary>
        /// <param name="requestCert">Сертификат из запроса</param>
        /// <param name="result">Результат</param>
        /// <returns></returns>
        bool ValidateCertificate(X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult? result);

        /// <summary>
        /// Проверка подписи файла
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="requestCertificate">Сертификат из запроса</param>
        /// <param name="result"></param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        bool ValidateMsg(byte[] msg, X509Certificate2? requestCertificate, [NotNullWhen(false)] out CryptoServiceResult result, byte[]? encodedSignature = null);

        /// <summary>
        /// Валидация XML по XSD
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="nameOfController"></param>
        /// <param name="apiVersion"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ValidateXml(MemoryStream memoryStream, string nameOfController, ApiVersion apiVersion, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация совпадения даты запроса в xml с текущей
        /// </summary>
        /// <param name="requestDate">Дата запроса</param>
        /// <param name="result">Результат валидации</param>
        /// <returns>Дата совпадает/нет</returns>
        bool ValidateRequestDate(DateTime? requestDate, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация кодировки
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ValidateEncoding(byte[] message, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация согласия в запросе
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ValidateAgreement(ЗапросСведенийОПлатежах? request, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация согласия в запросе
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool ValidateAgreement(ЗапросСведенийЗапрос request, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Проверка запроса на содержание ошибок не выявляющихся xsd
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result">Ответ</param>
        /// <returns>Результат проверки</returns>
        bool AdditionalValidation(ЗапросСведенийОПлатежах? request, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Проверка запроса на содержание ошибок не выявляющихся xsd
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result">Ответ</param>
        /// <returns>Результат проверки</returns>
        bool AdditionalValidation(ЗапросСведенийЗапрос request, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация запроса
        /// </summary>
        /// <param name="thumbprint">Отпечаток сертификата</param>
        /// <param name="inn">ИНН</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="result"></param>
        /// <returns>Результат проверки</returns>
        bool AbonentValidation(string? thumbprint, string? inn, string? ogrn, out AbonentValidatationResult result);

        /// <summary>
        /// Валидация запроса на предоставление смп
        /// </summary>
        /// <param name="thumbprint">Отпечаток</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool AbonentValidation(string? thumbprint, string? ogrn, out AbonentValidatationResult result);

        /// <summary>
        /// Являестся ли запрос от организации уникальным в течение дня
        /// </summary>
        /// <param name="requestId">Id запроса</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="methodName">наименование методы из которого прилетел запрос</param>
        /// <param name="result">Результат</param>
        /// <returns></returns>
        bool IsUniqueRequestId(string requestId, string methodName, string ogrn, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="ct"></param>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        public Task<bool> ValidateRules(string? thumbprint, string? serviceName, CancellationToken? ct = null);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ValidateQBCH(string? psrn, List<QBCHRequisite> QBCHList, [NotNullWhen(false)] out BaseResult? validationResult);

        /// <summary>
        /// Существует ли серт в БД
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        Task<bool> IsCertExists(byte[] cert);
    }
}
