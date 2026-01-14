using Application_lib;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adapters_lib.Kafka
{
    namespace Kafka.Services.Implementation
    {
        public class KafkaAdapter(ILogger<KafkaAdapter> logger, IProducer<Null, string> producer) : IKafkaAdapter
        {
            private IProducer<Null, string> _producer = producer;
            private readonly ILogger<KafkaAdapter> _logger = logger;

            public async Task<bool> Produce(Message<Null, string> message, string? topic = null)
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(topic);
                    ArgumentNullException.ThrowIfNull(message);

                    await _producer.ProduceAsync(topic, message);
                    _logger.LogDebug("Сообщение {message} отправлено в топик {topic}", message, topic);
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Ошибка добавления в кафку {value}", message.Value);
                    return false;
                }
            }
        }
    }

}
