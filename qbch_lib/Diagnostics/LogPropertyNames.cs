namespace qbch_lib.Diagnostics;

/// <summary>
/// Имена структурированных свойств логирования.
/// </summary>
public static class LogPropertyNames
{
    /// <summary>
    /// Идентификатор бизнес-транзакции.
    /// </summary>
    public const string TransactionId = "TransactionId";

    /// <summary>
    /// Контрагент, от имени которого выполняется входящий запрос.
    /// </summary>
    public const string Counterparty = "Контрагент";

    /// <summary>
    /// Отпечаток клиентского сертификата входящего запроса.
    /// </summary>
    public const string Thumbprint = "Отпечаток";
}