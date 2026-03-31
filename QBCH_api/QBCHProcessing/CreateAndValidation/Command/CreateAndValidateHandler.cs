using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.domain.entities;
using XmlService_lib.Services.Interfaces;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.Command;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
/// <param name="cryptoService"></param>
/// <param name="xmlService"></param>
/// <param name="repository"></param>
/// <param name="redisCache"></param>
/// <param name="bKIRequisits"></param>
/// <param name="logger"></param>
public sealed class CreateAndValidateHandler(ICryptoService cryptoService,
                                IXmlService xmlService,
                                IRepository repository,
                                ICacheService redisCache,
                                IBKIRequisitsHandler bKIRequisits,
                                ILogger<CreateAndValidateHandler> logger) : IRequestHandler<CreateToValidateCommand, QBCHProcessingTransaction>
{
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IXmlService _xmlService = xmlService;
    private readonly IRepository _repository = repository;
    private readonly ICacheService _redisCache = redisCache;
    private readonly IBKIRequisitsHandler _bKIRequisits = bKIRequisits;
    private readonly ILogger<CreateAndValidateHandler> _logger = logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QBCHProcessingTransaction> Handle(CreateToValidateCommand request, CancellationToken cancellationToken)
    {

        using var memoryStream = new MemoryStream();
        await request.Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var clientRequest = ClentRequest.Create(requestMethod: request.Request.Method,
                                                requestTime: DateTime.Now,
                                                ipAddress: request.Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                                                certificate: request.Request.HttpContext.Connection.ClientCertificate);

        var attachement = Attachment.Create(signedRequest: memoryStream.ToArray());


        return QBCHProcessingTransaction.
                 Create(DateTime.Now, clientRequest, attachement, _bKIRequisits.GetBureaList())
                .Validate(_cryptoService.ValidateMsg,
                          _xmlService.ValidateXml,
                          request.ApiVersion,
                          _repository.GetInnOgrnByThumbprint,
                          _repository.IsPermissionGrantedv2,
                          _redisCache.IsUniqueRequestId
                 );
    }
}
