using QBCH_lib.core;
using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;
using РезультатV3 = QBCH.Lib.qcb_xml.v3_0.Результат;
using РезультатПредставленияСведенийV3 = QBCH.Lib.qcb_xml.v3_0.РезультатПредставленияСведений;
namespace QBCH_api.Services.Interfaces.V3;

/// <summary>Обрабатывает сведения, переданные методом dlput.</summary>
public interface IDlPutServiceV3
{
    /// <summary>Формирует результат обработки или квитанцию о приеме.</summary>
    /// <param name="request">Представление сведений.</param>
    /// <param name="returnAcceptedTicket">Признак формирования квитанции о приеме.</param>
    /// <param name="responseId">Идентификатор отложенного ответа.</param>
    /// <returns>Результат обработки; сбой сериализации прерывает обработку всего запроса.</returns>
    Task<Result<DlPutServiceV3ProcessingResult>> ProcessAsync(ПредставлениеСведенийV3 request, bool returnAcceptedTicket = false, string? responseId = null);
}
public sealed record DlPutServiceV3ProcessingResult(
    bool IsAccepted,
    РезультатПредставленияСведенийV3? ReadyResult,
    РезультатV3? AcceptedTicket);