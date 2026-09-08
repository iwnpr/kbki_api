using System;
using System;
using System.Text.Json.Serialization;

namespace QBCH_lib.domain.DTOs;

/// <summary>
/// Контракт backup-файла fallback-сценария: единственное описание формата
/// <c>backup/{RequestId}.json</c>.
/// </summary>
public sealed class BackupRecord
{
    /// <summary>Время поступления запроса (строка формата dd.MM.yyyy HH:mm:ss:ffff).</summary>
    public string? RequestTime { get; set; }

    /// <summary>IP-адрес клиента.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Отпечаток сертификата клиента.</summary>
    public string? Thumbprint { get; set; }

    /// <summary>Сырые данные (DER) сертификата клиента, Base64.</summary>
    public byte[]? CertificateRawData { get; set; }

    /// <summary>Код ошибки процессинга (первая ошибка), если была.</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Текст ошибки процессинга (первая ошибка), если была.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Подписанное тело запроса (УЭП), Base64.</summary>
    public byte[]? SignedRequest { get; set; }

    /// <summary>
    /// Тело запроса без подписи (XML), Base64. Имя свойства в JSON — <c>request</c> с маленькой
    /// буквы: так поле называлось с первой версии формата, менять его нельзя из-за уже записанных файлов.
    /// </summary>
    [JsonPropertyName("request")]
    public byte[]? Request { get; set; }

    /// <summary>Идентификатор запроса (Guid транзакции). Дублирует имя файла.</summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// Тип запроса (числовое значение перечисления <c>СправочникСпособыЗапроса</c>).
    /// Используется только для диагностики, в Redis не восстанавливается.
    /// </summary>
    public int? RequestType { get; set; }

    /// <summary>Подписанный тикет (режим «одно окно»), Base64.</summary>
    public byte[]? SignedResponse_Ticket { get; set; }

    /// <summary>XML тикета (режим «одно окно»), Base64.</summary>
    public byte[]? ResponseXml_Ticket { get; set; }

    /// <summary>Подписанный ответ, Base64.</summary>
    public byte[]? SignedResponse { get; set; }

    /// <summary>XML ответа, Base64.</summary>
    public byte[]? ResponseXml { get; set; }

    /// <summary>Время окончания валидации (строка формата dd.MM.yyyy HH:mm:ss:ffff).</summary>
    public string? ValidationTime { get; set; }

    /// <summary>Время формирования ответа (строка формата dd.MM.yyyy HH:mm:ss:ffff).</summary>
    public string? ResponseTime { get; set; }
}