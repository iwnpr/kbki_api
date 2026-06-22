using Cache_lib.Interfaces;
using Crypto_lib.Model;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using qbch_lib.domain.errors;
using QBCH_lib.CommonTypes.Api.V3;
using QBCH_lib.Services.Interfaces.V3;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.Services.Implementations.V3;

public class ValidationServiceV3(
    IXmlServiceV3 xmlService,
    ICryptoService cryptoService,
    IKeyValueStorageService cache,
    IRepositoryV3 repository,
    ITicketServiceV3 ticketService,
    ILogger<ValidationServiceV3> logger) : IValidationServiceV3
{
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IKeyValueStorageService _cache = cache;
    private readonly IRepositoryV3 _repository = repository;
    private readonly ITicketServiceV3 _ticketService = ticketService;
    private readonly ILogger<ValidationServiceV3> _logger = logger;

    public bool ValidateXmlV3(MemoryStream memoryStream, string nameOfController, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var isValid = _xmlService.ValidateXmlV3(memoryStream, nameOfController, out var xmlResult);

        if (!isValid)
        {
            var error = new AnswerErrorCode(xmlResult.ErrorCode, xmlResult.Error);
            result = CreateErrorResult(error);
            return false;
        }

        result = null;
        return true;

    }

    public bool ValidateEncodingV3(byte[] message, [NotNullWhen(false)] out BaseResultV3? result)
    {
        try
        {
            var encoding = new UTF8Encoding(false, true);
            encoding.GetCharCount(message);
        }
        catch (DecoderFallbackException ex)
        {
            var error = AnswerErrorCode.Code8_UnsupportedEncoding();
            _logger.LogError(ex, error.Message);
            result = CreateErrorResult(error);

            return false;
        }

        result = null;
        return true;
    }

    public bool ValidateRequestDateV3(DateTime? requestDate, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var currentMoscowDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTimeZone).Date;

        if (requestDate?.Date != currentMoscowDate)
        {
            var error = AnswerErrorCode.Code23_InvalidRerquestDate();
            _logger.LogError(error.Message);

            result = CreateErrorResult(error);
            return false;
        }

        result = null;
        return true;
    }

    public bool ValidateMsgV3(byte[] msg, X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult result, byte[]? encodedSignature = null)
    {
        return _cryptoService.ValidateMsg(msg, requestCert, out result, encodedSignature);
    }

    /// <summary>
    /// Проверка сертификата
    /// </summary>
    /// <param name="requestCert"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool ValidateCertificateV3(X509Certificate2? requestCert, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var isValid = _cryptoService.ValidateCertificate(requestCert, out CryptoServiceResult? certResult);

        if (!isValid)
        {
            var code = certResult.ErrorCode;
            var message = certResult.Error;

            result = CreateErrorResult(new AnswerErrorCode(code, message));
            return false;
        }
        result = null;
        return true;
    }

    public async Task<bool> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null) => await _repository.IsPermissionGrantedV3(thumbprint, serviceName, ct);

    public async Task<(bool IsUnique, BaseResultV3? Error)> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn)
    {
        var isUnique = await _cache.IsUniqueRequestId(requestId, ogrn, methodName);

        if (isUnique)
            return (true, null);

        return (false, CreateErrorResult(AnswerErrorCode.Code11_RequestIdIsNotUnique()));
    }

    public async Task<bool> IsCertExistsV3(byte[] cert) => await _repository.IsCertExist(cert);

    public async Task<bool> IsCertActiveV3(string thumbprint) => await _repository.IsCertActive(thumbprint);

    public async Task<int> GetActiveCertificatesCountV3(byte[] cert)
    {
        if (cert.Length == 0)
        {
            _logger.LogError("Невозможно получить количество активных сертификатов v3: cert пустой");
            return 0;
        }

        var certificate = new X509Certificate2(cert);
        return await _repository.GetActiveCertificatesCountByThumbprint(certificate.Thumbprint ?? string.Empty);
    }

    public async Task<bool> SetCertificateInactiveV3(byte[] cert)
    {
        if (cert.Length == 0)
        {
            _logger.LogError("Невозможно деактивировать сертификат v3: cert пустой");
            return false;
        }

        var certificate = new X509Certificate2(cert);
        return await _repository.SetCertificateInactive(certificate.Thumbprint ?? string.Empty);
    }
    private BaseResultV3 CreateErrorResult(AnswerErrorCode error)
    {
        return new BaseResultV3
        {
            IsError = true,
            ErrorCode = error.Code,
            Error = error.Message,
            ErrorMessage = error.Message,
            TicketV3 = _ticketService.CreateResultV3Error(error)
        };
    }
}
