using CertManagement.Services.Interfaces;
using Qbch_db_lib.Services.Interfaces.V3;
using System.Security.Cryptography.X509Certificates;

namespace CertManagement.Services.Implementations
{
    /// <summary>
    /// Менеджмент сертификатов
    /// </summary>
    /// <param name="repository">
    /// Контекст БД
    /// </param>
    public class CertManagementService(IRepositoryV3 repository) : ICertManagementService
    {

        /// <summary>
        /// Добавить сертификаты
        /// </summary>
        /// <param name="certificate">Сертификат</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="guid"></param>
        /// <returns>Успешность операции</returns>
        public async Task<bool> AddCertificate(byte[] certificate, string? ogrn, string guid)
        {
            var abonentId = await repository.GetAbonentKeyIdByPSRN(ogrn);
            using X509Certificate2 cert = new(certificate);
            return await repository.AddCertificate(abonentId.Value, cert.Thumbprint, DateTime.Parse(cert.GetExpirationDateString()));
        }

        /// <summary>
        /// Сделать сертификат неактивным
        /// </summary>
        /// <param name="certificate">Сертификат</param>
        /// <param name="guid"></param>
        /// <returns>Успешность операции</returns>
        public async Task<bool> SetCertificateInactive(byte[] certificate, string guid)
        {
            using X509Certificate2 cert = new(certificate);
            return await repository.SetCertificateInactive(cert.Thumbprint);
        }
    }
}
