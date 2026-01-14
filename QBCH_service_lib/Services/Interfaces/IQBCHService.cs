using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.qcb_xml.v1_3.qcb_request;
using QBCHService_lib.Models;

namespace QBCHService_lib.Services.Interfaces
{
    /// <summary>
    /// Сервис ССП
    /// </summary>
    public interface IQBCHService
    {
        /// <summary>
        /// Запрос ССП
        /// </summary>
        /// <param name="requestId">Id запроса</param>
        /// <param name="request"></param>
        /// <param name="client">http клиент для каждого КБКИ</param>
        /// <param name="resendTimeout"></param>
        /// <param name="bureau"></param>
        /// <param name="IsOldVersion">Версия 1.2</param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpRequest(string requestId, ЗапросСведенийОПлатежах request, HttpClient client, long resendTimeout, QBCHRequisite bureau, bool IsOldVersion = false);

        /// <summary>
        /// Сведения о платежах из нашей БД
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpFromDB(ЗапросСведенийОПлатежах request, string guid);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processing"></param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpFromDBv2(QBCHProcessingTransaction processing);

        /// <summary>
        /// Сведения о платежах в КБКИ
        /// </summary>
        /// <param name="processing"></param>
        /// <param name="client"></param>
        /// <param name="bureau"></param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpRequestv2(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau);
    }
}
