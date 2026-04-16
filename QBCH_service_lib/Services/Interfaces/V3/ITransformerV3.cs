using QBCH_lib.CommonTypes.Api;
using QBCH_lib.upload_xml;
using ПредставлениеСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ПредставлениеСведений;

namespace QBCHService_lib.Services.Interfaces.V3;

public interface ITransformerV3
{
    List<Document> ConvertDlPutToUpload(ПредставлениеСведенийV3 data, AbonentValidatationResult abonent);
}
