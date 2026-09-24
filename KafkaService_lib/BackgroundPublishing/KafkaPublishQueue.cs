using Microsoft.Extensions.Configuration;
using System.Threading.Channels;

namespace KafkaService_lib.BackgroundPublishing;

/// <summary>
/// Очередь отправок в Kafka. Отправка вынесена с пути HTTP-ответа: клиент не ждёт
/// подтверждения от брокера, а получает ответ сразу после записи результата в Redis.
/// </summary>
public sealed class KafkaPublishQueue
{
    private const int DefaultCapacity = 500;

    private readonly Channel<Func<Task>> _channel;

    public KafkaPublishQueue(IConfiguration config)
    {
        var capacity = config.GetValue<int?>("KafkaService:PublishQueueCapacity") ?? DefaultCapacity;

        _channel = Channel.CreateBounded<Func<Task>>(
            new BoundedChannelOptions(capacity)
            {
                // Wait в паре с TryWrite: при переполнении TryWrite возвращает false.
                // WriteAsync использовать нельзя — он заблокировал бы поток запроса,
                // то есть вернул бы ровно ту проблему, ради которой отправка выносится в фон.
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });
    }

    /// <summary>
    /// Ставит отправку в очередь.
    /// </summary>
    /// <returns><c>false</c>, если очередь переполнена.</returns>
    public bool TryEnqueue(Func<Task> publish) => _channel.Writer.TryWrite(publish);

    /// <summary>
    /// Читает очередь до её закрытия.
    /// </summary>
    public IAsyncEnumerable<Func<Task>> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Закрывает очередь на запись. Чтение завершится, как только очередь опустеет.
    /// </summary>
    public void Complete() => _channel.Writer.TryComplete();
}
