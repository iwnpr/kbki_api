using System.Xml.Linq;

namespace Application_lib
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDBAdapter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        Task<XElement?> GetInnOgrnByThumbprint(string? thumbprint);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="psrn"></param>
        /// <returns></returns>
        Task<int?> GetAbonentKeyIdByPSRN(string? psrn);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="timeLeftMs"></param>
        /// <returns></returns>
        Task<List<long>> GetSearchAllSubjects(string request, long? timeLeftMs = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subjectIds"></param>
        /// <param name="timeLeftMs"></param>
        /// <returns></returns>
        Task<XElement?> GetCalculationOfAmp(List<long> subjectIds, long? timeLeftMs = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subjectIds"></param>
        /// <param name="timeLeftMs"></param>
        /// <returns></returns>
        Task<XElement?> GetSelfProhibition(List<long> subjectIds, long? timeLeftMs = null);
        Task<bool> IsPermissionGrantedv2(string? thumbprint, string? serviceName, CancellationToken? ct = null);
        /// <summary>
        /// Проверка существует ли сертификат
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="thumbprint">Отпечаток</param>
        /// <returns>true/false существует ли сертификат в БД</returns>
        Task<bool> IsCertExist(byte[] cert);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="abonentId"></param>
        /// <param name="thumbprint"></param>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        Task<bool> AddCertificate(int abonentId, string thumbprint, DateTime expirationDate);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<bool> SetCertificateInactive(string thumbprint);
        Task<XElement?> GetDlputData(string xml, long? timeLeft);
    }
}
