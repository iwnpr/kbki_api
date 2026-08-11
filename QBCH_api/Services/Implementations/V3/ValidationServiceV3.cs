using Cache_lib.Interfaces;
using Crypto_lib.Model;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces;
using qbch_lib.CommonTypes.Api;
using qbch_lib.domain.errors;
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

    public bool ValidateXmlV3(MemoryStream memoryStream, string nameOfController, [NotNullWhen(false)] out BaseResult? result)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateXmlV3: controller={nameOfController}, streamLength={streamLength}", nameOfController, memoryStream.Length);
        var isValid = _xmlService.ValidateXmlV3(memoryStream, nameOfController, out var xmlResult);

        if (!isValid)
        {
            _logger.LogDebug("ValidationServiceV3.ValidateXmlV3: невалидный XML, controller={nameOfController}, errorCode={errorCode}", nameOfController, xmlResult.ErrorCode);
            var error = new AnswerErrorCode(xmlResult.ErrorCode, xmlResult.Error);
            result = CreateErrorResult(error);
            return false;
        }

        _logger.LogDebug("ValidationServiceV3.ValidateXmlV3: XML валиден, controller={nameOfController}", nameOfController);

        result = null;
        return true;
    }

    public bool ValidateEncodingV3(byte[] message, [NotNullWhen(false)] out BaseResult? result)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateEncodingV3: messageLength={messageLength}", message.Length);
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
        _logger.LogDebug("ValidationServiceV3.ValidateEncodingV3: кодировка UTF-8 корректна");

        result = null;
        return true;
    }

    public bool ValidateRequestDateV3(DateTime? requestDate, [NotNullWhen(false)] out BaseResult? result)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateRequestDateV3: requestDate={requestDate}", requestDate);
        var currentMoscowDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTimeZone).Date;

        if (requestDate?.Date != currentMoscowDate)
        {
            var error = AnswerErrorCode.Code23_InvalidRerquestDate();
            _logger.LogError("ValidateRequestDateV3: дата запроса не совпадает: {requestDate} != {currentMoscowDate}. {errorMessage}", requestDate, currentMoscowDate, error.Message);

            result = CreateErrorResult(error);
            return false;
        }

        _logger.LogDebug("ValidationServiceV3.ValidateRequestDateV3: дата запроса корректна");
        result = null;
        return true;
    }

    public bool ValidateMsgV3(byte[] msg, X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult result, byte[]? encodedSignature = null)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateMsgV3: thumbprint={thumbprint}, msgLength={msgLength}, hasDetachedSignature={hasDetachedSignature}",
            requestCert?.Thumbprint, msg.Length, encodedSignature is not null);

        var isValid = _cryptoService.ValidateMsg(msg, requestCert, out result, encodedSignature);

        _logger.LogDebug("ValidationServiceV3.ValidateMsgV3 результат: isValid={isValid}, thumbprint={thumbprint}", isValid, requestCert?.Thumbprint);
        return isValid;
    }

    /// <summary>
    /// Проверка сертификата
    /// </summary>
    /// <param name="requestCert"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool ValidateCertificateV3(X509Certificate2? requestCert, [NotNullWhen(false)] out BaseResult? result)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: thumbprint={thumbprint}, notAfter={notAfter}",
            requestCert?.Thumbprint, requestCert?.NotAfter);

        var isValid = _cryptoService.ValidateCertificate(requestCert, out CryptoServiceResult? certResult);

        if (!isValid)
        {
            var code = certResult.ErrorCode;
            var message = certResult.Error;

            _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: сертификат не прошел проверку, thumbprint={thumbprint}, errorCode={code}",
                requestCert?.Thumbprint, code);
            result = CreateErrorResult(new AnswerErrorCode(code, message));
            return false;
        }

        _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: сертификат прошел проверку, thumbprint={thumbprint}",
            requestCert?.Thumbprint);
        result = null;
        return true;
    }

    public async Task<bool> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null)
    {
        _logger.LogDebug("ValidationServiceV3.ValidateRulesV3: thumbprint={thumbprint}, serviceName={serviceName}", thumbprint, serviceName);
        var isGranted = await _repository.IsPermissionGrantedV3(thumbprint, serviceName, ct);
        _logger.LogDebug("ValidationServiceV3.ValidateRulesV3 результат: isGranted={isGranted}, thumbprint={thumbprint}, serviceName={serviceName}", isGranted, thumbprint, serviceName);
        return isGranted;
    }

    public async Task<(bool IsUnique, BaseResult? Error)> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn)
    {
        _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: requestId={requestId}, ogrn={ogrn}, method={methodName}", requestId, ogrn, methodName);
        var isUnique = await _cache.IsUniqueRequestId(requestId, ogrn, methodName);

        if (isUnique)
        {
            _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: requestId уникален, requestId={requestId}, ogrn={ogrn}", requestId, ogrn);
            return (true, null);
        }

        _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: requestId не уникален, requestId={requestId}, ogrn={ogrn}", requestId, ogrn);
        return (false, CreateErrorResult(AnswerErrorCode.Code11_RequestIdIsNotUnique()));
    }

    public async Task<bool> IsCertExistsV3(byte[] cert)
    {
        _logger.LogDebug("ValidationServiceV3.IsCertExistsV3: certLength={certLength}", cert.Length);
        var exists = await _repository.IsCertExist(cert);
        _logger.LogDebug("ValidationServiceV3.IsCertExistsV3 результат: exists={exists}", exists);
        return exists;
    }

    public async Task<bool> IsCertActiveV3(string thumbprint)
    {
        _logger.LogDebug("ValidationServiceV3.IsCertActiveV3: thumbprint={thumbprint}", thumbprint);
        var isActive = await _repository.IsCertActive(thumbprint);
        _logger.LogDebug("ValidationServiceV3.IsCertActiveV3 результат: isActive={isActive}, thumbprint={thumbprint}", isActive, thumbprint);
        return isActive;
    }

    public async Task<int> GetActiveCertificatesCountV3(byte[] cert)
    {
        _logger.LogDebug("ValidationServiceV3.GetActiveCertificatesCountV3: certLength={certLength}", cert.Length);

        if (cert.Length == 0)
        {
            _logger.LogError("Невозможно получить количество активных сертификатов v3: входящий сертификат пустой");
            return 0;
        }

        using var certificate = new X509Certificate2(cert);

        _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3: thumbprint={thumbprint}", certificate.Thumbprint);
        var result = await _repository.GetActiveCertificatesCountByThumbprint(certificate.Thumbprint ?? string.Empty);
        _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3 Количество активных сертификатов: {success}, thumbprint={thumbprint}", result, certificate.Thumbprint);
        return result;
    }

    public async Task<bool> SetCertificateInactiveV3(byte[] cert)
    {
        if (cert.Length == 0)
        {
            _logger.LogError("Невозможно установить статус неактивного сертификатов v3: входящий сертификат пустой");
            return false;
        }

        using var certificate = new X509Certificate2(cert);
        _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3: thumbprint={thumbprint}", certificate.Thumbprint);
        var success = await _repository.SetCertificateInactive(certificate.Thumbprint ?? string.Empty);

        _logger.LogDebug("ValidationServiceV3.SetCertificateInactiveV3 результат: {success}, thumbprint={thumbprint}", success, certificate.Thumbprint);
        return success;
    }
    private BaseResult CreateErrorResult(AnswerErrorCode error)
    {
        return new BaseResult
        {
            IsError = true,
            ErrorCode = error.Code,
            Error = error.Message,
            ErrorMessage = error.Message,
            Ticket = _ticketService.CreateResultV3Error(error)
        };
    }
}
