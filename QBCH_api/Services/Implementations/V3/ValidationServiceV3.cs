using Cache_lib.Interfaces;
using Crypto_lib.Model;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.CommonTypes.Api.V3;
using QBCH_lib.core;
using QBCH_lib.Services.Interfaces.V3;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.Services.Implementations.V3;

public class ValidationServiceV3(
    IXmlServiceV3 xmlService,
    ICryptoService cryptoService,
    ICacheService cache,
    IRepositoryV3 repository,
    ITicketServiceV3 ticketService,
    ILogger<ValidationServiceV3> logger) : IValidationServiceV3
{
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ICacheService _cache = cache;
    private readonly IRepositoryV3 _repository = repository;
    private readonly ITicketServiceV3 _ticketService = ticketService;
    private readonly ILogger<ValidationServiceV3> _logger = logger;

    public bool ValidateXmlV3(MemoryStream memoryStream, string nameOfController, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var isValid = _xmlService.ValidateXmlV3(memoryStream, nameOfController, out var xmlResult);

        if (isValid)
        {
            result = null;
            return true;
        }

        result = CreateErrorResult(new Error(xmlResult?.ErrorCode ?? 9, xmlResult?.Error ?? "Запрос не соответствует схеме"));
        return false;
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
            _logger.LogError(ex, "Неподдерживаемая кодировка, файл не в кодировке Utf-8");
            result = CreateErrorResult(new Error(8, "Неподдерживаемая кодировка, файл не в кодировке Utf-8"));
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
            _logger.LogError("Дата запроса указана некорректно. Передано: {requestDate}, текущая московская дата: {currentMoscowDate}",
                requestDate?.ToString("dd.MM.yyyy"), currentMoscowDate.ToString("dd.MM.yyyy"));
            result = CreateErrorResult(Error.Code23_InvalidRerquestDate());
            return false;
        }

        result = null;
        return true;
    }

    public bool ValidateCertificateV3(X509Certificate2? requestCert, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var isValid = _cryptoService.ValidateCertificate(requestCert, out CryptoServiceResult? certResult);
        if (isValid)
        {
            result = null;
            return true;
        }

        var code = certResult?.ErrorCode ?? 5;
        var message = certResult?.Error ?? "Ошибка проверки сертификата";
        result = CreateErrorResult(new Error(code, message));
        return false;
    }

    public async Task<bool> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null)
       => await _repository.IsPermissionGrantedV3(thumbprint, serviceName, ct);

    public bool IsUniqueRequestIdV3(string requestId, string methodName, string ogrn, [NotNullWhen(false)] out BaseResultV3? result)
    {
        var isUnique = _cache.IsUniqueRequestId(requestId, ogrn, methodName).Result;
        if (isUnique)
        {
            result = null;
            return true;
        }

        result = CreateErrorResult(Error.Code11_RequestIdIsNotUnique());
        return false;
    }

    public async Task<bool> IsCertExistsV3(byte[] cert) => await _repository.IsCertExist(cert);

    public async Task<bool> IsCertActiveV3(string thumbprint) => await _repository.IsCertActive(thumbprint);

    public async Task<int> GetActiveCertificatesCountV3(byte[] cert)
    {
        if (cert.Length == 0)
        {
            return 0;
        }

        var certificate = new X509Certificate2(cert);
        return await _repository.GetActiveCertificatesCountByThumbprint(certificate.Thumbprint ?? string.Empty);
    }

    public async Task<bool> SetCertificateInactiveV3(byte[] cert)
    {
        if (cert.Length == 0)
        {
            return false;
        }

        var certificate = new X509Certificate2(cert);
        return await _repository.SetCertificateInactive(certificate.Thumbprint ?? string.Empty);
    }
    private BaseResultV3 CreateErrorResult(Error error)
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
