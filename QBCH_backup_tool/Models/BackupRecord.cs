using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBCH_backup_tool.Models;

/// <summary>
/// Модель одной записи backup-файла fallback-сценария.
/// Структура повторяет объект, который сериализует
/// <c>QBCHProcessingCompleteHandler.SaveBackupData</c> в API при недоступности Redis.
/// Каждый backup-файл (<c>backup/{RequestId}.json</c>) содержит ровно один такой объект.
/// </summary>
/// <remarks>
/// Поля-массивы байт (<c>byte[]</c>) сериализуются System.Text.Json в строку Base64
/// и автоматически декодируются обратно при чтении.
/// </remarks>
public sealed class BackupRecord
{
    // <summary>
    /// Версия API, обработавшего запрос (например, <c>3.0</c>).
    /// Записывается обработчиком V3; в старых (V2) файлах отсутствует.
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Версия контракта. Записывается обработчиком V3.
    /// </summary>
    public string? ContractVersion { get; set; }

    /// <summary>
    /// Вид ответа (<c>answer</c> / <c>ticket</c> / <c>putanswer</c>). Записывается обработчиком V3.
    /// </summary>
    public string? ResponseKind { get; set; }

    /// <summary>
    /// Семейство схемы ответа. Записывается обработчиком V3.
    /// </summary>
    public string? SchemaFamily { get; set; }

    /// <summary>
    /// Имя сервиса / redis-scope, под которым API сохранял бы запись.
    /// Используется, если не задан <c>--service-name</c>.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Время поступления запроса (строка формата dd.MM.yyyy HH:mm:ss:ffff).
    /// </summary>
    public string? RequestTime { get; set; }

    /// <summary>
    /// IP-адрес клиента.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Отпечаток сертификата клиента.
    /// </summary>
    public string? Thumbprint { get; set; }

    /// <summary>
    /// Сырые данные (DER) сертификата клиента, Base64. Записывается обработчиком V3.
    /// </summary>
    public byte[]? CertificateRawData { get; set; }

    /// <summary>
    /// Код ошибки процессинга (первая ошибка), если была.
    /// </summary>
    public int? ErrorCode { get; set; }

    /// <summary>
    /// Текст ошибки процессинга (первая ошибка), если была.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Подписанное тело запроса (УЭП), Base64.
    /// </summary>
    public byte[]? SignedRequest { get; set; }

    /// <summary>
    /// Тело запроса без подписи (XML), Base64.
    /// </summary>
    [JsonPropertyName("request")]
    public byte[]? Request { get; set; }

    /// <summary>
    /// Идентификатор запроса (Guid транзакции).
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// Клиентский идентификатор запроса (заголовок request_id). Записывается обработчиком V3.
    /// </summary>
    public string? ClientRequestId { get; set; }

    /// <summary>
    /// Ошибки пакетного запроса — уже сериализованный JSON-массив. Записывается обработчиком V3.
    /// </summary>
    public string? PackageError { get; set; }

    /// <summary>
    /// Тип запроса (перечисление), используется только для диагностики.
    /// </summary>
    public JsonElement? RequestType { get; set; }

    /// <summary>
    /// Подписанный тикет (режим «одно окно»), Base64.
    /// </summary>
    public byte[]? SignedResponse_Ticket { get; set; }

    /// <summary>
    /// XML тикета (режим «одно окно»), Base64.
    /// </summary>
    public byte[]? ResponseXml_Ticket { get; set; }

    /// <summary>
    /// Подписанный ответ, Base64.
    /// </summary>
    public byte[]? SignedResponse { get; set; }

    /// <summary>
    /// XML ответа, Base64.
    /// </summary>
    public byte[]? ResponseXml { get; set; }

    /// <summary>
    /// Время окончания валидации (строка формата dd.MM.yyyy HH:mm:ss:ffff).
    /// </summary>
    public string? ValidationTime { get; set; }

    /// <summary>
    /// Время формирования ответа (строка формата dd.MM.yyyy HH:mm:ss:ffff).
    /// </summary>
    public string? ResponseTime { get; set; }
}
