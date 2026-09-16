using qbch_lib.domain.aggregate.V3;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.Services.Interfaces.V3;

/// <summary>
/// Дополнительные проверки API 3.0, не покрываемые XSD.
/// </summary>
public interface IAdditionalValidatorV3
{
    QBCHProcessingTransactionV3 AdditionalValidationV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3);
}
