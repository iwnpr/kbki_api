using QBCH_lib.CommonTypes.Api;
using System.Text;

namespace Crypto_lib.Model
{
    /// <summary>
    /// Результат
    /// </summary>
    public class CryptoServiceResult : BaseResult
    {
        /// <summary>
        /// результат сравнения сертифкатов
        /// </summary>
        public StringBuilder? CertCompareResult { get; set; } //err

        /// <summary>
        /// Файл
        /// </summary>
        public byte[] Body { get; set; } = Array.Empty<byte>(); //att

        /// <summary>
        /// Подписанный файл
        /// </summary>
        public byte[]? SignedBody { get; set; }


        /// <summary>
        /// Отпечаток сертификата pfghjcf
        /// </summary>
        public string? RequestThumbprint { get; set; } //cr

        /// <summary>
        /// ОГРН из запроса
        /// </summary>
        public string? RequestOGRN { get; set; } //cr

        /// <summary>
        /// ИНН из запроса
        /// </summary>
        public string? RequestINN { get; set; } //cr


        /// <summary>
        /// Отпечаток сертификата подписи
        /// </summary>
        public string? SignThumbprint { get; set; } //att

        /// <summary>
        /// ИНН из подписи
        /// </summary>
        public string? SignINN { get; set; } //att

        /// <summary>
        /// ОГРН подписанта
        /// </summary>
        public string? SignOGRN { get; set; } //att
    }
}
