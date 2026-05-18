using Confluent.Kafka;
using KafkaService_lib.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
            _produceRetryCount = Math.Max(0, _config.GetValue<int?>("KafkaService:ProduceRetryCount") ?? 2);
            _produceRetryDelayMs = Math.Max(0, _config.GetValue<int?>("KafkaService:ProduceRetryDelayMs") ?? 100);
            _produceRetryTotalTimeoutMs = Math.Max(1, _config.GetValue<int?>("KafkaService:ProduceRetryTotalTimeoutMs") ?? 2000);
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
        //Обработка ошибки при доставке сообщения в топик

        void DeliveryReportHandler(DeliveryReport<string, string> deliveryReport)
        {

            if (deliveryReport.Error.IsError || deliveryReport.Error.IsFatal || deliveryReport.Error.IsLocalError || deliveryReport.Error.IsBrokerError)
            {
                _logger.LogCritical($"Kafka Delivery Topic: {deliveryReport.Topic} Partition: {deliveryReport.Partition} Offset: {deliveryReport.Offset} Error: {deliveryReport.Error.Reason}");
                throw new Exception($"Kafka Delivery Topic: {deliveryReport.Topic} Partition: {deliveryReport.Partition} Offset: {deliveryReport.Offset} Error: {deliveryReport.Error.Reason}");
            }
            else
                _logger.LogDebug($"Kafka Delivery Topic: {deliveryReport.Topic} Partition: {deliveryReport.Partition} Offset: {deliveryReport.Offset}");
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
            var stopwatch = Stopwatch.StartNew();

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var elapsedMsBeforeAttempt = stopwatch.ElapsedMilliseconds;
                if (elapsedMsBeforeAttempt >= _produceRetryTotalTimeoutMs)
                {
                    _logger.LogCritical(
                        "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Лимит времени исчерпан до отправки ({elapsedMs} ms из {timeoutMs} ms)",
                        message.Value,
                        attempt,
                        maxAttempts,
                        elapsedMsBeforeAttempt,
                        _produceRetryTotalTimeoutMs);
                    return false;
                }

                try
                {
                    var remainingTimeoutMsBeforeAttempt = _produceRetryTotalTimeoutMs - elapsedMsBeforeAttempt;
                    var produceTask = _producerMsg.ProduceAsync(topic, message);

                    if (remainingTimeoutMsBeforeAttempt <= 0)
                    {
                        _logger.LogCritical(
                            "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Лимит времени исчерпан до отправки ({elapsedMs} ms из {timeoutMs} ms)",
                            message.Value,
                            attempt,
                            maxAttempts,
                            elapsedMsBeforeAttempt,
                            _produceRetryTotalTimeoutMs);
                        return false;
                    }

                    var timeoutTask = Task.Delay((int)Math.Min(int.MaxValue, remainingTimeoutMsBeforeAttempt));
                    var completedTask = await Task.WhenAny(produceTask, timeoutTask);

                    if (completedTask != produceTask)
                    {
                        _logger.LogCritical(
                            "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Превышен общий timeout во время отправки ({timeoutMs} ms)",
                            message.Value,
                            attempt,
                            maxAttempts,
                            _produceRetryTotalTimeoutMs);
                        return false;
                    }

                    await produceTask;
                    return true;
                }
                catch (ProduceException<Null, string> e)
                {
                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    var hasAttemptsLeft = attempt < maxAttempts;
                    var hasTimeLeft = elapsedMs < _produceRetryTotalTimeoutMs;

                    if (!hasAttemptsLeft || !hasTimeLeft)
                    {
                        _logger.LogCritical(e,
                            "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Достигнут лимит ретраев/времени ({elapsedMs} ms из {timeoutMs} ms)",
                            message.Value,
                            attempt,
                            maxAttempts,
                            elapsedMs,
                            _produceRetryTotalTimeoutMs);
                        return false;
                    }

                    _logger.LogWarning(e,
                        "Ошибка добавления в кафку {value}. Попытка {attempt}/{maxAttempts}. Повтор через {delayMs} ms",
                        message.Value,
                        attempt,
                        maxAttempts,
                        _produceRetryDelayMs);

                    if (_produceRetryDelayMs > 0)
                    {
                        var remainingTimeoutMs = _produceRetryTotalTimeoutMs - elapsedMs;
                        var delayMs = (int)Math.Min(_produceRetryDelayMs, remainingTimeoutMs);
                        if (delayMs > 0)
                            await Task.Delay(delayMs);
                    }
                }
            }

            return false;
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
            _logger.LogInformation("Начало получения сообщения из Kafka");
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
                _logger.LogInformation("Offset =  {crOffset}, Partition = {crTopicPartitionOffset}, Topic = {crTopic}", cr.Offset, cr.TopicPartitionOffset.Partition.Value, cr.Topic);
                return cr.Message;
            }
            catch (ConsumeException e)
            {
                _logger.LogCritical("Consume error occured: {eErrorReason}", e.Error.Reason);
                return null;
            }
            finally
            {
                _logger.LogInformation("Завершение получения сообщения из Kafka");
            }
        }

    }
}
