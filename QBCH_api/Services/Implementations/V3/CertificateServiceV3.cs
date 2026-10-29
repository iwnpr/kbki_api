using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.core;
using qbch_lib.domain.errors;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>Чтение и изменение состояния сертификатов в хранилище.</summary>
public class CertificateServiceV3(IRepositoryV3 repository, ILogger<CertificateServiceV3> logger)
    : ICertificateServiceV3
{
    public Task<Result<bool>> IsCertExistsV3(byte[] cert) => ExecuteAsync(() => repository.IsCertExist(cert));

    public Task<Result<bool>> IsCertActiveV3(string thumbprint) => ExecuteAsync(() => repository.IsCertActive(thumbprint));

    public Task<Result<int>> GetActiveCertificatesCountV3(byte[] cert) => ExecuteAsync(async () =>
    {
        using var certificate = new X509Certificate2(cert);
        return await repository.GetActiveCertificatesCountByThumbprint(certificate.Thumbprint);
    });

    public Task<Result<bool>> SetCertificateInactiveV3(byte[] cert) => ExecuteAsync(async () =>
    {
        using var certificate = new X509Certificate2(cert);
        return await repository.SetCertificateInactive(certificate.Thumbprint);
    });

    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return Result<T>.Success(await operation());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось выполнить операцию с сертификатом API 3.0");
            return Result<T>.Failure(AnswerErrorCode.Code99_OtherError("Не удалось выполнить операцию с сертификатом."));
        }
    }
}
