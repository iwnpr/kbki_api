using QBCH_lib.qcb_xml.v3_0.Enums;
using System;

namespace QBCH_lib.Services.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="text"></param>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        qcb_xml.v3_0.qcb_result.Результат CreateResult(ResponseType type, string? code = null, string? text = null, string? requestId = null, string? guid = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Common(string requestId, string guid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Common(string requestId, string guid, DateTime dateTime);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Error(core.Error error);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        qcb_xml.v3_0.qcb_result.Результат CreateResultV2Success(string requestId);
    }
}
