using Domain.QBCHModels.aggregate;
using Domain.QBCHRequisitsService;
using Domain.QBCHServiceModels;

namespace Application_lib
{
    /// <summary>
    /// Сервис ССП
    /// </summary>
    public interface IQBCHService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="processing"></param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpFromDB(QBCHProcessingTransaction processing);

        /// <summary>
        /// Сведения о платежах в КБКИ
        /// </summary>
        /// <param name="processing"></param>
        /// <param name="client"></param>
        /// <param name="bureau"></param>
        /// <returns></returns>
        public Task<QBCHTaskResult> AmpRequest(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau);
    }
}
