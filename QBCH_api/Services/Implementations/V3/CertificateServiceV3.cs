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
    private readonly IRepositoryV3 _repository = repository;
    private readonly ILogger<CertificateServiceV3> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<bool>> IsCertExistsV3(byte[] cert)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.IsCertExistsV3: начало проверки наличия сертификата, certLength={certLength}", cert.Length);
            var exists = await _repository.IsCertExist(cert);
            _logger.LogDebug("ValidationServiceV3.IsCertExistsV3 результат: exists={exists}", exists);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить операцию с сертификатом API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsCertActiveV3(string thumbprint)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.IsCertActiveV3: начало проверки активности сертификата, thumbprint={thumbprint}", thumbprint);
            var isActive = await _repository.IsCertActive(thumbprint);
            _logger.LogDebug("ValidationServiceV3.IsCertActiveV3 результат: isActive={isActive}, thumbprint={thumbprint}", isActive, thumbprint);
            return Result<bool>.Success(isActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить операцию с сертификатом API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> GetActiveCertificatesCountV3(byte[] cert)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.GetActiveCertificatesCountV3: начало подсчета активных сертификатов, certLength={certLength}", cert.Length);

            if (cert.Length == 0)
            {
                _logger.LogError("Невозможно получить количество активных сертификатов v3: входящий сертификат пустой (0 байт). Возвращено количество активных сертификатов = 0");
                return Result<int>.Success(0);
            }

            using var certificate = new X509Certificate2(cert);

            _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3: сертификат разобран, thumbprint={thumbprint}", certificate.Thumbprint);
            var result = await _repository.GetActiveCertificatesCountByThumbprint(certificate.Thumbprint ?? string.Empty);
            _logger.LogDebug("ValidationServiceV3.GetActiveCertificatesCountV3: количество активных сертификатов: {success}, thumbprint={thumbprint}", result, certificate.Thumbprint);
            return Result<int>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить операцию с сертификатом API 3.0");
            return Result<int>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> SetCertificateInactiveV3(byte[] cert)
    {
        try
        {
            if (cert.Length == 0)
            {
                _logger.LogError("Невозможно отозвать сертификат v3: входящий сертификат пустой (0 байт). Отзыв не выполнен");
                return Result<bool>.Success(false);
            }

            using var certificate = new X509Certificate2(cert);
            _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3: начало деактивации сертификата, thumbprint={thumbprint}", certificate.Thumbprint);
            var success = await _repository.SetCertificateInactive(certificate.Thumbprint ?? string.Empty);

            _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3 результат: {success}, thumbprint={thumbprint}", success, certificate.Thumbprint);
            return Result<bool>.Success(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить операцию с сертификатом API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }
}
