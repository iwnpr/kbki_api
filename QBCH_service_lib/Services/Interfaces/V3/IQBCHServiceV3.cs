using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCHService_lib.Models;

namespace QBCHService_lib.Services.Interfaces.V3;

public interface IQBCHServiceV3
{
    Task<QBCHTaskResult> AmpFromDBv3(QBCHProcessingTransaction processing);

    Task<QBCHTaskResult> AmpRequestv3(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau);
}