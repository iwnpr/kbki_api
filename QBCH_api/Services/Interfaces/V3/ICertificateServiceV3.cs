using QBCH_lib.core;

namespace QBCH_api.Services.Interfaces.V3;

public interface ICertificateServiceV3
{
    Task<Result<bool>> IsCertExistsV3(byte[] cert);
    Task<Result<bool>> IsCertActiveV3(string thumbprint);
    Task<Result<int>> GetActiveCertificatesCountV3(byte[] cert);
    Task<Result<bool>> SetCertificateInactiveV3(byte[] cert);
}
