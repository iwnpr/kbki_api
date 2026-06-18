using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.domain.entities;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;

/// <summary>
/// Создание и валидация транзакции для dlrequest API 3.0.
/// </summary>
public sealed class CreateAndValidateHandlerV3(
    IValidationServiceV3 validationService,
    ICryptoService cryptoService,
    IXmlServiceV3 xmlService,
    IRepositoryV3 repository,
    IKeyValueStorageService storageService,
    IBKIRequisitsHandler bKIRequisits,
    ILogger<CreateAndValidateHandlerV3> logger,
    IConfiguration configuration)
    : IRequestHandler<CreateToValidateCommandV3, QBCHProcessingTransaction>
{
    private readonly IValidationServiceV3 _validationService = validationService;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly IRepositoryV3 _repository = repository;
    private readonly IKeyValueStorageService _storageService = storageService;
    private readonly IBKIRequisitsHandler _bKIRequisits = bKIRequisits;
    private readonly ILogger<CreateAndValidateHandlerV3> _logger = logger;

    public async Task<QBCHProcessingTransaction> Handle(CreateToValidateCommandV3 request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Начало создания и валидации транзакции v3. Method={Method}, Path={Path}", request.Request.Method, request.Request.Path);
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
        var transaction = QBCHProcessingTransaction.Create(DateTime.Now, clientRequest, attachement, _bKIRequisits.GetBureaList());

        var result = await transaction.ValidateV3(
            validationService: _validationService,
            cryptoService: _cryptoService,
            xmlService: _xmlService,
            repository: _repository,
            cacheService: _storageService,
            allowMissingClientCertificate: configuration.GetValue("CertificateValidation:AllowMissingClientCertificate", false),
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Окончание создания и валидации транзакции v3 {TransactionId}. Ошибок обработки={ProcessingErrorsCount}, пакетных ошибок={PackageErrorsCount}",
            result.Id,
            result.ProcessingErrors.Count,
            result.PackageValidationErrors.Count);

        return result;
    }
}
