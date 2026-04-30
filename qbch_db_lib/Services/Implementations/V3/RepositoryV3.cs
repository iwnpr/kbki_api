using Cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Qbch_db_lib.Services.Interfaces.V3;
using QBCH_lib.Configuration;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Xml.Linq;

namespace Qbch_db_lib.Services.Implementations.V3;

/// <summary>
/// Репозиторий версии 3.0 для доступа к данным КБКИ
/// Работает только с V3-конфигурацией
/// </summary>
public class RepositoryV3(
    IConfiguration config,
    ILogger<RepositoryV3> logger,
    ICacheService cacheService,
    ApiV3ContractOptions contractOptions,
    ApiV3ContractRules contractRules)
    : IRepositoryV3
{
    private readonly IConfiguration _config = config;
    private readonly ILogger<RepositoryV3> _logger = logger;
    private readonly ICacheService _cacheService = cacheService;

    private readonly ApiV3ContractOptions _contractOptions = contractOptions;
    private readonly ApiV3ContractRules _contractRules = contractRules;

    private readonly string[] _qbchDbConnectionPool = config.GetSection("ConnectionPoolV3:QbchDb").Get<string[]>() ?? [];
    private readonly string[] _searchSubjectsConnectionPool = config.GetSection("ConnectionPoolV3:QbchSearchSubjects").Get<string[]>() ?? [];
    private readonly string[] _calcOfAmpConnectionPool = config.GetSection("ConnectionPoolV3:QbchCalcOfAmp").Get<string[]>() ?? [];
    private readonly string[] _selfProhibitionConnectionPool = config.GetSection("ConnectionPoolV3:QbchSelfProhibition").Get<string[]>() ?? [];
    private readonly string[] _antifraudConnectionPool = config.GetSection("ConnectionPoolV3:QbchAntifraud").Get<string[]>() ?? [];
    private readonly string[] _creditHistoryPresenceFlagConnectionPool = config.GetSection("ConnectionPoolV3:QbchCreditHistoryPresenceFlag").Get<string[]>() ?? [];

    private readonly int _qbchDbTimeout = config.GetValue<int>("APIConfiguration:QbchDBreconnectCancelTimeoutMs");
    private readonly int _searchSubjectsTimeout = config.GetValue<int>("APIConfiguration:SearchSubjectsCancelTimeoutMs");
    private readonly int _calcOfAmpTimeout = config.GetValue<int>("APIConfiguration:QbchCalcOfAmpCancelTimeoutMs");
    private readonly int _selfProhibitionTimeout = config.GetValue<int>("APIConfiguration:SelfProhibitionCancelTimeoutMs");
    private readonly int _antifraudTimeout = config.GetValue<int>("APIConfiguration:AntifraudCancelTimeoutMs", 5000);
    private readonly int _creditHistoryPresenceFlagTimeout = config.GetValue<int>("APIConfiguration:CreditHistoryPresenceFlagCancelTimeoutMs", 5000);
    private readonly int _dbConnectDelayMs = config.GetValue<int>("APIConfiguration:DBConnectDelayMs");

    private readonly string? _schemaQbchDbV3 = config.GetValue<string>("QbchDbV3:Schema");
    private readonly string? _schemaQbchSearchSubjectsV3 = config.GetValue<string>("QbchSearchSubjectsV3:Schema");
    private readonly string? _schemaQbchCalcOfAmpV3 = config.GetValue<string>("QbchCalcOfAmpV3:Schema");
    private readonly string? _schemaQbchSelfProhibitionV3 = config.GetValue<string>("QbchSelfProhibitionV3:Schema");
    private readonly string? _schemaQbchAntifraudV3 = config.GetValue<string>("QbchAntifraudV3:Schema");
    private readonly string? _schemaQbchCreditHistoryPresenceFlagV3 = config.GetValue<string>("QbchCreditHistoryPresenceFlagV3:Schema");

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
        {
            return [];
        }

        var sql = $"SELECT {_schemaQbchSearchSubjectsV3}.{procName}(@request)";
        var value = await ExecuteScalarAsync(sql, procName, _searchSubjectsConnectionPool, timeLeftMs ?? _searchSubjectsTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("request", NpgsqlDbType.Xml, request);
        });

        return value as List<long> ?? [];
    }

    /// <summary>
    /// Возвращает блок обязательств (АМП) по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с обязательствами для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetCalculationOfAmpV3(List<long> subjectIds, long? timeLeftMs = null)
    {
        var xml = await ExecuteXmlProcedureV3(
            _config.GetValue<string>("QbchCalcOfAmpV3:Procedures:CalculationOfAmp"),
            _schemaQbchCalcOfAmpV3,
            _calcOfAmpConnectionPool,
            subjectIds,
            timeLeftMs ?? _calcOfAmpTimeout,
            "CalculationOfAmpV3");

        return ApplyFourDayWindowForContracts(xml);
    }

    /// <summary>
    /// Возвращает сведения о самозапрете по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с блоком самозапрета для прямого маппинга в ответ 3.0.</returns>
    public Task<XElement?> GetSelfProhibitionV3(List<long> subjectIds, long? timeLeftMs = null)
        => ExecuteXmlProcedureV3(
            _config.GetValue<string>("QbchSelfProhibitionV3:Procedures:GetSelfProhibition"),
            _schemaQbchSelfProhibitionV3,
            _selfProhibitionConnectionPool,
            subjectIds,
            timeLeftMs ?? _selfProhibitionTimeout,
            "GetSelfProhibitionV3");

    /// <summary>
    /// Возвращает антифрод-записи по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с антифрод-записями для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetAntifraudV3(List<long> subjectIds, long? timeLeftMs = null)
    {
        var xml = await ExecuteXmlProcedureV3(
            _config.GetValue<string>("QbchAntifraudV3:Procedures:GetAntifraud"),
            _schemaQbchAntifraudV3,
            _antifraudConnectionPool,
            subjectIds,
            timeLeftMs ?? _antifraudTimeout,
            "GetAntifraudV3");

        return SelectAntifraudFields(xml);
    }

    /// <summary>
    /// Возвращает признак наличия кредитной истории по списку субъектов.
    /// </summary>
    /// <param name="subjectIds">Идентификаторы субъектов.</param>
    /// <param name="timeLeftMs">Оставшееся время выполнения, мс.</param>
    /// <returns>XML с признаком наличия КИ для прямого маппинга в ответ 3.0.</returns>
    public async Task<XElement?> GetCreditHistoryPresenceFlagV3(List<long> subjectIds, long? timeLeftMs = null)
    {
        var xml = await ExecuteXmlProcedureV3(
            _config.GetValue<string>("QbchCreditHistoryPresenceFlagV3:Procedures:GetCreditHistoryPresenceFlag"),
            _schemaQbchCreditHistoryPresenceFlagV3,
            _creditHistoryPresenceFlagConnectionPool,
            subjectIds,
            timeLeftMs ?? _creditHistoryPresenceFlagTimeout,
            "GetCreditHistoryPresenceFlagV3");

        return NormalizeCreditHistoryPresenceFlag(xml);
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
        {
            return false;
        }

        var procName = _config.GetValue<string>("QbchDbV3:Procedures:IsPermissionGranted");
        if (string.IsNullOrWhiteSpace(procName))
        {
            return false;
        }

        var normalizedServiceName = NormalizeServiceNameForAccessCheck(serviceName);
        if (string.IsNullOrWhiteSpace(normalizedServiceName))
        {
            return false;
        }

        var sql = $"SELECT {_schemaQbchDbV3}.{procName}(@thumbprint, @serviceName)";
        var value = await ExecuteScalarAsync(sql, procName, _qbchDbConnectionPool, _qbchDbTimeout, cmd =>
        {
            cmd.Parameters.AddWithValue("thumbprint", thumbprint);
            cmd.Parameters.AddWithValue("serviceName", normalizedServiceName);
        }, "IsPermissionGrantedV3", ct);

        return value is bool boolValue && boolValue;
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

    private async Task<XElement?> ExecuteXmlProcedureV3(
        string? procName,
        string? schema,
        string[] connectionPool,
        List<long> subjectIds,
        long timeoutMs,
        string operationName)
    {
        if (string.IsNullOrWhiteSpace(procName) || string.IsNullOrWhiteSpace(schema) || subjectIds.Count == 0)
        {
            return null;
        }

        var sql = $"SELECT {schema}.{procName}(@subj_id)";
        var value = await ExecuteScalarAsync(sql, procName, connectionPool, timeoutMs, cmd =>
        {
            cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
        }, operationName);

        if (value is string xml && !string.IsNullOrWhiteSpace(xml))
        {
            return XElement.Parse(xml);
        }

        return null;
    }

    private static XElement? ApplyFourDayWindowForContracts(XElement? source)
    {
        if (source is null)
        {
            return null;
        }

        var minDate = DateTime.UtcNow.Date.AddDays(-4);
        var contracts = source.Descendants().Where(x => x.Name.LocalName == "Договор").ToList();

        foreach (var contract in contracts)
        {
            var terminationDateNode = contract.Elements().FirstOrDefault(x => x.Name.LocalName == "ДатаПрекращения");
            if (terminationDateNode is null || string.IsNullOrWhiteSpace(terminationDateNode.Value))
            {
                continue;
            }

            var rawTerminationDate = terminationDateNode.Value.Trim();
            if (!DateTime.TryParse(rawTerminationDate, out var terminationDate) || terminationDate.Date >= minDate)
            {
                continue;
            }

            contract.Remove();
        }

        return source;
    }

    private static XElement? SelectAntifraudFields(XElement? source)
    {
        if (source is null)
        {
            return null;
        }

        var result = new XElement(source.Name.Namespace + "Антифрод");
        var applications = source.Descendants().Where(x => x.Name.LocalName == "ОбращениеОбязательство");

        foreach (var app in applications)
        {
            var row = new XElement(source.Name.Namespace + "ОбращениеОбязательство");
            AddFieldIfExists(app, row, "КодИсточника");
            AddFieldIfExists(app, row, "СтадияРассмотрения");
            AddFieldIfExists(app, row, "ДатаСтадии");
            AddFieldIfExists(app, row, "СуммаЗайма");
            AddAllFieldsIfExists(app, row, "ПричинаОтказа");
            AddFieldIfExists(app, row, "УИД");
            result.Add(row);
        }

        return result;
    }

    private static void AddFieldIfExists(XElement source, XElement target, string fieldName)
    {
        var node = source.Elements().FirstOrDefault(x => x.Name.LocalName == fieldName);
        if (node is not null)
        {
            target.Add(new XElement(target.Name.Namespace + fieldName, node.Value));
        }
    }

    private static void AddAllFieldsIfExists(XElement source, XElement target, string fieldName)
    {
        var nodes = source.Elements().Where(x => x.Name.LocalName == fieldName).ToList();
        if (nodes.Count == 0)
        {
            return;
        }

        foreach (var node in nodes)
        {
            target.Add(new XElement(target.Name.Namespace + fieldName, node.Value));
        }
    }

    private static XElement? NormalizeCreditHistoryPresenceFlag(XElement? source)
    {
        if (source is null)
        {
            return null;
        }

        var node = source.Descendants().FirstOrDefault(x => x.Name.LocalName is "ПризнакНаличияКИ" or "CreditHistoryPresenceFlag");
        if (node is null || string.IsNullOrWhiteSpace(node.Value))
        {
            return null;
        }

        var value = node.Value.Trim();
        var normalizedValue = value;
        if (bool.TryParse(value, out var boolValue))
        {
            normalizedValue = boolValue ? "1" : "0";
        }
        else
        {
            normalizedValue = value.ToLowerInvariant() switch
            {
                "item1" => "1",
                "item0" => "0",
                "1" => "1",
                "0" => "0",
                _ => value
            };
        }

        var ns = source.Name.Namespace;
        return new XElement(ns + "СведенияКИ",
            new XElement(ns + "ПризнакНаличияКИ", normalizedValue));
    }

    private static string? NormalizeServiceNameForAccessCheck(string serviceName)
    {
        var normalized = serviceName.Split('?', 2)[0].Trim().Trim('/').ToLowerInvariant();
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
                    await Task.Delay(_dbConnectDelayMs, cts.Token);
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
                    await Task.Delay(_dbConnectDelayMs, cts.Token);
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
                    await Task.Delay(_dbConnectDelayMs, cts.Token);
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