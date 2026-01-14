namespace Application_lib
{
    /// <summary>
    /// Сервис для управления сертификатами
    /// </summary>
    public interface ICertManagementService
    {
        /// <summary>
        /// Добавить новый сертификат
        /// </summary>
        /// <returns></returns>
        Task<bool> AddCertificate(byte[] certificate, string? ogrn, string guid);

        /// <summary>
        /// Сделать сертификат неактивным
        /// </summary>
        /// <param name="certificate">Сертификат</param>
        /// <param name="guid"></param>
        /// <returns>Успешность операции</returns>
        Task<bool> SetCertificateInactive(byte[] certificate, string guid);
    }
}
