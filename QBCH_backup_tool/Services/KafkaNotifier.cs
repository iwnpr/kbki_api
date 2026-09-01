using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QBCH_backup_tool.Services;

/// <summary>
/// Отправка уведомлений в Kafka напрямую через Confluent.Kafka.
/// Утилита запускается отдельно от API и не зависит от его сборок и конфигурации, поэтому
/// продюсер настраивается здесь. Параметры читаются из собственного appsettings утилиты
/// (секция <c>KafkaService</c>), а сообщение формируется так же, как в API: ключ <c>Null</c>,
/// значение — строка <c>QBCH:{service}:{RequestId}</c>.
/// </summary>
public sealed class KafkaNotifier : IDisposable
{
    private readonly ILogger<KafkaNotifier> _logger;
    private readonly string _topic;
    private readonly IProducer<Null, string> _producer;

    public KafkaNotifier(ILogger<KafkaNotifier> logger, IConfiguration configuration)
    {
        _logger = logger;

        var bootstrapServers = configuration.GetValue<string?>("KafkaService:BootstrapServers");
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException(
                "Не задан адрес брокера Kafka (KafkaService:BootstrapServers). " +
                "Заполните его в appsettings.json утилиты или передайте -D KafkaService:BootstrapServers=...");

        var topic = configuration.GetValue<string?>("KafkaService:Topic");
        if (string.IsNullOrWhiteSpace(topic))
            throw new InvalidOperationException(
                "Не задан топик Kafka (KafkaService:Topic). " +
                "Заполните его в appsettings.json утилиты или передайте -D KafkaService:Topic=...");

        _topic = topic;

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            // Ждём подтверждения от всех брокеров: терять уведомление во второй раз нельзя.
            Acks = Acks.All,
            LingerMs = 0,
            MessageTimeoutMs = configuration.GetValue<int?>("KafkaService:MessageTimeoutMs") ?? 30000,
            RequestTimeoutMs = configuration.GetValue<int?>("KafkaService:RequestTimeoutMs") ?? 30000,
            SocketTimeoutMs = configuration.GetValue<int?>("KafkaService:SocketTimeoutMs") ?? 60000
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
        _logger.LogDebug("Kafka: продюсер настроен, brokers={brokers}, topic={topic}.", bootstrapServers, _topic);
    }

    /// <summary>
    /// Отправляет уведомление. Возвращает <see langword="true"/>, если брокер подтвердил запись.
    /// </summary>
    public async Task<bool> ProduceAsync(string value, CancellationToken ct = default)
    {
        var result = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = value }, ct);
        if (result.Status == PersistenceStatus.Persisted)
            return true;

        _logger.LogError("Kafka: сообщение '{value}' не подтверждено брокером (статус {status}).", value, result.Status);
        return false;
    }

    public void Dispose()
    {
        // Даём продюсеру дослать всё, что осталось в буфере, прежде чем закрыться.
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}


