using Cache_lib.Interfaces;
using Crypto_lib.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.Configuration;
using QBCHService_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCHService_lib.Services.Implementations.V3;

public class QBCHServiceV3(
    ICryptoService cryptoService,
    IXmlServiceV3 xmlService,
    ILogger<QBCHService> logger,
    IRepository qbchDb,
    ICacheService redisCache,
    IConfiguration config,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : QBCHService(cryptoService, xmlService, logger, qbchDb, redisCache, config), IQBCHServiceV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}