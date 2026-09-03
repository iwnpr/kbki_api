using System.Text;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace QBCH_backup_tool.Services;

/// <summary>
/// Работа с Redis напрямую через StackExchange.Redis.
/// Утилита запускается отдельно от API и не зависит от его сборок, поэтому формат ключа
/// и правила записи полей продублированы здесь. Они должны совпадать с тем, что делает
/// <c>Cache_lib.KeyValueStorageService</c>, иначе восстановленная запись будет отличаться
/// от записи, сделанной API.
/// </summary>
public sealed class RedisBackupStore
{
    /// <summary>
    /// Поля, которые хранятся в Redis сырыми байтами. Все остальные — как UTF-8 текст.
    /// Список повторяет <c>Cache_lib.KeyValueStorageService.AddHashArray</c>.
    /// </summary>
    private static readonly HashSet<string> BinaryFields = new(StringComparer.Ordinal)
    {
        "SignedRequest",
        "SignedResponse",
        "SignedQBCHResponse",
        "request_certificate_data",
        "request_signed_data",
        "response_signed_data"
    };

    private readonly ILogger<RedisBackupStore> _logger;
    private readonly IDatabase _db;

    public RedisBackupStore(ILogger<RedisBackupStore> logger, IConnectionMultiplexer multiplexer, int dbIndex)
    {
        _logger = logger;
        _db = multiplexer.GetDatabase(dbIndex);
    }

    /// <summary>
    /// Признак того, что результат обработки уже сохранён обработчиком завершения.
    /// Проверять существование самого ключа нельзя: хэш создаётся раньше, на этапе
    /// агрегации ответов (<c>qbch_tasks_aggregate_xml</c> и др.), и существует всегда.
    /// Это поле пишет только <c>ConstructResultData</c>.
    /// </summary>
    private const string ResultMarkerField = "api_version";

    /// <summary>Формат ключа записи: <c>QBCH:{serviceName}:{id}</c>.</summary>
    public static string BuildKey(string serviceName, Guid id) => $"QBCH:{serviceName}:{id}";

    /// <summary>Сохранён ли в Redis результат обработки по этой записи.</summary>
    public async Task<bool> ResultExistsAsync(string serviceName, Guid id)
    {
        var key = BuildKey(serviceName, id);
        var exists = await _db.HashExistsAsync(key, ResultMarkerField);
        _logger.LogDebug("Redis: поле {field} ключа {key} {state}.",
            ResultMarkerField, key, exists ? "заполнено" : "отсутствует");
        return exists;
    }

    /// <summary>Записывает набор полей в redis-хэш записи.</summary>
    public async Task WriteHashAsync(string serviceName, Guid id, Dictionary<string, byte[]> fields)
    {
        var key = BuildKey(serviceName, id);

        var entries = fields
            .Select(field => new HashEntry(
                field.Key,
                BinaryFields.Contains(field.Key)
                    ? field.Value
                    : Encoding.UTF8.GetString(field.Value)))
            .ToArray();

        await _db.HashSetAsync(key, entries);
        _logger.LogDebug("Redis: в ключ {key} записано {count} полей.", key, entries.Length);
    }
}
