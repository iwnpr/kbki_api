namespace QBCH_backup_tool;

/// <summary>
/// Куда доставлять данные из backup-записи.
/// </summary>
public enum RecoveryTarget
{
    /// <summary>
    /// Решение по состоянию Redis: данных нет — записать их и отправить уведомление в Kafka;
    /// данные есть — отправить в Kafka только ключ.
    /// </summary>
    Auto,

    /// <summary>Только записать данные в Redis (принудительно, без проверки).</summary>
    Redis,

    /// <summary>Только отправить уведомление в Kafka (принудительно, без проверки).</summary>
    Kafka
}

/// <summary>
/// Разобранные аргументы командной строки консольной утилиты.
/// </summary>
public sealed class CliOptions
{
    /// <summary>Каталог с backup-файлами.</summary>
    public string? BackupDirectory { get; private set; }

    /// <summary>Явно указанные файлы (обрабатываются вместо сканирования каталога).</summary>
    public List<string> Files { get; } = new();

    /// <summary>Произвольные переопределения конфигурации (ключ=значение).</summary>
    public Dictionary<string, string?> ConfigOverrides { get; } = new();

    /// <summary>Имя окружения для appsettings.{env}.json.</summary>
    public string? Environment { get; private set; }

    /// <summary>Куда доставлять данные.</summary>
    public RecoveryTarget? Target { get; private set; }

    /// <summary>Имя сервиса (redis-scope / префикс ключа).</summary>
    public string? ServiceName { get; private set; }

    /// <summary>Ничего не отправлять и не удалять — только показать, что было бы сделано.</summary>
    public bool DryRun { get; private set; }

    /// <summary>Не удалять backup-файл даже при успешной отправке.</summary>
    public bool KeepFiles { get; private set; }

    /// <summary>Остановить обработку при первой ошибке (по умолчанию — продолжать).</summary>
    public bool StopOnError { get; private set; }

    /// <summary>Включить подробный (Debug) вывод в консоль.</summary>
    public bool Verbose { get; private set; }

    /// <summary>Запрошена справка.</summary>
    public bool ShowHelp { get; private set; }

    /// <summary>
    /// Разбирает аргументы командной строки. Кидает <see cref="ArgumentException"/> при неверных аргументах.
    /// </summary>
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                case "-?":
                case "/?":
                    options.ShowHelp = true;
                    break;

                case "-d":
                case "--backup-dir":
                    options.BackupDirectory = RequireValue(args, ref i, arg);
                    break;

                case "-f":
                case "--file":
                    options.Files.Add(RequireValue(args, ref i, arg));
                    break;

                case "-e":
                case "--environment":
                    options.Environment = RequireValue(args, ref i, arg);
                    break;

                case "-t":
                case "--target":
                    options.Target = ParseTarget(RequireValue(args, ref i, arg));
                    break;

                case "--service-name":
                    options.ServiceName = RequireValue(args, ref i, arg);
                    break;

                case "-D":
                case "--define":
                    AddOverride(options, RequireValue(args, ref i, arg));
                    break;

                case "-n":
                case "--dry-run":
                    options.DryRun = true;
                    break;

                case "--keep":
                    options.KeepFiles = true;
                    break;

                case "--stop-on-error":
                    options.StopOnError = true;
                    break;

                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;

                default:
                    throw new ArgumentException($"Неизвестный аргумент: '{arg}'. Запустите с --help для справки.");
            }
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int i, string arg)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Для аргумента '{arg}' не указано значение.");
        return args[++i];
    }

    private static RecoveryTarget ParseTarget(string value) => value.Trim().ToLowerInvariant() switch
    {
        "auto" or "both" or "all" => RecoveryTarget.Auto,
        "redis" => RecoveryTarget.Redis,
        "kafka" => RecoveryTarget.Kafka,
        _ => throw new ArgumentException($"Недопустимое значение --target: '{value}'. Допустимо: auto | redis | kafka.")
    };

    private static void AddOverride(CliOptions options, string keyValue)
    {
        var idx = keyValue.IndexOf('=');
        if (idx <= 0)
            throw new ArgumentException($"Переопределение конфигурации должно быть в формате Ключ=Значение, получено: '{keyValue}'.");
        var key = keyValue[..idx].Trim();
        var value = keyValue[(idx + 1)..];
        options.ConfigOverrides[key] = value;
    }

    /// <summary>Текст справки / инструкция по использованию.</summary>
    public const string HelpText = """
        qbch-backup-tool — консольная утилита восстановления данных fallback-сценария QBCH.

        НАЗНАЧЕНИЕ
          Если QBCH_api не смог сохранить результат обработки, он записывает данные в
          backup-файл (backup/{RequestId}.json). Эта утилита работает в фоне: с заданным
          интервалом просматривает каталог backup и по состоянию Redis определяет, что
          именно нужно долить:
            - данных в Redis нет  => упал Redis: пишет данные в Redis и ключ в Kafka;
            - данные в Redis есть => упала Kafka: отправляет в Kafka только ключ.
          При успехе обработанный файл удаляется, при ошибке остаётся до следующего прохода.
          Работает бесконечно; остановка — средствами хостинга (служба, systemd, kill).

        ИСПОЛЬЗОВАНИЕ
          qbch-backup-tool [опции]

        ОПЦИИ
          -d, --backup-dir <путь>   Каталог с backup-файлами (по умолчанию: backup).
          -f, --file <путь>         Обработать конкретный файл. Можно указывать несколько раз.
                                    Если задано — каталог не сканируется.
          -t, --target <цель>       auto   — решать по состоянию Redis (по умолчанию);
                                    redis  — принудительно записать только в Redis;
                                    kafka  — принудительно отправить только ключ в Kafka.
          --service-name <имя>      Имя сервиса / redis-scope (по умолчанию: dlrequest).
          -e, --environment <env>   Окружение для appsettings.{env}.json.
          -D, --define <Ключ=Знач>  Переопределить параметр конфигурации.
                                    Напр.: -D ConnectionStrings:Redis=host:6379,...
          -n, --dry-run             Ничего не отправлять и не удалять — только показать план.
          --keep                    Не удалять backup-файлы даже при успешной отправке.
          --stop-on-error           Остановиться на первой ошибке (по умолчанию — продолжать).
          -v, --verbose             Подробный (Debug) вывод в консоль.
          -h, --help                Показать эту справку.

        КОНФИГУРАЦИЯ (порядок применения, каждый следующий переопределяет предыдущий)
          1. appsettings.json рядом с утилитой — основной и единственный файл настроек.
          2. appsettings.{environment}.json рядом с утилитой (если указано -e/--environment).
          3. Переменные окружения.
          4. Переопределения -D/--define.

          Утилита автономна: настройки берутся ТОЛЬКО из её собственных файлов,
          конфигурация веб-приложения не читается.

          Параметры подключения (заполняются в appsettings.json утилиты):
            ConnectionStrings:Redis        строка подключения StackExchange.Redis
            RedisCache:DBIndex             индекс БД Redis (по умолчанию 0)
            KafkaService:BootstrapServers  адрес брокера Kafka
            KafkaService:Topic             топик уведомлений
            BackupTool:BackupDirectory     каталог backup приложения
            BackupTool:IntervalSeconds     период проверки каталога (по умолчанию 60)

        КОДЫ ВОЗВРАТА
          0  показана справка
          2  ошибка запуска / конфигурации
          (при штатной работе утилита не завершается)

        ПРИМЕРЫ
          # Штатный запуск в фоне (настройки — из appsettings.json рядом с утилитой):
          qbch-backup-tool

          # Посмотреть, что будет сделано, без отправки и удаления:
          qbch-backup-tool --dry-run --verbose

          # Настройки для конкретного окружения (appsettings.Production.json утилиты):
          qbch-backup-tool -e Production

          # Восстановить один файл принудительно только в Redis:
          qbch-backup-tool -t redis -f backup/8f1c...json

          # Разовое переопределение подключения без правки файла:
          qbch-backup-tool -D ConnectionStrings:Redis="10.10.100.84:6379,password=***,abortConnect=false" \
                           -D KafkaService:BootstrapServers=10.10.100.71:9092 \
                           -D KafkaService:Topic=RedisMessagesTopicv2
        """;
}


