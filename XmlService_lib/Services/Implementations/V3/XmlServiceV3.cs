using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

public class XmlServiceV3(
    IMemoryCache memoryCache,
    IConfiguration config,
    ILogger<XmlService> logger,
    ITicketService ticketService,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : XmlService(memoryCache, config, logger, ticketService), IXmlServiceV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
