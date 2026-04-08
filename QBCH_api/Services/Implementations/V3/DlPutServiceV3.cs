using QBCH_api.Services.Interfaces.V3;
using QBCH_lib.Configuration;

namespace QBCH_api.Services.Implementations.V3;

public class DlPutServiceV3(ApiV3ContractOptions contractOptions, ApiV3ContractRules contractRules) : IDlPutServiceV3
{
    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;

    public bool IsEntitiesCountValid(int entitiesCount)
    {
        return _contractRules.IsDlPutEntitiesCountValid(entitiesCount);
    }
}
