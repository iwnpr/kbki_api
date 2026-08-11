using System.Xml.Linq;

namespace Qbch_db_lib.Services.Interfaces.V3;

/// <summary>
/// 
/// </summary>
public interface IRepositoryV3
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<List<long>> GetSearchAllSubjectsV3(string request, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<XElement?> GetCalculationOfAmpV3(List<long> subjectIds, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<XElement?> GetSelfProhibitionV3(List<long> subjectIds, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="birthDate"></param>
    /// <param name="inn"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<XElement?> GetAntifraudV3(DateTime birthDate, string inn, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thumbprint"></param>
    /// <param name="serviceName"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> IsPermissionGrantedV3(string? thumbprint, string? serviceName, CancellationToken? ct = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thumbprint"></param>
    /// <returns></returns>
    Task<XElement?> GetInnOgrnByThumbprintV3(string? thumbprint);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cert"></param>
    /// <returns></returns>
    Task<bool> IsCertExist(byte[] cert);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thumbprint"></param>
    /// <returns></returns>
    Task<bool> IsCertActive(string thumbprint);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thumbprint"></param>
    /// <returns></returns>
    Task<int> GetActiveCertificatesCountByThumbprint(string thumbprint);

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
    /// <param name="thumbprint"></param>
    /// <returns></returns>
    Task<bool> SetCertificateInactive(string thumbprint);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="psrn"></param>
    /// <returns></returns>
    Task<int?> GetAbonentKeyIdByPSRN(string? psrn);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectXml"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<List<long>?> SearchContractSubjectsForDlPutV3(string subjectXml, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="uid"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<bool?> ContractUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="uid"></param>
    /// <param name="calculationDate"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<bool?> ContractCalculationDateExistsForSubjectsV3(List<long> subjectIds, string uid, DateTime calculationDate, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inn"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<List<long>?> SearchAppealSubjectsByInnForDlPutV3(string inn, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="uid"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<bool?> AppealUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="subjectIds"></param>
    /// <param name="uid"></param>
    /// <param name="stage"></param>
    /// <param name="timeLeftMs"></param>
    /// <returns></returns>
    Task<bool?> AppealStageExistsForSubjectsV3(List<long> subjectIds, string uid, ushort stage, long? timeLeftMs = null);
}