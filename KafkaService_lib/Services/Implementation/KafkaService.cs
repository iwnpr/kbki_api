using Confluent.Kafka;
using KafkaService_lib.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Polly;
using Polly.Timeout;

namespace KafkaService_lib.Services.Implementation
{
    public class KafkaService : IKafkaService
    {
        private IProducer<string, string>? _producer;
        private IProducer<Null, string>? _producerMsg;
        private IConsumer<string, string>? _consumer;
        private readonly ILogger<KafkaService> _logger;
        private readonly IConfiguration _config;
        private readonly string _bootstrapServers;
        private readonly string _groupId;
        private readonly string _topic;
        private readonly int _transactionTimeoutMs;
        private readonly int _messageTimeoutMs;
        private readonly int _requestTimeoutMs;
        private readonly int _socketTimeoutMs;
        private readonly int _produceRetryCount;
        private readonly int _produceRetryDelayMs;
        private readonly int _produceRetryTotalTimeoutMs;
        private readonly ICompressService _compressService;


        public KafkaService(ILogger<KafkaService> logger, IConfiguration config, ICompressService compressService)
        {
            _logger = logger;
            _config = config;
            _compressService = compressService;
            _bootstrapServers = _config.GetValue<string>("KafkaService:BootstrapServers");
            _groupId = _config.GetValue<string>("KafkaService:GroupId");
            _topic = _config.GetValue<string>("KafkaService:Topic");
            _transactionTimeoutMs = _config.GetValue<int>("KafkaService:TransactionTimeoutMs");
            _messageTimeoutMs = _config.GetValue<int>("KafkaService:MessageTimeoutMs");
            _requestTimeoutMs = _config.GetValue<int>("KafkaService:RequestTimeoutMs");
            _socketTimeoutMs = _config.GetValue<int>("KafkaService:SocketTimeoutMs");
            _produceRetryCount = _config.GetValue<int?>("KafkaService:ProduceRetryCount") ?? 2;
            _produceRetryDelayMs = _config.GetValue<int?>("KafkaService:ProduceRetryDelayMs") ?? 100;
            _produceRetryTotalTimeoutMs = _config.GetValue<int?>("KafkaService:ProduceRetryTotalTimeoutMs") ?? 2000;
        }

        public bool IsAvailable()
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            try
            {
                // ReSharper disable once UnusedVariable
                var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error");
                return false;
            }

            return true;
        }

        public async Task<bool> Produce(Message<Null, string> message, string? topic = null)
        {
            _producerMsg ??= new ProducerBuilder<Null, string>(new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                // Задержка в мс между отправкой пакетов сообщений, нужно для равномерного распределения сообщений в партициях
                LingerMs = 0,
                // Ожидание сохранения сообщения во всех брокерах кластера, а не только в лидере
                Acks = Acks.All,
                TransactionTimeoutMs = _transactionTimeoutMs,
                MessageTimeoutMs = _messageTimeoutMs,
                RequestTimeoutMs = _requestTimeoutMs,
                SocketTimeoutMs = _socketTimeoutMs
            }).Build();

            topic ??= _topic;
            var maxAttempts = _produceRetryCount + 1;
            var timeoutPolicy = Policy.TimeoutAsync(_produceRetryTotalTimeoutMs / 1000, TimeoutStrategy.Optimistic);
            var retryPolicy = Policy
                .Handle<ProduceException<Null, string>>()
                .WaitAndRetryAsync(
                    _produceRetryCount,
                    _ => TimeSpan.FromMilliseconds(_produceRetryDelayMs),
                    (exception, delay, retryNumber, _) =>
                    {
                        _logger.LogWarning(exception, "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Повтор через {delayMs} ms", message.Value, retryNumber, maxAttempts, (int)delay.TotalMilliseconds);
                    });

            var policy = Policy.WrapAsync(timeoutPolicy, retryPolicy);

            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await _producerMsg.ProduceAsync(topic, message);
                    return true;
                }, CancellationToken.None);

                return true;
            }
            catch (Exception e) when (e is ProduceException<Null, string> || e is TimeoutRejectedException)
            {
                _logger.LogError(e, "Ошибка добавления в кафку {value}. Достигнут лимит ретраев/времени ({timeoutMs} ms)", message.Value, _produceRetryTotalTimeoutMs);
                return false;

            }
        }

        public async Task<bool> Produce(List<Message<string, string>> messages, string? topic = null)
        {
            if (messages.Count == 0)
            {
                _logger.LogDebug("Нет сообщений для отправки в кафку");
                return false;
            }


            _producer ??= new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                // Задержка в мс между отправкой пакетов сообщений, нужно для равномерного распределения сообщений в партициях
                LingerMs = 0,
                // Ожидание сохранения сообщения во всех брокерах кластера, а не только в лидере
                Acks = Acks.All
            }).Build();

            topic ??= _topic;

            foreach (var message in messages)
            {
                try
                {
                    await _producer.ProduceAsync(topic, message);
                }
                // В случае переполнения локальной очереди, необходимо выполнить отправку сообщений и сохранить последнее неотправленное сообщение
                catch (ProduceException<string, string> pe)
                {
                    _logger.LogCritical("error: {peMessage} topic: {topic} message:{message}", pe.Message, topic, message.Key.Equals("compress") ? _compressService.Decompress(message.Value) : message);
                    _producer.Flush(TimeSpan.FromSeconds(10));
                    await _producer.ProduceAsync(topic, message);

                    throw new Exception($"error: {pe.Message} topic: {topic} message:{message}");
                }
            }

            _producer.Flush(TimeSpan.FromSeconds(10));
            return true;
        }


        public Message<string, string>? Consume()
        {
            _logger.LogDebug("Начало получения сообщения из Kafka");
            _consumer ??= new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                GroupId = _groupId,
                BootstrapServers = _bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                MaxPollIntervalMs = 30_000_000
            }).Build();

            if (!_consumer.Subscription.Any() || !_consumer.Subscription.Contains(_topic))
                _consumer.Subscribe(_topic);

            try
            {
                var cr = _consumer.Consume();
                _logger.LogDebug("Offset =  {crOffset}, Partition = {crTopicPartitionOffset}, Topic = {crTopic}", cr.Offset, cr.TopicPartitionOffset.Partition.Value, cr.Topic);
                return cr.Message;
            }
            catch (ConsumeException e)
            {
                _logger.LogError("Ошибка получения сообщения: {eErrorReason}", e.Error.Reason);
                return null;
            }
            finally
            {
                _logger.LogDebug("Завершение получения сообщения из Kafka");
            }
        }

    }
}
