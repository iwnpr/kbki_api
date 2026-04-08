using Cache_lib.Interfaces;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.Services.Implementations.V3;

public class ValidationServiceV3(
    IXmlServiceV3 xmlService,
    ICryptoService cryptoService,
    ILogger<ValidationServiceV3> logger,
    ICacheService cache,
    IRepositoryV3 repository,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules) : IValidationServiceV3
{
    private readonly IXmlServiceV3 _xmlService = xmlService;
    private readonly IRepositoryV3 _repository = repository;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ILogger<ValidationServiceV3> _logger = logger;
    private readonly ICacheService _cache = cache;
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
