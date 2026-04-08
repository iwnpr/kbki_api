using Cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_lib.core;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cache_lib.Implementations
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CacheService> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _redisDb;
        private readonly int _uniqueIdExpirityDays;
        private readonly int _permissionsExpirationHours;
        private readonly int _reconnectCount;
        private readonly long _defaultHashExpirationMinutes;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public CacheService(IConfiguration config, ILogger<CacheService> logger, IConnectionMultiplexer connectionMultiplexer)
        {
            _config = config;
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _redisDb = _connectionMultiplexer.GetDatabase(_config.GetValue<int>("RedisCache:DBIndex"));
            _uniqueIdExpirityDays = config.GetValue<int?>("RedisCache:RequestIdUniqueDays") ?? 1;
            _permissionsExpirationHours = config.GetValue<int?>("RedisCache:PermissionsExpirationHours") ?? 4;
            _reconnectCount = config.GetValue<int?>("RedisCache:ReconnectCount") ?? 5;
            var retentionHours = config.GetValue<int?>("ApiV3Contract:ResponseRetentionHours")
                                 ?? config.GetValue<int?>("RedisCache:HashExpirityHours")
                                 ?? 8;
            _defaultHashExpirationMinutes = retentionHours * 60L;
        }

        /// <summary>
        /// Установить значение в redis Hash
        /// </summary>
        /// <param name="pKey">Ключ</param>
        /// <param name="pField">Поле</param>
        /// <param name="pData">Данные</param>
        /// <param name="dbIndex">id БД</param>
        public async Task AddHash(string methodName, string pKey, string pField, string pStrData, CancellationToken? ct = null)
        {
            await AddHash(methodName, pKey, pField, Encoding.UTF8.GetBytes(pStrData), ct: ct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="pKey"></param>
        /// <param name="pField"></param>
        /// <param name="pData"></param>
        /// <param name="dbIndex"></param>
        public async Task AddHash(string methodName, string pKey, string pField, byte[] pData, CancellationToken? ct = null)
        {
            try
            {
                var key = KeyFormatter([methodName, pKey]);
                await _redisDb.HashSetAsync(key, [ new(pField,
                    pField == "SignedRequest" ||
                    pField == "SignedResponse" ||
                    pField == "SignedQBCHResponse" ||
                    pField == "request_certificate_data" ||
                    pField == "request_signed_data" ||
                    pField == "response_signed_data" ? pData : Encoding.UTF8.GetString(pData)) ]); //TODO перенести

                _logger.LogTrace("Redis add cache db key: {pGuid}", pKey);
            }
            catch (Exception e)
            {
                _logger.LogCritical("Redis critical: {Message}", e.Message);
                throw;
            }
        }

        public async Task AddHashArray(string methodName, string pKey, Dictionary<string, byte[]> dictionary)
        {
            try
            {
                var transaction = _redisDb.CreateTransaction();

                var key = KeyFormatter(new[] { methodName, pKey });

                foreach (var entry in dictionary)
                {
                    transaction.HashSetAsync(key, new HashEntry[] { new(entry.Key,
                        entry.Key == "SignedRequest" ||
                        entry.Key == "SignedResponse" ||
                        entry.Key == "SignedQBCHResponse" ||
                        entry.Key == "request_certificate_data" ||
                        entry.Key == "request_signed_data" ||
                        entry.Key == "response_signed_data" ? entry.Value : Encoding.UTF8.GetString(entry.Value)) });
                }

                _logger.LogTrace("Redis add cache db key: {pGuid}", pKey);
                await transaction.ExecuteAsync();
            }
            catch (Exception e)
            {
                _logger.LogCritical("Redis critical: {Message}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Формирование ключа для сохранения в redis
        /// </summary>
        /// <param name="keyParts">Список частей ключа</param>
        /// <returns></returns>
        private static string KeyFormatter(string[] keyParts)
        {
            return $"QBCH:{string.Join(':', keyParts)}";
        }

        /// <summary>
        /// Получить значение из redis hash
        /// </summary>
        /// <param name="pKey">Ключ</param>
        /// <param name="pField">Поле</param>
        /// <param name="dbIndex">id БД</param>
        /// <returns></returns>
        public bool TryGetHash(string methodName, string pKey, string pField, [NotNullWhen(true)] out byte[]? bytes)
        {
            try
            {
                bytes = _redisDb.HashGet(KeyFormatter(new[] { methodName, pKey }), pField);

                if (bytes is null || bytes.Length == 0)
                {
                    bytes = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Redis error");

                bytes = null;
                return false;
            }
        }

        public Result<byte[]> TryGetHashV2(string methodName, string pKey, string pField)
        {
            try
            {
                byte[] bytes = _redisDb.HashGet(KeyFormatter(new[] { methodName, pKey }), pField);

                if (bytes is null || bytes.Length == 0)
                {

                    return Result<byte[]>.Failure(new Error(404, "Data on redis not found"));
                }

                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Redis error");
                return Result<byte[]>.Failure(new Error(500, ex.Message));
            }
        }

        /// <summary>
        /// Получить значение из redis hash
        /// </summary>
        /// <param name="pKey">Ключ</param>
        /// <param name="pField">Поле</param>
        /// <param name="dbIndex">id БД</param>
        /// <returns></returns>
        public bool TryGetHashValue(string methodName, string pKey, string pField, [NotNullWhen(true)] out RedisValue? value)
        {
            try
            {
                value = _redisDb.HashGet(KeyFormatter(new[] { methodName, pKey }), pField);

                if (!value.Value.HasValue)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Redis error");

                throw;
            }
        }

        /// <summary>
        /// Провекра существования ключа в БД
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dbIndex"></param>
        /// <returns></returns>
        public async Task<bool> KeyExists(string[] keys)
        {
            try
            {
                return await _redisDb.KeyExistsAsync(KeyFormatter(keys));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Redis error");
                throw;
            }
        }

        /// <summary>
        /// Устновка значения уникального request id в рамках организации
        /// </summary>
        /// <param name="requestId">Id Запроса</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="requestDate">Дата запроса</param>
        public async Task AddUniqueRequestId(string methodName, string requestId, string ogrn, DateTime? requestDate = null)
        {
            try
            {
                var key = KeyFormatter(new[] { methodName, ogrn, requestId });
                await _redisDb.SetAddAsync(key, requestDate?.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"));
                await _redisDb.KeyExpireAsync(key, DateTime.Today.AddDays(_uniqueIdExpirityDays));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Redis error");
            }
        }

        /// <summary>
        /// Валидация уникальности requestid в течение календароного дня
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="requestDate"></param>
        /// <returns></returns>
        public async Task<bool> IsUniqueRequestId(string requestId, string ogrn, string methodName, int? dbIndex = null)
        {
            var result = await KeyExists(new[] { methodName, ogrn, requestId });
            return !result;
        }

        /// <inheritdoc/>
        public async Task<bool> HashFieldExists(string methodName, string requestId, string fieldName)
        {
            return await _redisDb.HashExistsAsync(KeyFormatter(new[] { methodName, requestId }), fieldName);
        }

        public async Task TrySetKeyExpiration(string methodName, string pKey, long minutes, CancellationToken? ct = null)
        {
            var key = KeyFormatter(new[] { methodName, pKey });
            var ttlInMinutes = minutes > 0 ? minutes : _defaultHashExpirationMinutes;

            try
            {
                await _redisDb.KeyExpireAsync(key, TimeSpan.FromMinutes(ttlInMinutes));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось установить время жизни для ключа {key}", key);
            }
        }

        public async Task ListSet(string[] key, string value, CancellationToken? ct = null)
        {
            try
            {
                var constructedKey = KeyFormatter(key);
                await _redisDb.ListLeftPushAsync(constructedKey, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось установить сохранить данные ListSet {key}: {value}", key, value);
            }
        }
    }
}
