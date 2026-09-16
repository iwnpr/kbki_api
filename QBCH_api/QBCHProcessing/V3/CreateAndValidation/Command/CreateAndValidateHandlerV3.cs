using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using qbch_lib.domain.aggregate.V3;
using qbch_lib.domain.entities;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.entities;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;

/// <summary>
/// Создание и валидация транзакции для dlrequest
/// </summary>
public sealed class CreateAndValidateHandler(IQBCHValidationDispatcherV3 validationDispatcher, IBKIRequisitsHandler bKIRequisits, ILogger<CreateAndValidateHandler> logger) : IRequestHandler<CreateToValidateCommandV3, QBCHProcessingTransactionV3>
{
    private readonly IQBCHValidationDispatcherV3 _validationDispatcher = validationDispatcher;
    private readonly IBKIRequisitsHandler _bKIRequisits = bKIRequisits;
    private readonly ILogger<CreateAndValidateHandler> _logger = logger;

    public async Task<QBCHProcessingTransactionV3> Handle(CreateToValidateCommandV3 request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Начало создания и валидации транзакции. Method={Method}, Path={Path}", request.Request.Method, request.Request.Path);
        request.Request.EnableBuffering();
        if (request.Request.Body.CanSeek)
        {
            request.Request.Body.Position = 0;
        }

        using var memoryStream = new MemoryStream();
        await request.Request.Body.CopyToAsync(memoryStream, cancellationToken);
        if (request.Request.Body.CanSeek)
        {
            request.Request.Body.Position = 0;
        }

        var clientRequest = ClentRequest.Create(
            requestMethod: request.Request.Method,
            requestTime: DateTime.Now,
            ipAddress: request.Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            certificate: request.Request.HttpContext.Connection.ClientCertificate);

        var requestBody = memoryStream.ToArray();
        var attachement = Attachment.Create(signedRequest: requestBody);
        var transaction = QBCHProcessingTransactionV3.Create(DateTime.Now, clientRequest, attachement, _bKIRequisits.GetBureaList());

        var result = await _validationDispatcher.ValidateV3(transaction, cancellationToken);

        _logger.LogDebug(
            "Окончание создания и валидации транзакции v3. TransactionId={TransactionId}, Ошибок обработки={ProcessingErrorsCount}, пакетных ошибок={PackageErrorsCount}",
            result.Id,
            result.ProcessingErrors.Count,
            result.PackageValidationErrors.Count);

        return result;
    }
}
