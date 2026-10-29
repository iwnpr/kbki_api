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
    public Result<bool> ValidateEncodingV3(byte[] message) => Execute(() =>
    {
        try
        {
            new UTF8Encoding(false, true).GetCharCount(message);
            return Result<bool>.Success(true);
        }
        catch (DecoderFallbackException ex)
        {
            logger.LogError(ex, "Не пройдена проверка кодировки: тело запроса не является корректным UTF-8");
            return Result<bool>.Failure(AnswerErrorCode.Code8_UnsupportedEncoding());
        }
    });

    public Result<bool> ValidateRequestDateV3(DateTime? requestDate) => Execute(() =>
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
        logger.LogDebug("Проверка даты запроса: requestDate={RequestDate}, currentDate={CurrentDate}", requestDate, currentDate);
        return requestDate?.Date == currentDate
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(AnswerErrorCode.Code23_InvalidRerquestDate());
    });

    public Result<bool> ValidateCertificateV3(X509Certificate2? requestCert) => Execute(() =>
    {
        // Старый контракт криптосервиса адаптируется только на этой границе.
        var valid = cryptoService.ValidateCertificate(requestCert, out var result);
        logger.LogDebug("Проверка сертификата: thumbprint={Thumbprint}, valid={Valid}", requestCert?.Thumbprint, valid);
        return valid
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(new AnswerErrorCode(result.ErrorCode, result.Error ?? "Ошибка проверки сертификата."));
    });

    public Result<CryptoServiceResult> ValidateMsgV3(byte[] msg, X509Certificate2? requestCert,
        byte[]? encodedSignature = null) => Execute(() =>
        {
            var result = cryptoService.ValidateMsg(msg, requestCert, encodedSignature);
            logger.LogDebug("Проверка подписи: thumbprint={Thumbprint}, valid={Valid}", requestCert?.Thumbprint, result.IsSuccess);
            return result;
        });

    public Task<Result<bool>> ValidateRulesV3(string? thumbprint, string? serviceName,
        CancellationToken? ct = null) => ExecuteAsync(async () =>
    {
        var granted = await repository.IsPermissionGrantedV3(thumbprint, serviceName, ct);
        logger.LogDebug("Проверка прав доступа: thumbprint={Thumbprint}, serviceName={ServiceName}, granted={Granted}",
            thumbprint, serviceName, granted);
        return granted
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(AnswerErrorCode.Code22_AccessDenied());
    });

    public Task<Result<bool>> IsUniqueRequestIdV3Async(string requestId, string methodName,
        string ogrn) => ExecuteAsync(async () =>
    {
        var unique = await cache.IsUniqueRequestId(requestId, ogrn, methodName);
        logger.LogDebug("Проверка уникальности запроса: requestId={RequestId}, ogrn={Ogrn}, unique={Unique}",
            requestId, ogrn, unique);
        return unique
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(AnswerErrorCode.Code11_RequestIdIsNotUnique());
    });

    private Result<T> Execute<T>(Func<Result<T>> validate)
    {
        try
        {
            return validate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<T>.Failure(AnswerErrorCode.Code99_OtherError("Не удалось выполнить проверку запроса."));
        }
    }

    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> validate)
    {
        try
        {
            return await validate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось выполнить проверку запроса API 3.0");
            return Result<T>.Failure(AnswerErrorCode.Code99_OtherError("Не удалось выполнить проверку запроса."));
        }
    }
}
