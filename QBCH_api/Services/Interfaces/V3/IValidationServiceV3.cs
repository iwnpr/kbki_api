using Crypto_lib.Model;
using QBCH_lib.core;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Services.Interfaces.V3;

public interface IValidationServiceV3
{
    Result<bool> ValidateEncodingV3(byte[] message);
    Result<bool> ValidateRequestDateV3(DateTime? requestDate);
    Result<bool> ValidateCertificateV3(X509Certificate2? requestCert);
    Result<CryptoServiceResult> ValidateMsgV3(byte[] msg, X509Certificate2? requestCert, byte[]? encodedSignature = null);
    Task<Result<bool>> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null);
    Task<Result<bool>> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn);
}
