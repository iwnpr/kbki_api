using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Application_lib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Adapters_lib.DB
{
    /// <summary>
    /// 
    /// </summary>
    public class DBAdapter : IDBAdapter
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DBAdapter> _logger;
        private readonly IRedisAdapter _cacheService;

        private readonly string[] _QBCHDB_ConnectionPool;
        private readonly string[] _CalcOfAmp_ConnectionPool;
        private readonly string[] _DlputDB_ConnectionPool;
        private readonly string[] _SearchSubjects_ConnectionPool;
        private readonly string[] _SelfProhibition_ConnectionPool;

        private readonly int _QBCHDB_Timeout;
        private readonly int _CalcOfAmp_Timeout;
        private readonly int _SearchSubjects_Timeout;
        private readonly int _SearchSubjects_TotalTimeout;
        private readonly int _SelfProhibition_Timeout;

        private readonly string? _schema_QbchDb;
        private readonly string? _schema_QbchSearchSubjects;
        private readonly string? _schema_QbchCalcOfAmp;
        private readonly string? _schema_QbchSelfProhibition;

        private readonly long _permissionsLifeTime;
        private readonly long _requisitesLifeTime;
        private readonly int _DBConnectDelayMs;

        /// <summary>
        /// Получить подключение согласно итератору
        /// </summary>
        /// <param name="i">Итератор</param>
        /// <param name="connectionPool"></param>
        /// <param name="main">Основное</param>
        /// <param name="additional">Дополнительное</param>
        /// <returns></returns>
        private static NpgsqlConnection GetDbConnection(int i, string[] connectionPool)
        {
            return new NpgsqlConnection(connectionPool[i]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="cacheService"></param>
        public DBAdapter(IConfiguration config, ILogger<DBAdapter> logger, IRedisAdapter cacheService)
        {
            _config = config;
            _logger = logger;
            _cacheService = cacheService;

            // Пул подлючений к БД
            _QBCHDB_ConnectionPool = config.GetSection("ConnectionPool:QbchDb").Get<string[]>();
            _CalcOfAmp_ConnectionPool = config.GetSection("ConnectionPool:QbchCalcOfAmp").Get<string[]>();
            _DlputDB_ConnectionPool = config.GetSection("ConnectionPool:DlputDB").Get<string[]>();
            _SearchSubjects_ConnectionPool = config.GetSection("ConnectionPool:QbchSearchSubjects").Get<string[]>();
            _SelfProhibition_ConnectionPool = config.GetSection("ConnectionPool:QbchSelfProhibition").Get<string[]>();

            // Таймауты
            _QBCHDB_Timeout = config.GetValue<int>("APIConfiguration:QbchDBreconnectCancelTimeoutMs");
            _CalcOfAmp_Timeout = config.GetValue<int>("APIConfiguration:QbchCalcOfAmpCancelTimeoutMs");
            _SearchSubjects_Timeout = config.GetValue<int>("APIConfiguration:SearchSubjectsCancelTimeoutMs");
            _SearchSubjects_TotalTimeout = config.GetValue<int>("APIConfiguration:SearchSubjectsTotalCancelTimeoutMs");
            _SelfProhibition_Timeout = config.GetValue<int>("APIConfiguration:SelfProhibitionCancelTimeoutMs");

            _schema_QbchDb = _config.GetValue<string>("QbchDb:Schema");
            _schema_QbchSearchSubjects = _config.GetValue<string>("QbchSearchSubjects:Schema");
            _schema_QbchCalcOfAmp = _config.GetValue<string>("QbchCalcOfAmp:Schema");
            _schema_QbchSelfProhibition = _config.GetValue<string>("QbchSelfProhibition:Schema");

            _permissionsLifeTime = _config.GetValue<long>("Redis:PermissionsLifeTimeMinutes");
            _requisitesLifeTime = _config.GetValue<long>("Redis:RequisitesLifeTimeMinutes");
            _DBConnectDelayMs = _config.GetValue<int>("APIConfiguration:DBConnectDelayMs");
        }



        /// <summary>
        /// Запрос хидов из БД
        /// </summary>
        /// <param name="request">Запрос в формате xml</param>
        /// <param name="timeLeftMs"></param>
        /// <returns>Список хидов</returns>
        public async Task<List<long>> GetSearchAllSubjects(string request, long? timeLeftMs = null)
        {
            var result = new List<long>();

            if (string.IsNullOrEmpty(request))
                return result;

            var _procname = _config.GetValue<string>("QbchSearchSubjects:Procedures:SearchAllSubjects");
            string pgcmd = $"SELECT {_schema_QbchSearchSubjects}.{_procname}(@request)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds((timeLeftMs ?? _SearchSubjects_Timeout) - 1000));
            var ctsTotal = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeLeftMs ?? _SearchSubjects_Timeout));

            while (!ctsTotal.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection SearchAllSubjects retry");

                if (!cts.Token.IsCancellationRequested)
                {
                    _logger.LogDebug(" | First db connection");
                    using var connection = GetDbConnection(0, _SearchSubjects_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(cts.Token);

                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.AddWithValue("request", NpgsqlDbType.Xml, request);
                        using var reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync(cts.Token))
                        {
                            result = (await reader.IsDBNullAsync(reader.GetOrdinal(_procname)) ? null : reader.GetFieldValue<List<long>>(reader.GetOrdinal(_procname))) ?? new();   //GetString();
                        }

                        ctsTotal.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры SearchAllSubjects.");
                        await Task.Delay(_DBConnectDelayMs);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
                else
                {
                    if (ctsTotal.Token.IsCancellationRequested || _SearchSubjects_ConnectionPool.Length == 1)
                    {
                        _logger.LogDebug(" | ctsTotal db connection timeout");
                        break;
                    }

                    _logger.LogDebug(" | Second db connection");
                    for (int i = 1; i < _SearchSubjects_ConnectionPool.Length; i++)
                    {
                        using var connection = GetDbConnection(i, _SearchSubjects_ConnectionPool);
                        try
                        {
                            if (ctsTotal.Token.IsCancellationRequested)
                            {
                                _logger.LogDebug(" | ctsTotal db connection timeout");
                                break;
                            }

                            _logger.LogDebug(" | Connection string {i} from pool", i);

                            await connection.OpenAsync(ctsTotal.Token);
                            using var cmd = new NpgsqlCommand(pgcmd, connection);
                            cmd.Parameters.AddWithValue("request", NpgsqlDbType.Xml, request);
                            using var reader = await cmd.ExecuteReaderAsync();

                            while (await reader.ReadAsync())
                            {
                                result = (await reader.IsDBNullAsync(reader.GetOrdinal(_procname)) ? null : reader.GetFieldValue<List<long>>(reader.GetOrdinal(_procname))) ?? new();   //GetString();
                            }

                            ctsTotal.Cancel();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "Ошибка процедуры SearchAllSubjects.");
                            await Task.Delay(_DBConnectDelayMs);
                        }
                        finally
                        {
                            if (connection.State != ConnectionState.Closed)
                                await connection.CloseAsync();
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получить ССП по спсику хидов
        /// </summary>
        /// <param name="subjectIds">Хиды субъектов</param>
        /// <param name="timeLeftMs"></param>
        /// <returns>XElement с данными о договорах</returns>
        public async Task<XElement?> GetCalculationOfAmp(List<long> subjectIds, long? timeLeftMs = null)
        {
            XElement? result = null;

            var _procname = _config.GetValue<string>("QbchCalcOfAmp:Procedures:CalculationOfAmp");
            var pgcmd = $"SELECT {_schema_QbchCalcOfAmp}.{_procname}(@subj_id)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeLeftMs ?? _CalcOfAmp_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection CalculationOfAmp retry");

                for (int i = 0; i < _CalcOfAmp_ConnectionPool.Length; i++)
                {
                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _CalcOfAmp_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(cts.Token);
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
                        using var reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            result = XElement.Parse(await reader.IsDBNullAsync(reader.GetOrdinal(_procname)) ? string.Empty : reader.GetString(reader.GetOrdinal(_procname)));
                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры CalculationOfAmp.");
                        await Task.Delay(_DBConnectDelayMs);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Запрос сведений о самозапрете
        /// </summary>
        /// <param name="subjectIds">Идентификаторы субъекта</param>
        /// <param name="timeLeftMs"></param>
        /// <returns>XElement</returns>
        public async Task<XElement?> GetSelfProhibition(List<long> subjectIds, long? timeLeftMs = null)
        {
            XElement? result = null;

            var _procname = _config.GetValue<string>("QbchSelfProhibition:Procedures:GetSelfProhibition");
            var pgcmd = $"SELECT {_schema_QbchSelfProhibition}.{_procname}(@subj_id)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeLeftMs ?? _SelfProhibition_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection CalculationOfAmp retry");

                for (int i = 0; i < _SelfProhibition_ConnectionPool.Length; i++)
                {
                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _CalcOfAmp_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(cts.Token);
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.AddWithValue("subj_id", NpgsqlDbType.Array | NpgsqlDbType.Bigint, subjectIds);
                        using var reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            if (!await reader.IsDBNullAsync(reader.GetOrdinal(_procname)))
                                result = XElement.Parse(reader.GetString(reader.GetOrdinal(_procname)));

                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры GetSelfProhibition.");
                        await Task.Delay(_DBConnectDelayMs);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            return result;
        }



        /// <summary>
        /// Получить ИНН ОГРН по отпечатку сертификата
        /// </summary>
        /// <param name="thumbprint">Отпечаток</param>
        /// <returns>XElement ИНН ОГРН</returns>
        public async Task<XElement?> GetInnOgrnByThumbprint(string? thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
                return null;

            XElement? result = null;

            try
            {
                _cacheService.TryGetHashValue("requisites", thumbprint, "RequisitesXML", out var value);

                if (value.HasValue && value.Value.HasValue)
                    return XElement.Parse(value.Value);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При проверке кэша реквизитов из redis возникла ошибка.");
            }

            var _procname = _config.GetValue<string>("QbchDb:Procedures:GetInnOgrnByThumbprint");
            string pgcmd = $"SELECT {_schema_QbchDb}.{_procname}(@thumbprint)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection GetInnOgrnByThumbprint retry");

                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(cts.Token);
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.Add(new("thumbprint", thumbprint));
                        using var reader = await cmd.ExecuteReaderAsync(cts.Token);

                        while (await reader.ReadAsync())
                        {
                            result = XElement.Parse(await reader.IsDBNullAsync(reader.GetOrdinal(_procname)) ? string.Empty : reader.GetString(reader.GetOrdinal(_procname)));
                            break;
                        }

                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры GetInnOgrnByThumbprint.");
                        await Task.Delay(_DBConnectDelayMs);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            try
            {
                if (result is not null)
                {
                    await _cacheService.AddHash("requisites", thumbprint, "RequisitesXML", result.ToString());
                    await _cacheService.TrySetKeyExpiration("requisites", thumbprint, _requisitesLifeTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При установке кэша реквизитов в redis возникла ошибка.");
            }

            return result;
        }

        /// <summary>
        /// Проверка прав доступа
        /// </summary>
        /// <param name="thumbprint">Отпечаток</param>
        /// <param name="serviceName">Имя сервиса</param>
        /// <returns></returns>
        public async Task<bool> IsPermissionGrantedv2(string? thumbprint, string? serviceName, CancellationToken? ct = null)
        {
            bool result = false;
            bool redisError = false;
            bool dbError = true;

            if (thumbprint is null || serviceName is null)
                return result;

            try
            {
                _cacheService.TryGetHashValue("permissionsv2", thumbprint, serviceName, out var value);

                if (value.HasValue && value.Value.HasValue)
                    return bool.TryParse(value.Value, out result) ? result : result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При проверке кэша прав из redis возникла ошибка.");
                redisError = true;
            }

            var _procname = _config.GetValue<string>("QbchDb:Procedures:IsPermissionGranted");
            string pgcmd = $"SELECT {_schema_QbchDb}.{_procname}(@thumbprint,@serviceName)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection IsPermissionGranted retry");

                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(ct.GetValueOrDefault(cts.Token));
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.Add(new("thumbprint", thumbprint));
                        cmd.Parameters.Add(new("serviceName", serviceName));
                        using var reader = cmd.ExecuteReader();

                        while (await reader.ReadAsync(ct.GetValueOrDefault(cts.Token)))
                        {
                            result = !await reader.IsDBNullAsync(reader.GetOrdinal(_procname)) && reader.GetBoolean(reader.GetOrdinal(_procname));
                            dbError = false;
                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры IsPermissionGranted.");
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            if (dbError)
                throw new Exception("Alarm trigger");

            try
            {
                if (!redisError)
                    Task.Run(async () =>
                    {
                        // 2 Секунды
                        await _cacheService.AddHash("permissionsv2", thumbprint, serviceName, result.ToString(), ct: ct);
                        await _cacheService.TrySetKeyExpiration("permissionsv2", thumbprint, _permissionsLifeTime, ct: ct);
                    }).Wait(TimeSpan.FromMilliseconds(1500));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При установке кэша прав в redis возникла ошибка.");
            }

            return result;
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="psrn"></param>
        /// <returns></returns>
        public async Task<int?> GetAbonentKeyIdByPSRN(string? psrn)
        {
            int? result = null;

            if (string.IsNullOrWhiteSpace(psrn))
                return result;

            // Зпапрос кэшированных данных в Redis
            try
            {
                _cacheService.TryGetHashValue("abonentId", psrn, "KeyId", out var value);

                if (value.HasValue && value.Value.HasValue)
                    return int.Parse(value.Value);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При проверке кэша abonentId в Redis, возникла ошибка.");
            }

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            // Запрос в БД
            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection for tr_abonents retry");

                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug(" | cts tr_abonents");
                        break;
                    }

                    _logger.LogDebug(" | Connection string {i} from pool", i);
                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync();
                        string pgcmd = $"select key_id from {_schema_QbchDb}.tr_abonents ta where ogrn = @psrn limit 1";

                        using var cmd = new NpgsqlCommand(pgcmd, connection);

                        cmd.Parameters.Add(new("psrn", psrn));
                        using var reader = cmd.ExecuteReader();

                        while (await reader.ReadAsync())
                        {
                            result = (int)reader.GetValue(0);
                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка запроса tr_abonents where ogrn == {ogrn}.", psrn);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            // Пишем кэш в Redis
            try
            {
                if (result is not null)
                {
                    await _cacheService.AddHash("abonentId", psrn, "KeyId", result.ToString());
                    await _cacheService.TrySetKeyExpiration("abonentId", psrn, _requisitesLifeTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При установке кэша реквизитов в redis возникла ошибка.");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="abonent"></param>
        /// <param name="thumbprint"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        public async Task<bool> IsCertExist(byte[] cert)
        {
            bool result = false;
            X509Certificate2 certificate = new(cert);

            string pgcmd = $"SELECT EXISTS(SELECT 1 FROM {_schema_QbchDb}.tr_abonent_certificates WHERE UPPER(thumbprint)=UPPER(@thumbprint));";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection IsCertExist retry");
                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug(" | cts IsCertExist");
                        break;
                    }

                    _logger.LogDebug(" | Connection string {i} from pool", i);
                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync();
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.Add(new("thumbprint", certificate.Thumbprint));

                        using var reader = cmd.ExecuteReader();

                        while (await reader.ReadAsync())
                        {
                            result = (bool)reader.GetValue(0);
                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка запроса tr_abonent_certificates where thumb == {thumb}.", certificate.Thumbprint);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="abonentId"></param>
        /// <param name="thumbprint"></param>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        public async Task<bool> AddCertificate(int abonentId, string thumbprint, DateTime expirationDate)
        {
            bool result = false;
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection AddCertificate retry");
                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug(" | cts AddCertificate");
                        break;
                    }

                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    await connection.OpenAsync();
                    string pgcmd = $"insert into {_schema_QbchDb}.tr_abonent_certificates(abonent_key_id, thumbprint, expiration_date, is_active) values (@abonentId, @thumbprint, @expirationDate, true)";
                    using var cmd = new NpgsqlCommand(pgcmd, connection);

                    cmd.Parameters.Add(new("abonentId", abonentId));
                    cmd.Parameters.Add(new("thumbprint", thumbprint));
                    cmd.Parameters.Add(new("expirationDate", expirationDate));
                    using var reader = cmd.ExecuteReader();

                    await reader.ReadAsync();
                    result = reader.RecordsAffected != 0;

                    if (connection.State != ConnectionState.Closed)
                        await connection.CloseAsync();
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        public async Task<bool> SetCertificateInactive(string thumbprint)
        {
            bool result = false;
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_QBCHDB_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection tr_abonent_certificates SET retry");
                for (int i = 0; i < _QBCHDB_ConnectionPool.Length; i++)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogDebug(" | cts tr_abonent_certificates SET");
                        break;
                    }

                    _logger.LogDebug(" | Connection string {i} from pool", i);
                    using var connection = GetDbConnection(i, _QBCHDB_ConnectionPool);
                    await connection.OpenAsync();

                    string pgcmd = $"UPDATE {_schema_QbchDb}.tr_abonent_certificates SET is_active=false WHERE UPPER(thumbprint)=UPPER(@thumbprint);";
                    using var cmd = new NpgsqlCommand(pgcmd, connection);

                    cmd.Parameters.Add(new("thumbprint", thumbprint));
                    using var reader = cmd.ExecuteReader();

                    await reader.ReadAsync();
                    result = reader.Rows != 0;

                    if (connection.State != ConnectionState.Closed)
                        await connection.CloseAsync();
                }
            }

            return result;
        }

        public async Task<XElement?> GetDlputData(string xml, long? timeLeft)
        {
            XElement? result = null;

            if (!_config.GetValue<bool>("APIConfiguration:UseDlput"))
                return result;

            var  schema = _config.GetValue<string>("DlputDB:Schema");
            var funcName = _config.GetValue<string>("DlputDB:Procedures:CalculationOfAmpDlput");

            var pgcmd = $"SELECT {schema}.{funcName}(@request)";
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeLeft ?? _CalcOfAmp_Timeout));

            while (!cts.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Loop connection DlputDB retry");

                for (int i = 0; i < _CalcOfAmp_ConnectionPool.Length; i++)
                {
                    _logger.LogDebug(" | Connection string {i} from pool", i);

                    using var connection = GetDbConnection(i, _CalcOfAmp_ConnectionPool);
                    try
                    {
                        await connection.OpenAsync(cts.Token);
                        using var cmd = new NpgsqlCommand(pgcmd, connection);
                        cmd.Parameters.AddWithValue("request", NpgsqlDbType.Xml, xml);
                        using var reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            result = XElement.Parse(await reader.IsDBNullAsync(reader.GetOrdinal(funcName)) ? string.Empty : reader.GetString(reader.GetOrdinal(funcName)));
                            break;
                        }

                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Ошибка процедуры DlputDB.");
                        await Task.Delay(_DBConnectDelayMs);
                    }
                    finally
                    {
                        if (connection.State != ConnectionState.Closed)
                            await connection.CloseAsync();
                    }
                }
            }

            return result;
        }
    }
}
