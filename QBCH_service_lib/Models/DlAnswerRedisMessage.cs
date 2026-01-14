using System.Net;
using System.Text.Json.Serialization;

namespace QBCHService_lib.Models;

/// <summary>
/// 
/// </summary>
public class DlAnswerRedisMessage : BaseRedisMessage
{
    /// <summary>
    /// 
    /// </summary>
    [JsonIgnore] public string Name { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("request_date_time")] public string? RequestDateTime { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_date_time")] public string? ResponseDateTime { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("error_code")] public string? ErrorCode { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("http_response_code")] public string? HttpResponseCode { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_data")] public byte[]? ResponseData { get; private set; } // signed
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("response_xml")] public byte[]? ResponseXml { get; private set; } //byte[] unsigned

    /// <summary>
    /// 
    /// </summary>
    private DlAnswerRedisMessage()
    {
        Name = "dlanswer";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    private DlAnswerRedisMessage(DateTime? startTime = null)
    {
        Name = "dlanswer";
        RequestDateTime = startTime?.ToString("dd.MM.yyyy HH:mm:ss:ffff") ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <returns></returns>
    public static DlAnswerRedisMessage Create(DateTime? startTime = null)
    {
        return new DlAnswerRedisMessage(startTime);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public DlAnswerRedisMessage SetError(string code, string? message)
    {
        ErrorCode = code;
        ErrorMessage = message;
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="responseTime"></param>
    /// <returns></returns>
    public DlAnswerRedisMessage SetResponseTime(DateTime responseTime)
    {
        ResponseDateTime ??= responseTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public override DlAnswerRedisMessage SetResponseCode(HttpStatusCode? code)
    {
        HttpResponseCode = ((int?)code).ToString();
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signedResponseData"></param>
    /// <returns></returns>
    public override DlAnswerRedisMessage SetSignedResponse(byte[] signedResponseData)
    {
        ResponseData = signedResponseData;
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unsignedResponseXML"></param>
    /// <returns></returns>
    public override DlAnswerRedisMessage SetResponseXml(byte[] unsignedResponseXML)
    {
        ResponseXml = unsignedResponseXML;
        return this;
    }
}
