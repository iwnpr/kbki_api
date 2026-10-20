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
    /// Добавляет идентификатор транзакции в событие, если он задан и сообщение не передало своё значение.
    /// </summary>
    /// <param name="logEvent">Обогащаемое событие логирования.</param>
    /// <param name="propertyFactory">Фабрика структурированных свойств Serilog.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (TransactionId is null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogPropertyNames.TransactionId, TransactionId));
    }
}
