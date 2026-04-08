using Cache_lib.Interfaces;
using Crypto_lib.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using QBCHService_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCHService_lib.Services.Implementations.V3;

public class QBCHServiceV3(
    ICryptoService cryptoService,
    IXmlServiceV3 xmlService,
    ILogger<QBCHServiceV3> logger,
    IRepositoryV3 qbchDb,
    ICacheService redisCache,
    IConfiguration config,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : IQBCHServiceV3
{
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly ILogger<QBCHServiceV3> _logger = logger;
    private readonly IRepositoryV3 _qbchDb = qbchDb;
    private readonly ICacheService _redisCache = redisCache;
    private readonly IConfiguration _config = config;
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}