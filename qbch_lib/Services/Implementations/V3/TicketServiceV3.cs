using Microsoft.Extensions.Configuration;
using QBCH_lib.Configuration;
using QBCH_lib.Services.Interfaces.V3;

namespace QBCH_lib.Services.Implementations.V3;

public class TicketServiceV3(IConfiguration config, ApiV3ContractOptions contractOptions, ApiV3ContractRules contractRules) : ITicketServiceV3
{
    private readonly IConfiguration _config = config;
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
