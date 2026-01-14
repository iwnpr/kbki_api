using Application_lib;
using Domain.QBCHModels.aggregate;
using Domain.QBCHModels.entities;
using MediatR;

namespace QBCH_api.QBCHProcessing.CreateAndValidation.DlRequestValidationMediatr;

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
/// <param name="qbchRequisits"></param>
/// <param name="logger"></param>
public sealed class DlrequestValidationHandler(ICryptoAdapter cryptoService,
                                IXmlService xmlService,
                                IDBAdapter repository,
                                IRedisAdapter redisCache,
                                IQBCHRequisitsService qbchRequisits,
                                ILogger<DlrequestValidationHandler> logger) : IRequestHandler<DlRequestAggregateMediatrInput, QBCHProcessingTransaction>
{
    private readonly ICryptoAdapter _cryptoService = cryptoService;
    private readonly IXmlService _xmlService = xmlService;
    private readonly IDBAdapter _repository = repository;
    private readonly IRedisAdapter _redisCache = redisCache;
    private readonly IQBCHRequisitsService _qbchRequisits = qbchRequisits;
    private readonly ILogger<DlrequestValidationHandler> _logger = logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QBCHProcessingTransaction> Handle(DlRequestAggregateMediatrInput request, CancellationToken cancellationToken)
    {

        using var memoryStream = new MemoryStream();
        await request.Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var clientRequest = ClentRequest.Create(requestMethod: request.Request.Method,
                                                requestTime: DateTime.Now,
                                                ipAddress: $"{request.Request.HttpContext.Connection.LocalIpAddress?.ToString()}|{request.Request.HttpContext.Connection.RemoteIpAddress?.ToString()}",
                                                certificate: request.Request.HttpContext.Connection.ClientCertificate);

        var attachement = Attachment.Create(signedRequest: memoryStream.ToArray());


        return QBCHProcessingTransaction.
                 Create(DateTime.Now, "dlrequest", clientRequest, attachement, _qbchRequisits.GetBureaList())
                .ValidateDlRequest(_cryptoService.ValidateMsg,
                          _xmlService.ValidateXml,
                          request.ApiVersion,
                          _repository.GetInnOgrnByThumbprint,
                          _repository.IsPermissionGrantedv2,
                          _redisCache.IsUniqueRequestId
                 );
    }
}
