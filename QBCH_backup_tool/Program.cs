using Cache_lib.Implementations;
using KafkaService_lib.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_backup_tool;
using QBCH_backup_tool.Services;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using StackExchange.Redis;

// Коды возврата: 0 — успех, 1 — часть записей не обработана, 2 — ошибка запуска/конфигурации.
const int ExitOk = 0;
const int ExitPartial = 1;
const int ExitFatal = 2;

CliOptions options;
try
{
    options = CliOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return ExitFatal;
}

if (options.ShowHelp)
{
    Console.WriteLine(CliOptions.HelpText);
    return ExitOk;
}

// --- Логирование (Serilog: консоль + файл) ---
// Конфигурация логирования берётся ТОЛЬКО из appsettings.json самой утилиты,
// без файлов --config: иначе секция Serilog от API (со стойками Elasticsearch и т.п.,
// которых нет в этой утилите) сломала бы инициализацию логгера.
var loggingConfiguration = BuildConfiguration(options, includeExternalFiles: false);

var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(loggingConfiguration);

// Страховка: если секция Serilog отсутствует (нет appsettings.json), всё равно пишем в консоль,
// чтобы утилита не оставалась «немой» во время инцидента.
var hasConfiguredSinks = loggingConfiguration.GetSection("Serilog:WriteTo").GetChildren().Any();
if (!hasConfiguredSinks)
{
    loggerConfig
        .MinimumLevel.Is(options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information)
        .WriteTo.Console();
}

Log.Logger = loggerConfig.CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: false);
var appLogger = loggerFactory.CreateLogger("QBCH_backup_tool");

try
{
    IConfigurationRoot configuration;
    try
    {
        configuration = BuildConfiguration(options, includeExternalFiles: true);
    }
    catch (Exception ex)
    {
        appLogger.LogCritical(ex, "Ошибка загрузки конфигурации.");
        return ExitFatal;
    }
    // --- Настройки запуска ---
    var settings = BuildSettings(options, configuration);

    var redisConnectionString = configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redisConnectionString) &&
        settings.Target is RecoveryTarget.Auto or RecoveryTarget.Redis)
    {
        appLogger.LogCritical(
            "Не задана строка подключения к Redis (ConnectionStrings:Redis). " +
            "Укажите её через --config <appsettings> или -D ConnectionStrings:Redis=...");
        return ExitFatal;
    }

    // --- Подключение к Redis (в режиме Auto нужно и для проверки состояния записи) ---
    ConnectionMultiplexer? multiplexer = null;
    Cache_lib.Interfaces.IKeyValueStorageService redis;
    if (settings.Target is RecoveryTarget.Auto or RecoveryTarget.Redis)
    {
        try
        {
            multiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString!);
        }
        catch (Exception ex)
        {
            appLogger.LogCritical(ex, "Не удалось подключиться к Redis по строке подключения.");
            return ExitFatal;
        }
        redis = new KeyValueStorageService(configuration, loggerFactory.CreateLogger<KeyValueStorageService>(), multiplexer);
    }
    else
    {
        redis = new NullKeyValueStorageService();
    }

    // --- Kafka ---
    var kafka = new KafkaService(
        loggerFactory.CreateLogger<KafkaService>(),
        configuration,
        new CompressService());

    // --- Восстановление ---
    var service = new BackupRecoveryService(
        loggerFactory.CreateLogger<BackupRecoveryService>(),
        redis,
        kafka);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        appLogger.LogWarning("Получен сигнал прерывания — завершаем текущую запись и останавливаемся.");
        cts.Cancel();
    };

    RecoverySummary summary;
    try
    {
        summary = await service.RunAsync(settings, cts.Token);
    }
    catch (OperationCanceledException)
    {
        appLogger.LogWarning("Обработка прервана пользователем.");
        return ExitPartial;
    }
    finally
    {
        multiplexer?.Dispose();
    }

    appLogger.LogInformation(
        "Итог: всего {total}, восстановлено {recovered}, пропущено {skipped}, ошибок {failed}.",
        summary.Total, summary.Recovered, summary.Skipped, summary.Failed);

    return summary.Failed > 0 ? ExitPartial : ExitOk;
}
catch (Exception ex)
{
    appLogger.LogCritical(ex, "Непредвиденная ошибка при выполнении утилиты.");
    return ExitFatal;
}
finally
{
    Log.CloseAndFlush();
}

// ---------- Локальные функции ----------

// includeExternalFiles=false собирает конфигурацию только из appsettings утилиты
// (используется для логирования); =true добавляет файлы --config (используется для подключений).
static IConfigurationRoot BuildConfiguration(CliOptions options, bool includeExternalFiles)
{
    var environment = options.Environment
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    var builder = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

    if (!string.IsNullOrWhiteSpace(environment))
        builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

    // Внешние файлы конфигурации (например, appsettings API) — по указанным путям.
    if (includeExternalFiles)
    {
        foreach (var configFile in options.ConfigFiles)
        {
            var fullPath = Path.GetFullPath(configFile);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Файл конфигурации не найден: {fullPath}");
            builder.AddJsonFile(fullPath, optional: false, reloadOnChange: false);
        }
    }

    builder.AddEnvironmentVariables();

    // Переопределение уровня консоли для --verbose (Console — первый sink в appsettings.json).
    var inMemory = new Dictionary<string, string?>(options.ConfigOverrides);
    if (options.Verbose)
        inMemory["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"] = "Debug";

    if (inMemory.Count > 0)
        builder.AddInMemoryCollection(inMemory);

    return builder.Build();
}

static RecoverySettings BuildSettings(CliOptions options, IConfiguration configuration)
{
    var backupDir = options.BackupDirectory
                    ?? configuration.GetValue<string?>("BackupTool:BackupDirectory")
                    ?? "backup";

    var target = options.Target
                 ?? ParseTargetFromConfig(configuration.GetValue<string?>("BackupTool:Target"))
                 ?? RecoveryTarget.Auto;

    var serviceName = options.ServiceName
                      ?? configuration.GetValue<string?>("BackupTool:ServiceName")
                      ?? "dlrequest";

    return new RecoverySettings
    {
        BackupDirectory = backupDir,
        Files = options.Files,
        Target = target,
        ServiceName = serviceName,
        DryRun = options.DryRun,
        KeepFiles = options.KeepFiles,
        StopOnError = options.StopOnError
    };
}

static RecoveryTarget? ParseTargetFromConfig(string? value) => value?.Trim().ToLowerInvariant() switch
{
    null or "" => null,
    "auto" or "both" or "all" => RecoveryTarget.Auto,
    "redis" => RecoveryTarget.Redis,
    "kafka" => RecoveryTarget.Kafka,
    _ => null
};
