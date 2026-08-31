using System.Text;
using System.Text.Json;
using Cache_lib.Interfaces;
using Confluent.Kafka;
using KafkaService_lib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using QBCH_backup_tool.Models;

namespace QBCH_backup_tool.Services;

/// <summary>
/// Ядро утилиты: чтение backup-записей fallback-сценария, повторная отправка
/// данных в Redis и/или Kafka и удаление обработанных файлов при успехе.
/// </summary>
public sealed class BackupRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<BackupRecoveryService> _logger;
    private readonly IKeyValueStorageService _redis;
    private readonly IKafkaService _kafka;

    public BackupRecoveryService(
        ILogger<BackupRecoveryService> logger,
        IKeyValueStorageService redis,
        IKafkaService kafka)
    {
        _logger = logger;
        _redis = redis;
        _kafka = kafka;
    }

    /// <summary>
    /// Обрабатывает набор backup-файлов согласно настройкам.
    /// </summary>
    public async Task<RecoverySummary> RunAsync(RecoverySettings settings, CancellationToken ct = default)
    {
        var files = ResolveFiles(settings);
        var summary = new RecoverySummary { Total = files.Count };

        if (files.Count == 0)
        {
            _logger.LogInformation("Backup-файлы не найдены — обрабатывать нечего.");
            return summary;
        }

        _logger.LogInformation(
            "Найдено файлов: {count}. Цель: {target}. Сервис: {service}. Режим: {mode}.",
            files.Count, settings.Target, settings.ServiceName, settings.DryRun ? "DRY-RUN" : "БОЕВОЙ");

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            var outcome = await ProcessFileAsync(file, settings, ct);
            switch (outcome)
            {
                case FileOutcome.Recovered:
                    summary.Recovered++;
                    break;
                case FileOutcome.Skipped:
                    summary.Skipped++;
                    break;
                default:
                    summary.Failed++;
                    if (settings.StopOnError)
                    {
                        _logger.LogWarning("Обработка остановлена по --stop-on-error после ошибки в {file}.", file);
                        return summary;
                    }
                    break;
            }
        }

        return summary;
    }

    private async Task<FileOutcome> ProcessFileAsync(string file, RecoverySettings settings, CancellationToken ct)
    {
        BackupRecord? record;
        try
        {
            var content = await File.ReadAllTextAsync(file, ct);
            record = JsonSerializer.Deserialize<BackupRecord>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось прочитать/разобрать backup-файл {file}. Файл пропущен.", file);
            return FileOutcome.Failed;
        }

        if (record is null)
        {
            _logger.LogError("Backup-файл {file} пуст или не является корректным JSON. Файл пропущен.", file);
            return FileOutcome.Failed;
        }

        if (!TryResolveId(file, record, out var id))
        {
            _logger.LogError("Не удалось определить идентификатор записи для {file} (нет валидного RequestId и имя файла не Guid). Файл пропущен.", file);
            return FileOutcome.Failed;
        }

        _logger.LogInformation("Обработка записи {id} из {file}.", id, Path.GetFileName(file));

        // --- Что именно доставлять ---
        // В режиме Auto решение принимается по состоянию Redis: данных нет — упал Redis,
        // восстанавливаем и данные, и уведомление; данные есть — упала Kafka, шлём только ключ.
        // Режимы Redis/Kafka принудительные: состояние Redis не проверяется.
        bool writeRedis;
        bool produceKafka;

        if (settings.Target == RecoveryTarget.Auto)
        {
            var exists = await TryKeyExistsAsync(settings.ServiceName, id);
            if (exists is null)
            {
                _logger.LogError("Запись {id}: не удалось проверить наличие данных в Redis. Файл {file} сохранён для повторной попытки.",
                    id, Path.GetFileName(file));
                return FileOutcome.Failed;
            }

            writeRedis = !exists.Value;
            produceKafka = true;

            if (writeRedis)
                _logger.LogInformation("Запись {id}: данных в Redis нет — восстанавливаем данные и уведомление.", id);
            else
                _logger.LogInformation("Запись {id}: данные в Redis уже есть — переотправляем только ключ в Kafka.", id);
        }
        else
        {
            writeRedis = settings.Target == RecoveryTarget.Redis;
            produceKafka = settings.Target == RecoveryTarget.Kafka;
        }

        // Пропущенный шаг считается успешным и не мешает удалить файл.
        var redisOk = true;
        var kafkaOk = true;

        // --- Redis ---
        if (writeRedis)
        {
            var dict = BuildRedisPayload(record, id);
            if (settings.DryRun)
            {
                _logger.LogInformation("[DRY-RUN] Redis: записал бы {count} полей в ключ QBCH:{service}:{id} ({fields}).",
                    dict.Count, settings.ServiceName, id, string.Join(", ", dict.Keys));
            }
            else
            {
                redisOk = await TryWriteRedisAsync(settings.ServiceName, id, dict);
            }
        }

        // --- Kafka ---
        if (produceKafka)
        {
            // Уведомление имеет смысл только тогда, когда данные в Redis есть:
            // либо они уже были, либо мы их только что успешно записали.
            if (!redisOk)
            {
                _logger.LogWarning("Запись {id}: пропуск Kafka, т.к. данные не сохранены в Redis.", id);
                kafkaOk = false;
            }
            else
            {
                var kafkaKey = $"QBCH:{settings.ServiceName}:{id}";
                if (settings.DryRun)
                    _logger.LogInformation("[DRY-RUN] Kafka: отправил бы сообщение с ключом-значением '{key}'.", kafkaKey);
                else
                    kafkaOk = await TryProduceKafkaAsync(kafkaKey, id);
            }
        }

        if (!redisOk || !kafkaOk)
        {
            _logger.LogError("Запись {id}: восстановление не выполнено (redis={redis}, kafka={kafka}). Файл {file} сохранён для повторной попытки.",
                id, redisOk, kafkaOk, Path.GetFileName(file));
            return FileOutcome.Failed;
        }

        if (settings.DryRun)
        {
            _logger.LogInformation("[DRY-RUN] Запись {id} обработана бы успешно, файл {file} НЕ удаляется.", id, Path.GetFileName(file));
            return FileOutcome.Skipped;
        }

        if (settings.KeepFiles)
        {
            _logger.LogInformation("Запись {id} восстановлена, файл {file} сохранён (--keep).", id, Path.GetFileName(file));
            return FileOutcome.Recovered;
        }

        try
        {
            File.Delete(file);
            _logger.LogInformation("Запись {id} восстановлена, backup-файл {file} удалён.", id, Path.GetFileName(file));
            return FileOutcome.Recovered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Запись {id} восстановлена, но не удалось удалить файл {file}. Удалите его вручную, чтобы избежать повторной обработки.", id, file);
            return FileOutcome.Failed;
        }
    }

    /// <summary>
    /// Проверяет, есть ли в Redis данные по записи.
    /// <see langword="true"/> — данные на месте (упала Kafka), <see langword="false"/> — данных нет (упал Redis),
    /// <see langword="null"/> — проверить не удалось, решение принимать нельзя.
    /// </summary>
    private async Task<bool?> TryKeyExistsAsync(string serviceName, Guid id)
    {
        try
        {
            return await _redis.KeyExists([serviceName, id.ToString()]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Запись {id}: ошибка проверки ключа QBCH:{service}:{id} в Redis.", id, serviceName, id);
            return null;
        }
    }

    private async Task<bool> TryWriteRedisAsync(string serviceName, Guid id, Dictionary<string, byte[]> dict)
    {
        try
        {
            await _redis.AddHashArray(serviceName, id.ToString(), dict);
            _logger.LogInformation("Запись {id}: {count} полей сохранены в Redis (ключ QBCH:{service}:{id}).", id, dict.Count, serviceName, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Запись {id}: ошибка сохранения в Redis.", id);
            return false;
        }
    }

    private async Task<bool> TryProduceKafkaAsync(string kafkaKey, Guid id)
    {
        try
        {
            var produced = await _kafka.Produce(new Message<Null, string> { Value = kafkaKey });
            if (produced)
                _logger.LogInformation("Запись {id}: уведомление отправлено в Kafka ('{key}').", id, kafkaKey);
            else
                _logger.LogError("Запись {id}: Kafka не подтвердила отправку сообщения '{key}'.", id, kafkaKey);
            return produced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Запись {id}: ошибка отправки в Kafka.", id);
            return false;
        }
    }

    /// <summary>
    /// Восстанавливает набор полей redis-хэша по backup-записи.
    /// Порядок и имена полей повторяют <c>QBCHProcessingCompleteHandler.ConstractResultData</c>.
    /// </summary>
    /// <remarks>
    /// В backup-файле нет сырых данных сертификата (request_certificate_data),
    /// ошибок пакета (package_error) и клиентского request_id — эти поля восстановить нельзя.
    /// </remarks>
    private Dictionary<string, byte[]> BuildRedisPayload(BackupRecord record, Guid id)
    {
        var now = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        var dict = new Dictionary<string, byte[]>
        {
            ["request_date_time"] = Utf8(record.RequestTime ?? string.Empty),
            ["request_certificate_thumbprint"] = Utf8(record.Thumbprint ?? "-"),
            ["response_date_time"] = Utf8(record.ResponseTime ?? now),
            ["response_guid"] = Utf8(id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(record.IpAddress))
            dict["ip_address"] = Utf8(record.IpAddress);

        if (record.ErrorCode.HasValue)
        {
            dict["error_code"] = Utf8(record.ErrorCode.Value.ToString());
            dict["error_message"] = Utf8(record.ErrorMessage ?? string.Empty);
        }

        // Хранится как сырые байты (см. KeyValueStorageService.AddHashArray).
        if (record.SignedRequest is { Length: > 0 })
            dict["request_signed_data"] = record.SignedRequest;

        // Хранится как UTF-8 текст (байты XML).
        if (record.Request is { Length: > 0 })
            dict["request_xml"] = record.Request;

        // Режим «одно окно» (тикет) имеет приоритет над обычным ответом — как в исходном обработчике.
        if (record.SignedResponse_Ticket is { Length: > 0 } && record.ResponseXml_Ticket is { Length: > 0 })
        {
            dict["response_signed_data"] = record.SignedResponse_Ticket;
            dict["response_xml"] = record.ResponseXml_Ticket;
        }
        else
        {
            if (record.SignedResponse is { Length: > 0 })
                dict["response_signed_data"] = record.SignedResponse;
            if (record.ResponseXml is { Length: > 0 })
                dict["response_xml"] = record.ResponseXml;
        }

        dict["validation_date_time"] = Utf8(record.ValidationTime ?? now);

        return dict;
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    private static bool TryResolveId(string file, BackupRecord record, out Guid id)
    {
        // Имя файла — {transaction.Id}.json — основной источник идентификатора.
        var name = Path.GetFileNameWithoutExtension(file);
        if (Guid.TryParse(name, out id))
            return true;

        // Резервно — поле RequestId внутри записи.
        if (record.RequestId is { } fromBody && fromBody != Guid.Empty)
        {
            id = fromBody;
            return true;
        }

        id = Guid.Empty;
        return false;
    }

    private List<string> ResolveFiles(RecoverySettings settings)
    {
        if (settings.Files.Count > 0)
        {
            var explicitFiles = new List<string>();
            foreach (var f in settings.Files)
            {
                if (File.Exists(f))
                    explicitFiles.Add(f);
                else
                    _logger.LogWarning("Указанный файл не найден и будет пропущен: {file}", f);
            }
            return explicitFiles;
        }

        if (!Directory.Exists(settings.BackupDirectory))
        {
            _logger.LogWarning("Каталог backup не найден: {dir}", settings.BackupDirectory);
            return new List<string>();
        }

        return Directory
            .EnumerateFiles(settings.BackupDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    private enum FileOutcome
    {
        Recovered,
        Skipped,
        Failed
    }
}

/// <summary>Настройки одного запуска восстановления.</summary>
public sealed class RecoverySettings
{
    public required string BackupDirectory { get; init; }
    public required IReadOnlyList<string> Files { get; init; }
    public required RecoveryTarget Target { get; init; }
    public required string ServiceName { get; init; }
    public required bool DryRun { get; init; }
    public required bool KeepFiles { get; init; }
    public required bool StopOnError { get; init; }
}

/// <summary>Итог запуска восстановления.</summary>
public sealed class RecoverySummary
{
    /// <summary>Всего найдено файлов.</summary>
    public int Total { get; set; }

    /// <summary>Успешно восстановлено (данные отправлены, файл удалён/сохранён по --keep).</summary>
    public int Recovered { get; set; }

    /// <summary>Пропущено без ошибки (например, dry-run).</summary>
    public int Skipped { get; set; }

    /// <summary>Не обработано из-за ошибки.</summary>
    public int Failed { get; set; }
}
