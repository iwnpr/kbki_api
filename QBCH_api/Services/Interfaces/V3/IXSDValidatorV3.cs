using qbch_lib.domain.aggregate.V3;

namespace QBCH_api.Services.Interfaces.V3;

/// <summary>
/// XSD-валидация и десериализация dlrequest
/// </summary>
public interface IXSDValidatorV3
{
    QBCHProcessingTransactionV3 ValidateXml(QBCHProcessingTransactionV3 transaction);
}
