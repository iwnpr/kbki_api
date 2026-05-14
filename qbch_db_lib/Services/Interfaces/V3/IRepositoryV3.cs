using QBCH.Lib.qcb_xml.v3_0;
using System.Xml.Linq;

namespace Qbch_db_lib.Services.Interfaces.V3;

public interface IRepositoryV3
{
    Task<List<long>> GetSearchAllSubjectsV3(string request, long? timeLeftMs = null);
    Task<XElement?> GetCalculationOfAmpV3(List<long> subjectIds, long? timeLeftMs = null);
    Task<XElement?> GetSelfProhibitionV3(List<long> subjectIds, long? timeLeftMs = null);
    Task<XElement?> GetAntifraudV3(List<long> subjectIds, long? timeLeftMs = null);
    Task<bool> IsPermissionGrantedV3(string? thumbprint, string? serviceName, CancellationToken? ct = null);
    Task<XElement?> GetInnOgrnByThumbprintV3(string? thumbprint);
    Task<bool> IsCertExist(byte[] cert);
    Task<bool> IsCertActive(string thumbprint);
    Task<int> GetActiveCertificatesCountByThumbprint(string thumbprint);
    Task<bool> AddCertificate(int abonentId, string thumbprint, DateTime expirationDate);
    Task<bool> SetCertificateInactive(string thumbprint);
    Task<int?> GetAbonentKeyIdByPSRN(string? psrn);

    Task<List<long>?> SearchContractSubjectsForDlPutV3(string subjectXml, long? timeLeftMs = null);
    Task<bool?> ContractUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null);
    Task<bool?> ContractCalculationDateExistsForSubjectsV3(List<long> subjectIds, string uid, DateTime calculationDate, long? timeLeftMs = null);
    Task<List<long>?> SearchAppealSubjectsByInnForDlPutV3(string inn, long? timeLeftMs = null);
    Task<bool?> AppealUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null);
    Task<bool?> AppealStageExistsForSubjectsV3(List<long> subjectIds, string uid, ushort stage, long? timeLeftMs = null);
}