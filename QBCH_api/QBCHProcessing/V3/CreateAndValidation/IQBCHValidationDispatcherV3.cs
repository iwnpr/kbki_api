using qbch_lib.domain.aggregate.V3;

namespace QBCH_api.QBCHProcessing.V3.CreateAndValidation;

/// <summary>
/// Отдельный диспетчер start-to-finish валидации для API 3.0.
/// </summary>
public interface IQBCHValidationDispatcherV3
{
    Task<QBCHProcessingTransactionV3> ValidateV3(QBCHProcessingTransactionV3 transaction, CancellationToken cancellationToken);
}
