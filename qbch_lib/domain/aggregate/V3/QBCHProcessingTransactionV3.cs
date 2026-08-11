using QBCH.Lib.qcb_xml.v3_0;
using qbch_lib.domain.entities;

namespace QBCH_lib.domain.aggregate.V3;

/// <summary>
/// Обертка транзакции для API 3.0.
/// </summary>
public class QBCHProcessingTransactionV3
{
    private readonly QBCHProcessingTransaction _inner;

    private QBCHProcessingTransactionV3(QBCHProcessingTransaction inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Внутренняя транзакция.
    /// </summary>
    public QBCHProcessingTransaction Inner => _inner;

    /// <summary>
    /// Получить запрос версии 3.0.
    /// </summary>
    public ЗапросСведений? GetRequest()
    {
        return _inner.ClentRequest.RequestPayload as ЗапросСведений;
    }

    /// <summary>
    /// Получить обертку клиентского запроса версии 3.0.
    /// </summary>
    public ClentRequest GetClientRequest()
    {
        return ClentRequest.From(_inner.ClentRequest);
    }
}
