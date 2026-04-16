using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;
using РезультатПредставленияСведенийV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведений;
using РезультатV3 = QBCH.Lib.qcb_xml.v3_0.Результат;
namespace QBCH_api.Services.Interfaces.V3;

public interface IDlPutServiceV3
{
    DlPutServiceV3ProcessingResult Process(ПредставлениеСведенийV3 request, bool returnAcceptedTicket = false, string? responseId = null, long? readyTime = null);
}
public sealed record DlPutServiceV3ProcessingResult(
    bool IsAccepted,
    РезультатПредставленияСведенийV3? ReadyResult,
    РезультатV3? AcceptedTicket);