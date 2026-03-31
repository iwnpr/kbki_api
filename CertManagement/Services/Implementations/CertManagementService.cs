using CertManagement.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.Services.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace CertManagement.Services.Implementations
{
    /// <summary>
    /// Менеджмент сертификатов
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    /// <param name="logger">
    /// Logger
    /// </param>
    /// <param name="repository">
    /// Контекст БД
    /// </param>        
    /// <param name="ticketService">
    /// Сервис тикетов
    /// </param>
    public class CertManagementService(
        ILogger<CertManagementService> logger,
        IRepository repository,
        ITicketService ticketService) : ICertManagementService
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
