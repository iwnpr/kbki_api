using Cache_lib.Interfaces;
using Crypto_lib.Model;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.core;
using qbch_lib.domain.errors;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace QBCH_api.Services.Implementations.V3;

/// <summary>Проверки входящего запроса. Не изменяет транзакции и не формирует ответы.</summary>
public class ValidationServiceV3(
    ICryptoService cryptoService,
    IKeyValueStorageService cache,
    IRepositoryV3 repository,
    ILogger<ValidationServiceV3> logger) : IValidationServiceV3
{
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IKeyValueStorageService _cache = cache;
    private readonly IRepositoryV3 _repository = repository;
    private readonly ILogger<ValidationServiceV3> _logger = logger;

    /// <inheritdoc />
    public Result<bool> ValidateEncodingV3(byte[] message)
    {
        try
        {
            _logger.LogDebug("Начало проверки кодировки: messageLength={messageLength}, method={methodName}", message.Length, nameof(ValidateEncodingV3));
            var encoding = new UTF8Encoding(false, true);
            encoding.GetCharCount(message);
            _logger.LogDebug("Проверка кодировки успешно завершена. Кодировка UTF-8 корректна. method={methodName}", nameof(ValidateEncodingV3));

            return Result<bool>.Success(true);
        }
        catch (DecoderFallbackException ex)
        {
            var error = AnswerErrorCode.Code8_UnsupportedEncoding();
            _logger.LogError(ex, "Не пройдена проверка кодировки: тело запроса не является корректным UTF-8, code={QbchErrorCode}, message: {QbchErrorMessage}, method={methodName}", error.Code, error.Message, nameof(ValidateEncodingV3));
            return Result<bool>.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<bool> ValidateRequestDateV3(DateTime? requestDate)
    {
        try
        {
            var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            var currentMoscowDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone).Date;

            if (requestDate?.Date != currentMoscowDate)
            {
                var error = AnswerErrorCode.Code23_InvalidRerquestDate();

                return Result<bool>.Failure(error);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<CryptoServiceResult> ValidateMsgV3(byte[] msg, X509Certificate2? requestCert, byte[]? encodedSignature = null)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.ValidateMsgV3:  начало проверки подписи сообщения, thumbprint={thumbprint}, msgLength={msgLength}, hasDetachedSignature={hasDetachedSignature}",
                requestCert?.Thumbprint, msg.Length, encodedSignature is not null);

            var isValid = _cryptoService.ValidateMsg(msg, requestCert, out var result, encodedSignature);

            _logger.LogDebug("ValidationServiceV3.ValidateMsgV3 результат: isValid={isValid}, thumbprint={thumbprint}", isValid, requestCert?.Thumbprint);
            return isValid
                ? Result<CryptoServiceResult>.Success(result)
                : Result<CryptoServiceResult>.Failure(new AnswerErrorCode(result.ErrorCode, result.Error!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<CryptoServiceResult>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<bool> ValidateCertificateV3(X509Certificate2? requestCert)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: начало проверки сертификата, thumbprint={thumbprint}, notAfter={notAfter}",
                requestCert?.Thumbprint, requestCert?.NotAfter);

            var isValid = _cryptoService.ValidateCertificate(requestCert, out CryptoServiceResult? certResult);

            if (!isValid)
            {
                var code = certResult.ErrorCode;
                var message = certResult.Error;

                _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: сертификат не прошел проверку, thumbprint={thumbprint}, errorCode={code}",
                    requestCert?.Thumbprint, code);

                return Result<bool>.Failure(new AnswerErrorCode(code, message));
            }

            _logger.LogDebug("ValidationServiceV3.ValidateCertificateV3: сертификат прошел проверку, thumbprint={thumbprint}",
                requestCert?.Thumbprint);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateRulesV3(string? thumbprint, string? serviceName, CancellationToken? ct = null)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.ValidateRulesV3: thumbprint={thumbprint}, serviceName={serviceName}", thumbprint, serviceName);
            var isGranted = await _repository.IsPermissionGrantedV3(thumbprint, serviceName, ct);
            _logger.LogDebug("ValidationServiceV3.ValidateRulesV3 результат: isGranted={isGranted}, thumbprint={thumbprint}, serviceName={serviceName}", isGranted, thumbprint, serviceName);
            return isGranted
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(AnswerErrorCode.Code22_AccessDenied());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsUniqueRequestIdV3Async(string requestId, string methodName, string ogrn)
    {
        try
        {
            _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: начало проверки уникальности идентификатора запроса, requestId={requestId}, ogrn={ogrn}, method={methodName}", requestId, ogrn, methodName);
            var isUnique = await _cache.IsUniqueRequestId(requestId, ogrn, methodName);

            if (isUnique)
            {
                _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: requestId уникален, requestId={requestId}, ogrn={ogrn}", requestId, ogrn);
                return Result<bool>.Success(true);
            }

            _logger.LogDebug("ValidationServiceV3.IsUniqueRequestIdV3Async: requestId не уникален, requestId={requestId}, ogrn={ogrn}", requestId, ogrn);
            return Result<bool>.Failure(AnswerErrorCode.Code11_RequestIdIsNotUnique());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<bool>.Failure(new OperationError(ex));
        }
    }
}
