using QBCH_lib.CommonTypes.Api.V3;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Services.Interfaces.V3;

public interface IValidationServiceV3
{
    bool ValidateXmlV3(MemoryStream memoryStream, string nameOfController, [NotNullWhen(false)] out BaseResultV3? result);

    bool ValidateEncodingV3(byte[] message, [NotNullWhen(false)] out BaseResultV3? result);

    bool ValidateRequestDateV3(DateTime? requestDate, [NotNullWhen(false)] out BaseResultV3? result);

    bool ValidateCertificateV3(X509Certificate2? requestCert, [NotNullWhen(false)] out BaseResultV3? result);

    Task<bool> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null);

    Task<(bool IsUnique, BaseResultV3? Error)> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn);

    Task<bool> IsCertExistsV3(byte[] cert);

    Task<bool> IsCertActiveV3(string thumbprint);

    Task<int> GetActiveCertificatesCountV3(byte[] cert);

    Task<bool> SetCertificateInactiveV3(byte[] cert);
}
