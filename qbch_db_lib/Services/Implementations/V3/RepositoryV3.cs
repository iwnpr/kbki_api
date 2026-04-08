using Cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.Configuration;

namespace Qbch_db_lib.Services.Implementations.V3;

public class RepositoryV3(
    IConfiguration config,
    ILogger<Repository> logger,
    ICacheService cacheService,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : Repository(config, logger, cacheService), IRepositoryV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
