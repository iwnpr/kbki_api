using Domain.QBCHModels.CommonTypes;
using Domain.QBCHModels.qcb_xml.v2_0.Enums;
using Domain.QBCHModels.qcb_xml.v2_0.qcb_result;

namespace Application_lib
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
        Результат CreateResultv2(ResponseType type, string? code = null, string? text = null, string? requestId = null, string? guid = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        Результат CreateResultV2Common(string requestId, string guid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        Результат CreateResultV2Common(string requestId, string guid, DateTime dateTime);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        Результат CreateResultV2Error(Error error);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        Результат CreateResultV2Success(string requestId);
    }
}
