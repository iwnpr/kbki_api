using System.Text.Json.Serialization;

namespace QBCHService_lib.Models.DTOs;

/// <summary>
/// 
/// </summary>
public class DlAnswerRedisMessageDTO
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("request_date_time")] public string? RequestDateTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_date_time")] public string? ResponseDateTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("request_data")] public byte[]? RequestData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("request_xml")] public byte[]? RequestXml { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("error_code")] public string? ErrorCode { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_data")] public byte[]? ResponseData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_xml")] public byte[]? ResponseXml { get; set; }
}
