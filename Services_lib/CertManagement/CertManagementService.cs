using System.Security.Cryptography.X509Certificates;
using Application_lib;

namespace Services_lib.CertManagement
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    public class CertManagementService(IDBAdapter repository) : ICertManagementService
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
            X509Certificate2 cert = new(certificate);
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
            X509Certificate2 cert = new(certificate);
            return await repository.SetCertificateInactive(cert.Thumbprint);
        }
    }
}
