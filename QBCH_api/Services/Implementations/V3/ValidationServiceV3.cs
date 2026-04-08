using Cache_lib.Interfaces;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces.V3;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces;
using XmlService_lib.Services.Interfaces.V3;

namespace QBCH_api.Services.Implementations.V3;

public class ValidationServiceV3(
    IXmlServiceV3 xmlService,
    ICryptoService cryptoService,
    IRepository qbchDb,
    ILogger<ValidationService> logger,
    ICacheService cache,
    ITicketService ticketService,
    IRepository repository,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : ValidationService(xmlService, cryptoService, qbchDb, logger, cache, ticketService, repository), IValidationServiceV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
