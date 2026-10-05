using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaService_lib.BackgroundPublishing;

/// <summary>
/// Фоновый отправитель: разбирает <see cref="KafkaPublishQueue"/> и выполняет поставленные
/// в неё отправки в Kafka.
/// </summary>
public sealed class KafkaPublishWorker(KafkaPublishQueue queue, ILogger<KafkaPublishWorker> logger) : BackgroundService
{
    private readonly KafkaPublishQueue _queue = queue;
    private readonly ILogger<KafkaPublishWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fallbackCount = 0;

        // Токен намеренно не передаётся в ReadAllAsync: при остановке приложения очередь
        // нужно разобрать, а не бросить. Цикл завершает Complete() из StopAsync.
        await foreach (var item in _queue.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    fallbackCount++;
                    await item.Fallback();
                }
                else
                {
                    await item.Publish();
                }
            }

            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Необработанная ошибка при фоновой отправке в Kafka");
            }
        }

        if (fallbackCount > 0)
            _logger.LogCritical("Остановка приложения: отправок не выполнено {fallbackCount}, данные переданы резервному обработчику", fallbackCount);
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Остановка фоновой отправки в Kafka: в очереди осталось {Count} отправок", _queue.Count);

        // Закрываем очередь на запись; остаток ExecuteAsync разберёт резервным обработчиком.
        _queue.Complete();
        await base.StopAsync(cancellationToken);

        if (ExecuteTask?.IsCompleted == true)
        {
            _logger.LogInformation("Фоновая отправка в Kafka остановлена: очередь полностью обработана");
        }
        else
        {
            _logger.LogWarning("Фоновая отправка в Kafka остановлена по таймауту: не отправлено {Count} сообщений", _queue.Count);
        }
    }
}
