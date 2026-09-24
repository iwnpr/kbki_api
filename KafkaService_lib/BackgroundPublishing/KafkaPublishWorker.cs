using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaService_lib.BackgroundPublishing;

/// <summary>
/// Фоновый отправитель: разбирает <see cref="KafkaPublishQueue"/> и выполняет поставленные
/// в неё отправки в Kafka.
/// </summary>
public sealed class KafkaPublishWorker(
    KafkaPublishQueue queue,
    ILogger<KafkaPublishWorker> logger) : BackgroundService
{
    private readonly KafkaPublishQueue _queue = queue;
    private readonly ILogger<KafkaPublishWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Токен намеренно не передаётся в ReadAllAsync: при остановке приложения очередь
        // нужно дочитать, а не бросить. Цикл завершает Complete() из StopAsync.
        await foreach (var publish in _queue.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await publish();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Необработанная ошибка при фоновой отправке в Kafka");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Закрываем очередь на запись и даём ExecuteAsync отправить остаток.
        // Общее время дренирования ограничено таймаутом остановки хоста.
        _queue.Complete();
        await base.StopAsync(cancellationToken);
    }
}
