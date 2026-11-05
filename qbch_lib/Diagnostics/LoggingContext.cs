using System;
using Serilog.Core;
using Serilog.Events;

namespace qbch_lib.Diagnostics;

/// <summary>
/// Свойства логирования, общие для всех записей одного HTTP-запроса.
/// </summary>
/// <remarks>
/// Значение можно выставить позже, когда оно станет известно, все последующие записи запроса,
/// включая итоговую запись о завершении, получат его.
/// </remarks>
public sealed class LoggingContext : ILogEventEnricher
{
    /// <summary>
    /// Идентификатор бизнес-транзакции. Пока не задан, записи выходят без него.
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Имя контрагента из клиентского сертификата. Без сертификата не задано.
    /// </summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// Отпечаток клиентского сертификата. Без сертификата не задан.
    /// </summary>
    public string? Thumbprint { get; set; }

    /// <summary>
    /// Способ поступления запроса, определённый по ноде приложения.
    /// </summary>
    public string? RequestChannel { get; set; }

    /// <summary>
    /// Добавляет свойства контекста, сохраняя явно переданные значения.
    /// Явно переданный TransactionId типа Guid приводит к строке для стандартного ECS-поля transaction.id.
    /// </summary>
    /// <param name="logEvent">Обогащаемое событие логирования.</param>
    /// <param name="propertyFactory">Фабрика структурированных свойств Serilog.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // ECS сопоставляет TransactionId со стандартным полем transaction.id только для строк.
        if (logEvent.Properties.TryGetValue(LogPropertyNames.TransactionId, out var transactionIdProperty)
            && transactionIdProperty is ScalarValue { Value: Guid explicitTransactionId })
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(LogPropertyNames.TransactionId, explicitTransactionId.ToString("D")));
        }
        else if (TransactionId is not null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogPropertyNames.TransactionId, TransactionId));
        }

        if (!string.IsNullOrWhiteSpace(Counterparty))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogPropertyNames.Counterparty, Counterparty));

        if (!string.IsNullOrWhiteSpace(Thumbprint))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogPropertyNames.Thumbprint, Thumbprint));

        if (!string.IsNullOrWhiteSpace(RequestChannel))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogPropertyNames.RequestChannel, RequestChannel));
    }
}
