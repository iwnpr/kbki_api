using QBCH_lib.qcb_xml.v3_0.Enums;
using System;

namespace QBCH_lib.Services.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITicketService
    {
        qcb_xml.v3_0.qcb_result.Результат CreateReceiptWithAnswerId(string requestId, string answerId, DateTime requestDate, long? readyInMs = null);

        qcb_xml.v3_0.qcb_result.Результат CreateSuccessReceipt(string requestId, DateTime requestDate);

        qcb_xml.v3_0.qcb_result.Результат CreateErrorReceipt(string code, string message);

        qcb_xml.v3_0.qcb_result.Результат CreateErrorReceipt(core.Error error);

        qcb_xml.v3_0.qcb_result.Результат CreateResult(ResponseType type, string? code = null, string? text = null, string? requestId = null, string? guid = null);



        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Common(string requestId, string guid);

        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Common(string requestId, string guid, DateTime dateTime);

        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Error(core.Error error);

        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Success(string requestId);
    }
}
