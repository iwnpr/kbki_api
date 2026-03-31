using QBCH_lib.core;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;

namespace Cache_lib.Interfaces
{
    public interface ICacheService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="pKey"></param>
        /// <param name="pField"></param>
        /// <param name="pData"></param>
        /// <param name="dbIndex"></param>
        Task AddHash(string methodName, string pKey, string pField, byte[] pData, CancellationToken? ct = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="pKey"></param>
        /// <param name="pField"></param>
        /// <param name="pData"></param>
        /// <param name="dbIndex"></param>
        Task AddHash(string methodName, string pKey, string pField, string pData, CancellationToken? ct = null);

        Task AddHashArray(string methodName, string pKey, Dictionary<string, byte[]> dictionary);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisFieldName"></param>
        /// <param name="dbIndex"></param>
        /// <returns></returns>
        bool TryGetHash(string methodName, string pKey, string pField, [NotNullWhen(true)] out byte[]? bytes);
        public Result<byte[]> TryGetHashV2(string methodName, string pKey, string pField);

        bool TryGetHashValue(string methodName, string pKey, string pField, [NotNullWhen(true)] out RedisValue? value);

        /// <summary>
        /// Проверка что ключ существует
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="dbIndex">id бд в редисе</param>
        /// <returns>Ключ существует</returns>
        Task<bool> KeyExists(string[] keys);

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="methodName"></param>
        ///// <param name="key"></param>
        ///// <param name="field"></param>
        ///// <param name="result"></param>
        ///// <returns></returns>
        //bool TryGetIntValue(string methodName, string key, string field, [NotNullWhen(true)] out int result, int? dbIndex = null);

        ///// <summary>
        ///// Получить все значения из Hash
        ///// </summary>
        ///// <param name="methodName"></param>
        ///// <param name="pKey"></param>
        ///// <param name="dbIndex"></param>
        ///// <returns></returns>
        //Task<HashEntry[]> TryGetHashAll(string methodName, string pKey, int? dbIndex = null);

        /// <summary>
        /// Валидация уникальности requestid в течение календароного дня
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="requestDate"></param>
        /// <returns></returns>
        Task<bool> IsUniqueRequestId(string requestId, string ogrn, string methodName, int? dbIndex = null);

        /// <summary>
        /// Устновка значения уникального request id в рамках организации
        /// </summary>
        /// <param name="requestId">Id Запроса</param>
        /// <param name="inn">ИНН</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="requestDate">Дата запроса</param>
        Task AddUniqueRequestId(string methodName, string requestId, string ogrn, DateTime? requestDate = null);

        /// <summary>
        /// Проверить существует ли поле в хэше
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="requestId"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        Task<bool> HashFieldExists(string methodName, string requestId, string fieldName);

        public Task TrySetKeyExpiration(string methodName, string pKey, long minutes, CancellationToken? ct = null);

        public Task ListSet(string[] key, string value, CancellationToken? ct = null);
    }
}
