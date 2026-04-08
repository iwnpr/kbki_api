using QBCH.Lib.qcb_xml.v3_0;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;


namespace QBCH_lib.CommonTypes.Api.V3;

/// <summary>
/// Модель результата для API 3.0.
/// </summary>
public class BaseResultV3
{/// <summary>
 /// 
 /// </summary>
    public bool IsError { get; set; } = false;

    /// <summary>
    /// Список ошибок
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Тип ошибки
    /// </summary>
    public int ErrorCode { get; set; }

    /// <summary>
    /// Тикет
    /// </summary>
    public Результат? TicketV3 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Stopwatch Timer { get; set; } = Stopwatch.StartNew();

    /// <summary>
    /// 
    /// </summary>
    public string RequestTime { get; set; } = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

    /// <summary>
    /// 
    /// </summary>
    public string ServiceName { get; set; } = "dlrequest";

    /// <summary>
    /// 
    /// </summary>
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// 
    /// </summary>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public byte[]? SignedRequest { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public byte[]? SignedResponse { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public byte[]? SignedTicket { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public byte[]? ResponseXml { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public byte[]? TicketXml { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public byte[]? CryptoServiceResult { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? ErrorMessage { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? ValidationTime { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? ResponseTime { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? RequestId { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? RequestType { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Десериализованная модель запроса версии 3.0.
    /// </summary>
    public ЗапросСведений? RequestV3 { get; set; } = null;
}
