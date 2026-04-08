using QBCH_lib.Configuration;
using QBCHService_lib.Services.Interfaces.V3;

namespace QBCHService_lib.Services.Implementations.V3;

public class TransformerV3(
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : Transformer(), ITransformerV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;
}
