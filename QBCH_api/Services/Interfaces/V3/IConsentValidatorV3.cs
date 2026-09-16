using qbch_lib.domain.aggregate.V3;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.Services.Interfaces.V3;

/// <summary>
/// Валидация блока "Согласие" для API 3.0.
/// </summary>
public interface IConsentValidatorV3
{
    QBCHProcessingTransactionV3 ValidateConsentV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3);
}
