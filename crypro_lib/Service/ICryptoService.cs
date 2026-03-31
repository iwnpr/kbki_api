using Crypto_lib.Model;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Crypto_lib.Service
{
    /// <summary>
    /// Интерфейс сервиса подписания
    /// </summary>
    public interface ICryptoService
    {
        /// <summary>
        /// Проверка подписи файла (Перегрузка под возврат Result)
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="requestCert">Сертифкат из запроса</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        public QBCH_lib.core.Result<CryptoServiceResult> ValidateMsg(byte[] msg, X509Certificate2? requestCert, byte[]? encodedSignature = null);
        /// <summary>
        /// Проверка подписи файла
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="requestCert">Сертифкат из запроса</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        bool ValidateMsg(byte[] msg, X509Certificate2? requestCert, out CryptoServiceResult result, byte[]? encodedSignature = null);

        /// <summary>
        /// Проверка подписи файла без сравнения сертификатов
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        bool ValidateMsg(byte[] msg, out CryptoServiceResult result, byte[]? encodedSignature = null, CancellationToken? ct = null);

        /// <summary>
        /// Подписываем сообщение секретным ключем.
        /// </summary>
        /// <param name="msg">Сообщение в формет byte</param>
        /// <param name="signerCert">Сертифкат подписанта</param>
        /// <returns>Подписанный файл</returns>
        byte[] SignMsg(byte[] msg);

        bool ValidateCertificate(X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult? result);
    }
}
