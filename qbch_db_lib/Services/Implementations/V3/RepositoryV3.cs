using Cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using NRedisStack.Search;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using System.Collections;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Qbch_db_lib.Services.Implementations.V3;

/// <summary>
/// Репозиторий версии 3.0 для доступа к данным КБКИ
/// Работает только с V3-конфигурацией
/// </summary>
public class RepositoryV3(IConfiguration config, ILogger<RepositoryV3> logger, IKeyValueStorageService cacheService, ApiV3ContractOptions contractOptions, ApiV3ContractRules contractRules) : IRepositoryV3
{
    private readonly IConfiguration _config = config;
    private readonly ILogger<RepositoryV3> _logger = logger;
    private readonly IKeyValueStorageService _cacheService = cacheService;

    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;

    private readonly string[] _qbchDbConnectionPool = config.GetSection("ConnectionPoolV3:QbchDb").Get<string[]>() ?? [];
    private readonly string[] _searchSubjectsConnectionPool = config.GetSection("ConnectionPoolV3:QbchSearchSubjects").Get<string[]>() ?? [];
    private readonly string[] _calcOfAmpConnectionPool = config.GetSection("ConnectionPoolV3:QbchCalcOfAmp").Get<string[]>() ?? [];
    private readonly string[] _selfProhibitionConnectionPool = config.GetSection("ConnectionPoolV3:QbchSelfProhibition").Get<string[]>() ?? [];
    private readonly string[] _antifraudConnectionPool = config.GetSection("ConnectionPoolV3:QbchAntifraud").Get<string[]>() ?? [];

    private readonly int _qbchDbTimeout = config.GetValue<int>("APIConfiguration:QbchDBreconnectCancelTimeoutMs");
    private readonly int _searchSubjectsTimeout = config.GetValue<int>("APIConfiguration:SearchSubjectsCancelTimeoutMs");
    private readonly int _calcOfAmpTimeout = config.GetValue<int>("APIConfiguration:QbchCalcOfAmpCancelTimeoutMs");
    private readonly int _selfProhibitionTimeout = config.GetValue<int>("APIConfiguration:SelfProhibitionCancelTimeoutMs");
    private readonly int _antifraudTimeout = config.GetValue<int>("APIConfiguration:AntifraudCancelTimeoutMs", 5000);
    private readonly int _dbConnectDelayMs = config.GetValue<int>("APIConfiguration:DBConnectDelayMs");
    private readonly long _permissionsLifeTime = config.GetValue<long>("RedisCache:PermissionsLifeTimeMinutes");

    private readonly string? _schemaQbchDbV3 = config.GetValue<string>("QbchDbV3:Schema");
    private readonly string? _schemaQbchSearchSubjectsV3 = config.GetValue<string>("QbchSearchSubjectsV3:Schema");
    private readonly string? _schemaQbchCalcOfAmpV3 = config.GetValue<string>("QbchCalcOfAmpV3:Schema");
    private readonly string? _schemaQbchSelfProhibitionV3 = config.GetValue<string>("QbchSelfProhibitionV3:Schema");
    private readonly string? _schemaQbchAntifraudV3 = config.GetValue<string>("QbchAntifraudV3:Schema");

    private const string PermissionsCacheName = "permissionsv3";

    /// <summary>
    /// Возвращает список идентификаторов субъектов
    /// </summary>
    /// <param name="request">XML запроса на поиск субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>Список идентификаторов субъектов.</returns>
    public async Task<List<long>> GetSearchAllSubjectsV3(string request, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchSearchSubjectsV3:Procedures:SearchAllSubjects");

        if (string.IsNullOrWhiteSpace(request) || string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchSearchSubjectsV3))
            return [];

        _logger.LogDebug("XML для поиска субъектов ({Proc}): {Xml}", procName, request);

        var sql = $"SELECT {_schemaQbchSearchSubjectsV3}.{procName}(@request)";

        var subjects = await ExecuteSubjectIdsAsync(sql, procName, _searchSubjectsConnectionPool, timeLeftMs ?? _searchSubjectsTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("request", NpgsqlDbType.Xml, request);
        });

        _logger.LogDebug("Кол-во субъектов - {SubjectCount}. Запрос: ({Proc}): {Xml}", subjects.Count, procName, request);

        return subjects;
    }

    private async Task<List<long>> ExecuteSubjectIdsAsync(string sql, string resultColumn, string[] connectionPool, long timeoutMs, Action<NpgsqlCommand> addParams)
    {
        var result = new List<long>();

        if (connectionPool.Length == 0)
        {
            return result;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        while (!cts.Token.IsCancellationRequested)
        {
            for (var i = 0; i < connectionPool.Length; i++)
            {
                using var connection = new NpgsqlConnection(connectionPool[i]);
                try
                {
                    await connection.OpenAsync(cts.Token);
                    using var cmd = new NpgsqlCommand(sql, connection);
                    addParams(cmd);

                    _logger.LogInformation("Выполняется процедура поиска субъектов. PoolIndex={PoolIndex}, ResultColumn={ResultColumn}", i, resultColumn);

                    using var reader = await cmd.ExecuteReaderAsync(cts.Token);

                    while (await reader.ReadAsync(cts.Token))
                    {
                        var ordinal = reader.GetOrdinal(resultColumn);
                        if (await reader.IsDBNullAsync(ordinal, cts.Token))
                        {
                            _logger.LogInformation("Процедура вернула NULL в колонке {ResultColumn}.", resultColumn);
                            continue;
                        }


                        var rawValue = reader.GetValue(ordinal);
                        var subjectIds = ReadSubjectIds(rawValue).ToList();

                        _logger.LogInformation(
                            "Результат процедуры. Column={ResultColumn}, RawValue={@RawValue}, ParsedSubjectIds={@SubjectIds}, Count={Count}",
                            resultColumn,
                            rawValue,
                            subjectIds,
                            subjectIds.Count);

                        result.AddRange(ReadSubjectIds(reader.GetValue(ordinal)));
                    }

                    var distinctResult = result.Distinct().ToList();

                    _logger.LogInformation(
                        "Итоговый результат процедуры поиска субъектов. SubjectIds={@SubjectIds}, Count={Count}",
                        distinctResult,
                        distinctResult.Count);


                    return distinctResult;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка процедуры {OperationName}.", nameof(GetSearchAllSubjectsV3));
                    await Task.Delay(_dbConnectDelayMs);
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }
        var timeoutResult = result.Distinct().ToList();

        _logger.LogWarning(
            "Таймаут выполнения процедуры поиска субъектов. Частичный результат: SubjectIds={@SubjectIds}, Count={Count}",
            timeoutResult,
            timeoutResult.Count);

        return timeoutResult;
    }


    private static List<long> ReadSubjectIds(object? value)
    {
        if (value is null || value is DBNull)
        {
            return [];
        }

        if (value is long[] longArray)
        {
            return longArray.ToList();
        }

        if (value is int[] intArray)
        {
            return intArray.Select(Convert.ToInt64).ToList();
        }

        if (value is IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object>()
                .Where(item => item is not null && item is not DBNull)
                .Select(Convert.ToInt64)
                .ToList();
        }

        return [Convert.ToInt64(value)];
    }

    /// <summary>
    /// Возвращает блок обязательств (АМП) по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с обязательствами для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetCalculationOfAmpV3(List<long> subjectIds, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchCalcOfAmpV3:Procedures:CalculationOfAmp");

        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchCalcOfAmpV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchCalcOfAmpV3}.{procName}(@subj_id)";
        var value = await ExecuteScalarAsync(sql, procName, _calcOfAmpConnectionPool, timeLeftMs ?? _calcOfAmpTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
        }, nameof(GetCalculationOfAmpV3));

        if (value is string xml && !string.IsNullOrWhiteSpace(xml))
        {
            return XElement.Parse(xml);
        }

        return null;
    }

    /// <summary>
    /// Возвращает сведения о самозапрете по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с блоком самозапрета для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetSelfProhibitionV3(List<long> subjectIds, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchSelfProhibitionV3:Procedures:GetSelfProhibition");

        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchSelfProhibitionV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchSelfProhibitionV3}.{procName}(@subj_id)";
        var value = await ExecuteScalarAsync(sql, procName, _selfProhibitionConnectionPool, timeLeftMs ?? _selfProhibitionTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
        }, nameof(GetSelfProhibitionV3));

        if (value is string xml && !string.IsNullOrWhiteSpace(xml))
        {
            return XElement.Parse(xml);
        }

        return null;
    }

    /// <summary>
    /// Возвращает антифрод-записи по дате рождения и ИНН субъекта.
    /// </summary>
    /// <param name="birthDate">Дата рождения субъекта.</param>
    /// <param name="inn">ИНН субъекта.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с антифрод-записями для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetAntifraudV3(DateTime birthDate, string inn, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchAntifraudV3:Procedures:GetAntifraud");

        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(inn))
        {
            return null;
        }

        var procedureFullName = procName.Contains('.') || string.IsNullOrWhiteSpace(_schemaQbchAntifraudV3)
            ? procName
            : $"{_schemaQbchAntifraudV3}.{procName}";
        
        var sql = $"SELECT {procedureFullName}(@p_birth, @p_tax_num)";

        var value = await ExecuteScalarAsync(sql, procName, _antifraudConnectionPool, timeLeftMs ?? _antifraudTimeout, cmd =>
            {
                cmd.Parameters.AddWithValue("p_birth", NpgsqlDbType.Date, birthDate.Date);
                cmd.Parameters.AddWithValue("p_tax_num", NpgsqlDbType.Text, inn);
            },
            nameof(GetAntifraudV3));

        if (value is string xml && !string.IsNullOrWhiteSpace(xml))
        {
            return XElement.Parse(xml);
        }

        return null;
    }

    /// <summary>
    /// Проверяет наличие прав доступа у абонента к указанному сервису.
    /// </summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="serviceName">Имя сервиса.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><see langword="true"/>, если доступ разрешен.</returns>
    public async Task<bool> IsPermissionGrantedV3(string? thumbprint, string? serviceName, CancellationToken? ct = null)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
            return false;

        var procName = _config.GetValue<string>("QbchDbV3:Procedures:IsPermissionGranted");

        if (string.IsNullOrWhiteSpace(procName))
            return false;

        var normalizedServiceName = NormalizeServiceNameForAccessCheck(serviceName);

        if (string.IsNullOrWhiteSpace(normalizedServiceName))
            return false;

        var foundInCache = _cacheService.TryGetHashValue(PermissionsCacheName, thumbprint, normalizedServiceName, out var cachedValue);

        if (foundInCache && bool.TryParse(cachedValue.Value, out var cachedResult))
            return cachedResult;

        _logger.LogDebug("В Redis не найдены права доступа для сертификата с отпечатком {thumbprint}", thumbprint);

        var sql = $"SELECT {_schemaQbchDbV3}.{procName}(@thumbprint, @serviceName)";
        var value = await ExecuteScalarAsync(sql, procName, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
            cmd.Parameters.AddWithValue("serviceName", normalizedServiceName);
        }, nameof(IsPermissionGrantedV3), ct);

        if (value is not bool result)
            return false;

        try
        {
            await _cacheService.AddHash(PermissionsCacheName, thumbprint, normalizedServiceName, result.ToString(), ct);
            await _cacheService.TrySetKeyExpiration(PermissionsCacheName, thumbprint, _permissionsLifeTime, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "При установке прав V3 в redis возникла ошибка.");
        }

        return result;
    }

    /// <summary>
    /// Возвращает ИНН/ОГРН абонента по отпечатку сертификата.
    /// </summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <returns>XML с реквизитами абонента.</returns>
    public async Task<XElement?> GetInnOgrnByThumbprintV3(string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return null;
        }

        var procName = _config.GetValue<string>("QbchDbV3:Procedures:GetInnOgrnByThumbprint");
        if (string.IsNullOrWhiteSpace(procName))
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchDbV3}.{procName}(@thumbprint)";
        var value = await ExecuteScalarAsync(sql, procName, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        }, "GetInnOgrnByThumbprintV3");

        return value is string xml && !string.IsNullOrWhiteSpace(xml)
            ? XElement.Parse(xml)
            : null;
    }

    /// <summary>
    /// Возвращает идентификатор абонента по ОГРН/ОГРНИП.
    /// </summary>
    /// <param name="psrn">ОГРН/ОГРНИП абонента.</param>
    /// <returns>Идентификатор абонента или <see langword="null"/>.</returns>
    public async Task<int?> GetAbonentKeyIdByPSRN(string? psrn)
    {
        if (string.IsNullOrWhiteSpace(psrn) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return null;
        }

        var sql = $"SELECT key_id FROM {_schemaQbchDbV3}.tr_abonents WHERE ogrn = @psrn LIMIT 1";
        var value = await ExecuteFirstColumnAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("psrn", psrn);
        }, "GetAbonentKeyIdByPSRNV3");

        return value as int?;
    }

    /// <summary>
    /// Проверяет наличие сертификата абонента в базе.
    /// </summary>
    /// <param name="cert">Сертификат в виде массива байтов.</param>
    /// <returns><see langword="true"/>, если сертификат найден.</returns>
    public async Task<bool> IsCertExist(byte[] cert)
    {
        if (cert.Length == 0 || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return false;
        }

        var certificate = new X509Certificate2(cert);
        var sql = $"SELECT EXISTS(SELECT 1 FROM {_schemaQbchDbV3}.tr_abonent_certificates WHERE UPPER(thumbprint)=UPPER(@thumbprint))";

        var value = await ExecuteFirstColumnAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", certificate.Thumbprint ?? string.Empty);
        }, "IsCertExistV3");

        return value is bool boolValue && boolValue;
    }

    public async Task<bool> IsCertActive(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return false;
        }

        var sql = $"SELECT EXISTS(SELECT 1 FROM {_schemaQbchDbV3}.tr_abonent_certificates WHERE UPPER(thumbprint)=UPPER(@thumbprint) AND is_active=true)";
        var value = await ExecuteFirstColumnAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        }, "IsCertActiveV3");

        return value is bool boolValue && boolValue;
    }

    public async Task<int> GetActiveCertificatesCountByThumbprint(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return 0;
        }

        var sql = $"""
                   SELECT COUNT(*)
                   FROM {_schemaQbchDbV3}.tr_abonent_certificates ac
                   WHERE ac.is_active = true
                     AND ac.abonent_key_id = (
                        SELECT abonent_key_id
                        FROM {_schemaQbchDbV3}.tr_abonent_certificates
                        WHERE UPPER(thumbprint) = UPPER(@thumbprint)
                        LIMIT 1
                     )
                   """;

        var value = await ExecuteFirstColumnAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        }, "GetActiveCertificatesCountByThumbprintV3");

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => 0
        };
    }

    /// <summary>
    /// Добавляет сертификат абонента в базу.
    /// </summary>
    /// <param name="abonentId">Идентификатор абонента.</param>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="expirationDate">Дата окончания действия сертификата.</param>
    /// <returns><see langword="true"/>, если запись добавлена.</returns>
    public async Task<bool> AddCertificate(int abonentId, string thumbprint, DateTime expirationDate)
    {
        if (abonentId <= 0 || string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return false;
        }

        var sql = $"INSERT INTO {_schemaQbchDbV3}.tr_abonent_certificates(abonent_key_id, thumbprint, expiration_date, is_active) VALUES (@abonentId, @thumbprint, @expirationDate, true)";
        var affectedRows = await ExecuteNonQueryAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("abonentId", abonentId);
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
            cmd.Parameters.AddWithValue("expirationDate", expirationDate);
        }, "AddCertificateV3");

        return affectedRows > 0;
    }

    /// <summary>
    /// Деактивирует сертификат абонента по отпечатку.
    /// </summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <returns><see langword="true"/>, если запись изменена.</returns>
    public async Task<bool> SetCertificateInactive(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(_schemaQbchDbV3))
        {
            return false;
        }

        var sql = $"UPDATE {_schemaQbchDbV3}.tr_abonent_certificates SET is_active=false WHERE UPPER(thumbprint)=UPPER(@thumbprint)";
        var affectedRows = await ExecuteNonQueryAsync(sql, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
        }, "SetCertificateInactiveV3");

        return affectedRows > 0;
    }

    public async Task<List<long>?> SearchContractSubjectsForDlPutV3(string subjectXml, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchSearchSubjectsV3:Procedures:SearchContractSubjectsForDlPut");

        if (string.IsNullOrWhiteSpace(subjectXml) || string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchSearchSubjectsV3))
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchSearchSubjectsV3}.{procName}(@subject)";
        var value = await ExecuteScalarAsync(sql, procName, _searchSubjectsConnectionPool, timeLeftMs ?? _searchSubjectsTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subject", NpgsqlDbType.Xml, subjectXml);
        }, nameof(SearchContractSubjectsForDlPutV3));

        return value as List<long>;
    }

    public async Task<bool?> ContractUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchCalcOfAmpV3:Procedures:ContractUidExistsForDlPut");
        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchCalcOfAmpV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchCalcOfAmpV3}.{procName}(@subj_id, @uid)";
        var value = await ExecuteScalarAsync(sql, procName, _calcOfAmpConnectionPool, timeLeftMs ?? _calcOfAmpTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
            cmd.Parameters.AddWithValue("uid", uid);
        }, nameof(ContractUidExistsForSubjectsV3));

        return value is bool b ? b : null;
    }

    public async Task<bool?> ContractCalculationDateExistsForSubjectsV3(List<long> subjectIds, string uid, DateTime calculationDate, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchCalcOfAmpV3:Procedures:ContractCalculationDateExistsForDlPut");
        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchCalcOfAmpV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchCalcOfAmpV3}.{procName}(@subj_id, @uid, @calc_date)";
        var value = await ExecuteScalarAsync(sql, procName, _calcOfAmpConnectionPool, timeLeftMs ?? _calcOfAmpTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("calc_date", NpgsqlDbType.Date, calculationDate.Date);
        }, nameof(ContractCalculationDateExistsForSubjectsV3));

        return value is bool b ? b : null;
    }

    public async Task<List<long>?> SearchAppealSubjectsByInnForDlPutV3(string inn, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchAntifraudV3:Procedures:SearchAppealSubjectsByInnForDlPut");
        if (string.IsNullOrWhiteSpace(inn) || string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchAntifraudV3))
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchAntifraudV3}.{procName}(@inn)";
        var value = await ExecuteScalarAsync(sql, procName, _antifraudConnectionPool, timeLeftMs ?? _antifraudTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("inn", inn);
        }, nameof(SearchAppealSubjectsByInnForDlPutV3));

        return value as List<long>;
    }

    public async Task<bool?> AppealUidExistsForSubjectsV3(List<long> subjectIds, string uid, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchAntifraudV3:Procedures:AppealUidExistsForDlPut");
        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchAntifraudV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchAntifraudV3}.{procName}(@subj_id, @uid)";
        var value = await ExecuteScalarAsync(sql, procName, _antifraudConnectionPool, timeLeftMs ?? _antifraudTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
            cmd.Parameters.AddWithValue("uid", uid);
        }, nameof(AppealUidExistsForSubjectsV3));

        return value is bool b ? b : null;
    }

    public async Task<bool?> AppealStageExistsForSubjectsV3(List<long> subjectIds, string uid, ushort stage, long? timeLeftMs = null)
    {
        var procName = _config.GetValue<string>("QbchAntifraudV3:Procedures:AppealStageExistsForDlPut");
        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(_schemaQbchAntifraudV3) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {_schemaQbchAntifraudV3}.{procName}(@subj_id, @uid, @stage)";
        var value = await ExecuteScalarAsync(sql, procName, _antifraudConnectionPool, timeLeftMs ?? _antifraudTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
            cmd.Parameters.AddWithValue("uid", uid);
            cmd.Parameters.AddWithValue("stage", (int)stage);
        }, nameof(AppealStageExistsForSubjectsV3));

        return value is bool b ? b : null;
    }

    private static string? NormalizeServiceNameForAccessCheck(string serviceName)
    {
        var normalized = serviceName.Split('?', 2)[0].Trim().Trim('/').ToLowerInvariant();
        normalized = normalized.Split(':', 2)[0];

        if (normalized.StartsWith("v3.0/"))
        {
            normalized = normalized["v3.0/".Length..];
        }
        else if (normalized.StartsWith("v3/"))
        {
            normalized = normalized["v3/".Length..];
        }

        return normalized switch
        {
            "dlrequest" => "dlrequest",
            "dlanswer" => "dlanswer",
            "dlput" => "dlput",
            "dlputanswer" => "dlputanswer",
            "certadd" => "certadd",
            "certrevoke" => "certrevoke",
            _ => null
        };
    }

    private async Task<object?> ExecuteScalarAsync(
        string sql,
        string resultColumn,
        string[] connectionPool,
        long timeoutMs,
        Action<NpgsqlCommand> addParams,
        string? operationName = null,
        CancellationToken? cancellationToken = null)
    {
        if (connectionPool.Length == 0)
        {
            return null;
        }

        using var cts = cancellationToken is null
            ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs))
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);

        if (cancellationToken is not null)
        {
            cts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
        }

        while (!cts.Token.IsCancellationRequested)
        {
            for (var i = 0; i < connectionPool.Length; i++)
            {
                using var connection = new NpgsqlConnection(connectionPool[i]);
                try
                {
                    await connection.OpenAsync(cts.Token);
                    using var cmd = new NpgsqlCommand(sql, connection);
                    addParams(cmd);
                    using var reader = await cmd.ExecuteReaderAsync(cts.Token);

                    while (await reader.ReadAsync(cts.Token))
                    {
                        var ordinal = reader.GetOrdinal(resultColumn);
                        if (await reader.IsDBNullAsync(ordinal, cts.Token))
                        {
                            return null;
                        }

                        return reader.GetValue(ordinal);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка процедуры {OperationName}.", operationName ?? resultColumn);
                    await Task.Delay(_dbConnectDelayMs);
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }

        return null;
    }

    private async Task<object?> ExecuteFirstColumnAsync(
        string sql,
        string[] connectionPool,
        int timeoutMs,
        Action<NpgsqlCommand> addParams,
        string operationName)
    {
        if (connectionPool.Length == 0)
        {
            return null;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        while (!cts.Token.IsCancellationRequested)
        {
            for (var i = 0; i < connectionPool.Length; i++)
            {
                using var connection = new NpgsqlConnection(connectionPool[i]);
                try
                {
                    await connection.OpenAsync(cts.Token);
                    using var cmd = new NpgsqlCommand(sql, connection);
                    addParams(cmd);
                    using var reader = await cmd.ExecuteReaderAsync(cts.Token);

                    if (await reader.ReadAsync(cts.Token) && !await reader.IsDBNullAsync(0, cts.Token))
                    {
                        return reader.GetValue(0);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка запроса {OperationName}.", operationName);
                    await Task.Delay(_dbConnectDelayMs);
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }

        return null;
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        string[] connectionPool,
        int timeoutMs,
        Action<NpgsqlCommand> addParams,
        string operationName)
    {
        if (connectionPool.Length == 0)
        {
            return 0;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        while (!cts.Token.IsCancellationRequested)
        {
            for (var i = 0; i < connectionPool.Length; i++)
            {
                using var connection = new NpgsqlConnection(connectionPool[i]);
                try
                {
                    await connection.OpenAsync(cts.Token);
                    using var cmd = new NpgsqlCommand(sql, connection);
                    addParams(cmd);
                    return await cmd.ExecuteNonQueryAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Ошибка запроса {OperationName}.", operationName);
                    await Task.Delay(_dbConnectDelayMs);
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }

        return 0;
    }
}