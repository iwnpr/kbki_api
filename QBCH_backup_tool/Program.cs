using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_backup_tool;
using QBCH_backup_tool.Services;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using StackExchange.Redis;

// Утилита работает бесконечно, поэтому штатного завершения нет.
// Коды возврата: 0 — показана справка, 2 — ошибка запуска/конфигурации.
const int ExitOk = 0;
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

// Вся конфигурация — из собственных файлов утилиты. Настройки веб-приложения не читаются:
// утилита запускается отдельно и не должна зависеть от его окружения.
IConfigurationRoot configuration;
try
{
    configuration = BuildConfiguration(options);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
    return ExitFatal;
}

// --- Логирование (Serilog: консоль + файл) ---
var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(configuration);

// Страховка: если секция Serilog отсутствует (нет appsettings.json), всё равно пишем в консоль,
// чтобы утилита не оставалась «немой» во время инцидента.
var hasConfiguredSinks = configuration.GetSection("Serilog:WriteTo").GetChildren().Any();
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
    // --- Настройки запуска ---
    var settings = BuildSettings(options, configuration);

    var redisConnectionString = configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redisConnectionString) &&
        settings.Target is RecoveryTarget.Auto or RecoveryTarget.Redis)
    {
        appLogger.LogCritical(
            "Не задана строка подключения к Redis (ConnectionStrings:Redis). " +
            "Заполните её в appsettings.json утилиты или передайте -D ConnectionStrings:Redis=...");
        return ExitFatal;
    }

    // --- Подключение к Redis (в режиме Auto нужно и для проверки состояния записи) ---
    ConnectionMultiplexer? multiplexer = null;
    RedisBackupStore? redis = null;
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

        redis = new RedisBackupStore(
            loggerFactory.CreateLogger<RedisBackupStore>(),
            multiplexer,
            configuration.GetValue<int?>("RedisCache:DBIndex") ?? 0);
    }

    // --- Kafka (только если она является целью: в режиме --target redis не нужна) ---
    KafkaNotifier? kafka = null;
    if (settings.Target is RecoveryTarget.Auto or RecoveryTarget.Kafka)
    {
        try
        {
            kafka = new KafkaNotifier(loggerFactory.CreateLogger<KafkaNotifier>(), configuration);
        }
        catch (Exception ex)
        {
            appLogger.LogCritical(ex, "Не удалось настроить продюсер Kafka.");
            multiplexer?.Dispose();
            return ExitFatal;
        }
    }

    // --- Восстановление ---
    var service = new BackupRecoveryService(
        loggerFactory.CreateLogger<BackupRecoveryService>(),
        redis,
        kafka);

    // --- Фоновый цикл ---
    // Утилита работает как служба: периодически просматривает каталог backup и доливает
    // найденное. Остановка — средствами хостинга (служба, systemd, kill), вручную ничем
    // не управляется.
    var interval = TimeSpan.FromSeconds(configuration.GetValue<int?>("BackupTool:IntervalSeconds") ?? 60);
    appLogger.LogInformation("Запуск в фоновом режиме, интервал проверки: {interval}.", interval);

    try
    {
        while (true)
        {
            var summary = await service.RunAsync(settings);

            // Пустые проходы не засоряют лог: итог пишется, только если что-то было.
            if (summary.Total > 0)
                appLogger.LogInformation(
                    "Итог прохода: всего {total}, восстановлено {recovered}, пропущено {skipped}, ошибок {failed}.",
                    summary.Total, summary.Recovered, summary.Skipped, summary.Failed);

            await Task.Delay(interval);
        }
    }
    finally
    {
        // Flush продюсера обязателен: иначе последние уведомления могут не уйти.
        kafka?.Dispose();
        multiplexer?.Dispose();
    }
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

// Конфигурация утилиты: только её собственные appsettings рядом с exe,
// переменные окружения и переопределения -D. Файлы веб-приложения не подключаются.
static IConfigurationRoot BuildConfiguration(CliOptions options)
{
    var environment = options.Environment
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    var builder = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

    if (!string.IsNullOrWhiteSpace(environment))
        builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

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
    // Путь к backup приводится к абсолютному так же, как в API: относительный
    // разворачивается от каталога утилиты, а не от текущего рабочего каталога процесса.
    var backupDir = ResolveBackupDirectory(
        options.BackupDirectory
        ?? configuration.GetValue<string?>("BackupTool:BackupDirectory"));

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

static string ResolveBackupDirectory(string? configured)
{
    var directory = string.IsNullOrWhiteSpace(configured) ? "backup" : configured;
    return Path.IsPathRooted(directory)
        ? directory
        : Path.Combine(AppContext.BaseDirectory, directory);
}

static RecoveryTarget? ParseTargetFromConfig(string? value) => value?.Trim().ToLowerInvariant() switch
{
    null or "" => null,
    "auto" or "both" or "all" => RecoveryTarget.Auto,
    "redis" => RecoveryTarget.Redis,
    "kafka" => RecoveryTarget.Kafka,
    _ => null
};


