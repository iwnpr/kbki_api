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

    private readonly Channel<(Func<Task> Publish, Func<Task> Fallback)> _channel;

    public KafkaPublishQueue(IConfiguration config)
    {
        var capacity = config.GetValue<int?>("KafkaService:PublishQueueCapacity") ?? DefaultCapacity;

        _channel = Channel.CreateBounded<(Func<Task> Publish, Func<Task> Fallback)>(
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
    /// <param name="publish">Отправка в Kafka.</param>
    /// <param name="fallback">Если отправку выполнить не успели: остаток очереди
    /// при остановке приложения уходит сюда, а не в брокер.</param>
    /// <returns><c>false</c>, если очередь переполнена или закрыта на запись.</returns>
    public bool TryEnqueue(Func<Task> publish, Func<Task> fallback)
        => _channel.Writer.TryWrite((publish, fallback));

    /// <summary>
    /// Текущее количество ожидающих отправок в очереди.
    /// </summary>
    public int Count => _channel.Reader.Count;

    /// <summary>
    /// Читает очередь до её закрытия.
    /// </summary>
    public IAsyncEnumerable<(Func<Task> Publish, Func<Task> Fallback)> ReadAllAsync(CancellationToken cancellationToken)
    => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Закрывает очередь на запись. Чтение завершится, как только очередь опустеет.
    /// </summary>
    public void Complete() => _channel.Writer.TryComplete();
}
