using qbch_lib.domain.aggregate.V3;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;

namespace QBCH_api.Services.Interfaces.V3;

/// <summary>
/// Проверки ИНН/ПризнакПроверки для самозапрета и антифрод сценариев API 3.0.
/// </summary>
public interface ISelfLockedUpValidatorV3
{
    QBCHProcessingTransactionV3 ValidateInnAndSelfProhibitionV3(QBCHProcessingTransactionV3 transaction, ЗапросСведенийV3? requestV3);
}
