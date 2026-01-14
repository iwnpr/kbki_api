using Adapters_lib.Crypto;
using Adapters_lib.DB;
using Adapters_lib.Kafka;
using Adapters_lib.Kafka.Kafka.Services.Implementation;
using Adapters_lib.Redis;
using Application_lib;
using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using Elastic.Apm.StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Adapters_lib
{
    public static class DIConfigAdapters
    {
        /// <summary>
        /// Расширение для конфигурирования адаптеров
        /// </summary>
        /// <param name="services">Сервисы</param>
        /// <param name="configuration">Кофигурация</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection DIAddAdapters(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConfig = configuration.GetSection("Redis");
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RedisAdapter>>();
                var options = new ConfigurationOptions
                {
                    EndPoints = { $"{redisConfig["Host"]}:{redisConfig["Port"]}" },
                    User = redisConfig["User"],
                    Password = redisConfig["Password"],
                    AbortOnConnectFail = redisConfig.GetValue<bool>("AbortOnConnectFail"),
                    ConnectTimeout = redisConfig.GetValue<int>("ConnectTimeout"),
                    SyncTimeout = redisConfig.GetValue<int>("SyncTimeout"),
                    DefaultDatabase = redisConfig.GetValue<int>("Database"),
                    ConnectRetry = redisConfig.GetValue<int>("ConnectRetry")
                };
                try
                {
                    var connection = ConnectionMultiplexer.Connect(options);
                    logger.LogInformation("Содениение с Redis устновлено");
                    connection.UseElasticApm();
                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Не удалось установить соединение с Redis");
                    throw;
                }
            });

            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RedisAdapter>>();
                var transactionTimeoutMs = configuration.GetValue<int>("KafkaService:TransactionTimeoutMs");
                var messageTimeoutMs = configuration.GetValue<int>("KafkaService:MessageTimeoutMs");
                var requestTimeoutMs = configuration.GetValue<int>("KafkaService:RequestTimeoutMs");
                var socketTimeoutMs = configuration.GetValue<int>("KafkaService:SocketTimeoutMs");
                var bootstrapServers = configuration.GetValue<string>("KafkaService:BootstrapServers");

                logger.LogInformation("Добавление Kafka producer");
                logger.LogDebug("   - transactionTimeoutMs {transactionTimeoutMs}", transactionTimeoutMs);
                logger.LogDebug("   - messageTimeoutMs {messageTimeoutMs}", messageTimeoutMs);
                logger.LogDebug("   - requestTimeoutMs {requestTimeoutMs}", requestTimeoutMs);
                logger.LogDebug("   - socketTimeoutMs {socketTimeoutMs}", socketTimeoutMs);
                logger.LogDebug("   - bootstrapServers {bootstrapServers}", bootstrapServers);

                return new ProducerBuilder<Null, string>(new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    LingerMs = 0,
                    Acks = Acks.All,
                    TransactionTimeoutMs = transactionTimeoutMs,
                    MessageTimeoutMs = messageTimeoutMs,
                    RequestTimeoutMs = requestTimeoutMs,
                    SocketTimeoutMs = socketTimeoutMs,
                    ClientId = Environment.MachineName
                }).BuildWithInstrumentation();
            });

            services.AddScoped<IRedisAdapter, RedisAdapter>();
            services.AddScoped<IDBAdapter, DBAdapter>();
            services.AddScoped<ICryptoAdapter, CryptoAdapter>();
            services.AddScoped<IKafkaAdapter, KafkaAdapter>();
            services.AddSingleton<ICompressService, CompressService>();

            return services;
        }
    }
}
