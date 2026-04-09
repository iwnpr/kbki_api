using Cache_lib.Interfaces;
using MediatR;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_api.QBCHProcessing.CreateAndValidation.V3;
using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.domain.entities;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.Command;

/// <summary>
/// Создание и валидация транзакции для dlrequest API 3.0.
/// </summary>
public sealed class CreateAndValidateHandlerV3(
    IValidationServiceV3 validationService,
    IXmlServiceV3 xmlService,
    IRepositoryV3 repository,
    ICacheService cacheService,
    IBKIRequisitsHandler bKIRequisits,
    ILogger<CreateAndValidateHandlerV3> logger)
    : IRequestHandler<CreateToValidateCommandV3, QBCHProcessingTransaction>
{
    private readonly IValidationServiceV3 _validationService = validationService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly IRepositoryV3 _repository = repository;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IBKIRequisitsHandler _bKIRequisits = bKIRequisits;
    private readonly ILogger<CreateAndValidateHandlerV3> _logger = logger;

    public async Task<QBCHProcessingTransaction> Handle(CreateToValidateCommandV3 request, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await request.Request.Body.CopyToAsync(memoryStream, cancellationToken);

        var clientRequest = ClentRequest.Create(
            requestMethod: request.Request.Method,
            requestTime: DateTime.Now,
            ipAddress: request.Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            certificate: request.Request.HttpContext.Connection.ClientCertificate);

        var attachement = Attachment.Create(signedRequest: memoryStream.ToArray());
        var transaction = QBCHProcessingTransaction.Create(DateTime.Now, clientRequest, attachement, _bKIRequisits.GetBureaList());

        return await transaction.ValidateV3(
            validationService: _validationService,
            xmlService: _xmlService,
            repository: _repository,
            cacheService: _cacheService,
            apiVersion: request.ApiVersion.ToString(),
            cancellationToken: cancellationToken);
    }
}
