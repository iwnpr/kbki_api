namespace QBCH_backup_tool;

/// <summary>
/// Куда доставлять данные из backup-записи.
/// </summary>
public enum RecoveryTarget
{
    /// <summary>Записать данные в Redis и отправить уведомление в Kafka (полное восстановление).</summary>
    Both,

    /// <summary>Только записать данные в Redis.</summary>
    Redis,

    /// <summary>Только отправить уведомление в Kafka.</summary>
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

    /// <summary>Дополнительные json-файлы конфигурации (например, appsettings API).</summary>
    public List<string> ConfigFiles { get; } = new();

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

                case "-c":
                case "--config":
                    options.ConfigFiles.Add(RequireValue(args, ref i, arg));
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
        "both" or "all" => RecoveryTarget.Both,
        "redis" => RecoveryTarget.Redis,
        "kafka" => RecoveryTarget.Kafka,
        _ => throw new ArgumentException($"Недопустимое значение --target: '{value}'. Допустимо: both | redis | kafka.")
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
          Если QBCH_api не смог сохранить результат обработки в Redis, он записывает
          данные в backup-файл (backup/{RequestId}.json). Эта утилита вычитывает такие
          файлы, повторно отправляет данные в Redis и уведомление в Kafka, и при успехе
          удаляет обработанный файл. Запускается вручную в случае инцидента.

        ИСПОЛЬЗОВАНИЕ
          qbch-backup-tool [опции]

        ОПЦИИ
          -d, --backup-dir <путь>   Каталог с backup-файлами (по умолчанию: backup).
          -f, --file <путь>         Обработать конкретный файл. Можно указывать несколько раз.
                                    Если задано — каталог не сканируется.
          -t, --target <цель>       Куда отправлять: both | redis | kafka (по умолчанию: both).
          --service-name <имя>      Имя сервиса / redis-scope (по умолчанию: dlrequest).
          -c, --config <путь>       Доп. json-файл конфигурации с настройками Redis/Kafka
                                    (например, appsettings.Production.json от API).
                                    Можно указывать несколько раз.
          -e, --environment <env>   Окружение для appsettings.{env}.json.
          -D, --define <Ключ=Знач>  Переопределить параметр конфигурации.
                                    Напр.: -D ConnectionStrings:Redis=host:6379,...
          -n, --dry-run             Ничего не отправлять и не удалять — только показать план.
          --keep                    Не удалять backup-файлы даже при успешной отправке.
          --stop-on-error           Остановиться на первой ошибке (по умолчанию — продолжать).
          -v, --verbose             Подробный (Debug) вывод в консоль.
          -h, --help                Показать эту справку.

        КОНФИГУРАЦИЯ (порядок применения, каждый следующий переопределяет предыдущий)
          1. appsettings.json рядом с утилитой (логирование Serilog, значения по умолчанию).
          2. appsettings.{environment}.json (если указано -e/--environment).
          3. Файлы, указанные через -c/--config.
          4. Переменные окружения.
          5. Переопределения -D/--define.

          Обязательные параметры подключения:
            ConnectionStrings:Redis        строка подключения StackExchange.Redis
            RedisCache:DBIndex             индекс БД Redis (по умолчанию 0)
            KafkaService:BootstrapServers  адрес брокера Kafka
            KafkaService:Topic             топик уведомлений
          (эти параметры совпадают с параметрами QBCH_api — проще всего передать
           appsettings нужного окружения через --config).

        КОДЫ ВОЗВРАТА
          0  все записи обработаны успешно (или обрабатывать нечего)
          1  одна или несколько записей не обработаны
          2  ошибка запуска / конфигурации

        ПРИМЕРЫ
          # Восстановить все записи, взяв настройки из appsettings API:
          qbch-backup-tool --config /opt/qbch_api/appsettings.Production.json

          # Посмотреть, что будет сделано, без отправки и удаления:
          qbch-backup-tool -c appsettings.Production.json --dry-run --verbose

          # Восстановить один файл только в Redis:
          qbch-backup-tool -c appsettings.Production.json -t redis -f backup/8f1c...json

          # Указать подключение к Redis напрямую:
          qbch-backup-tool -D ConnectionStrings:Redis="10.10.100.84:6379,password=***,abortConnect=false" \
                           -D KafkaService:BootstrapServers=10.10.100.71:9092 \
                           -D KafkaService:Topic=RedisMessagesTopicv2
        """;
}
