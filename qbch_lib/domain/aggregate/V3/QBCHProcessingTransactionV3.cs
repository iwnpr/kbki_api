using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.domain.entities.V3;

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
    /// Обернуть существующую транзакцию.
    /// </summary>
    public static QBCHProcessingTransactionV3 From(QBCHProcessingTransaction transaction)
    {
        return new QBCHProcessingTransactionV3(transaction);
    }

    /// <summary>
    /// Получить запрос версии 3.0.
    /// </summary>
    public ЗапросСведений? GetRequestV3()
    {
        return _inner.ClentRequest.RequestPayload as ЗапросСведений;
    }

    /// <summary>
    /// Получить обертку клиентского запроса версии 3.0.
    /// </summary>
    public ClentRequestV3 GetClientRequestV3()
    {
        return ClentRequestV3.From(_inner.ClentRequest);
    }
}
