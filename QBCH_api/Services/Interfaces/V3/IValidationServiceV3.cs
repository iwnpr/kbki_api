using Crypto_lib.Model;
using QBCH_lib.core;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Services.Interfaces.V3;

/// <summary>Проверки входящего запроса API 3.0 с возвратом результата без формирования квитанций.</summary>
public interface IValidationServiceV3
{
    /// <summary>Проверяет корректность кодировки UTF-8.</summary>
    /// <param name="message">Тело сообщения после снятия подписи.</param>
    /// <returns>Успешный результат со значением true либо ошибка проверки кодировки.</returns>
    Result<bool> ValidateEncodingV3(byte[] message);
    /// <summary>Проверяет совпадение даты запроса с текущей датой по московскому времени.</summary>
    /// <param name="requestDate">Дата из запроса.</param>
    /// <returns>Успешный результат со значением true либо ошибка проверки даты.</returns>
    Result<bool> ValidateRequestDateV3(DateTime? requestDate);
    /// <summary>Проверяет сертификат средствами криптосервиса.</summary>
    /// <param name="requestCert">Сертификат клиентского запроса.</param>
    /// <returns>Успешный результат со значением true либо ошибка проверки сертификата.</returns>
    Result<bool> ValidateCertificateV3(X509Certificate2? requestCert);
    /// <summary>Проверяет подпись сообщения и соответствие сертификату запроса.</summary>
    /// <param name="msg">Подписанное сообщение или данные с отсоединённой подписью.</param>
    /// <param name="requestCert">Сертификат клиентского запроса.</param>
    /// <param name="encodedSignature">Отсоединённая подпись; null для сообщения со встроенной подписью.</param>
    /// <returns>Данные криптографической проверки либо ошибка проверки подписи.</returns>
    Result<CryptoServiceResult> ValidateMsgV3(byte[] msg, X509Certificate2? requestCert, byte[]? encodedSignature = null);
    /// <summary>Проверяет право сертификата на обращение к сервису.</summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="serviceName">Имя сервиса.</param>
    /// <param name="ct">Необязательный токен отмены операции хранилища.</param>
    /// <returns>Успешный результат со значением true либо ошибка доступа или выполнения проверки.</returns>
    Task<Result<bool>> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null);
    /// <summary>Проверяет уникальность идентификатора запроса для абонента и метода.</summary>
    /// <param name="requestId">Идентификатор запроса.</param>
    /// <param name="methodName">Имя метода API.</param>
    /// <param name="ogrn">ОГРН абонента.</param>
    /// <returns>Успешный результат со значением true либо ошибка повторного идентификатора или выполнения проверки.</returns>
    Task<Result<bool>> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn);
}
