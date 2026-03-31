using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.qcb_put;
using QBCH_lib.upload_xml;

namespace QBCHService_lib.Services.Interfaces
{
    /// <summary>
    /// Сервис транссформации
    /// </summary>
    public interface ITransformer
    {
        /// <summary>
        /// Конвертация запроса dlput в формат выгрузки бюро
        /// </summary>
        /// <param name="data">Запрос dlput</param>
        /// <param name="abonent"></param>
        /// <returns></returns>
        List<Document> ConvertDlPutToUpload(ПредставлениеСведенийОПлатежах data, AbonentValidatationResult abonent);
    }
}
