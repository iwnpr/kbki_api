using System.Diagnostics.CodeAnalysis;
using Cache_lib.Interfaces;
using QBCH_lib.core;
using StackExchange.Redis;

namespace QBCH_backup_tool.Services;

/// <summary>
/// Заглушка <see cref="IKeyValueStorageService"/> для запуска с целью только Kafka
/// (<c>--target kafka</c>), когда подключение к Redis не требуется.
/// Методы не вызываются в этом режиме; любой вызов сигнализирует об ошибке конфигурации.
/// </summary>
internal sealed class NullKeyValueStorageService : IKeyValueStorageService
{
    private static NotSupportedException Fail() =>
        new("Redis недоступен: утилита запущена с целью только Kafka (--target kafka).");

    public Task AddHash(string methodName, string pKey, string pField, byte[] pData, CancellationToken? ct = null) => throw Fail();

    public Task AddHash(string methodName, string pKey, string pField, string pData, CancellationToken? ct = null) => throw Fail();

    public Task AddHashArray(string methodName, string pKey, Dictionary<string, byte[]> dictionary) => throw Fail();

    public bool TryGetHash(string methodName, string pKey, string pField, [NotNullWhen(true)] out byte[]? bytes) => throw Fail();

    public Result<byte[]> TryGetHashV2(string methodName, string pKey, string pField) => throw Fail();

    public bool TryGetHashValue(string methodName, string pKey, string pField, [NotNullWhen(true)] out RedisValue? value) => throw Fail();

    public Task<bool> KeyExists(string[] keys) => throw Fail();

    public Task<bool> IsUniqueRequestId(string requestId, string ogrn, string methodName, int? dbIndex = null) => throw Fail();

    public Task AddUniqueRequestId(string methodName, string requestId, string ogrn, DateTime? requestDate = null) => throw Fail();

    public Task<bool> HashFieldExists(string methodName, string requestId, string fieldName) => throw Fail();

    public Task TrySetKeyExpiration(string methodName, string pKey, long minutes, CancellationToken? ct = null) => throw Fail();

    public Task ListSet(string[] key, string value, CancellationToken? ct = null) => throw Fail();
}
