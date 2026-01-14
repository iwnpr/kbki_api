using System.Net;
using System.Text.Json.Serialization;

namespace Domain.QBCHServiceModels;

/// <summary>
/// 
/// </summary>
public class DlRequestRedisMessage : BaseRedisMessage
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
    [JsonPropertyName("request_data")] public byte[]? RequestData { get; private set; } // singed
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("request_xml")] public byte[]? RequestXml { get; private set; } //byte[] unsigned
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
    private DlRequestRedisMessage()
    {
        Name = "dlrequest";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="signedRequestData"></param>
    /// <param name="unsignedRequestData"></param>
    private DlRequestRedisMessage(DateTime startTime, byte[] signedRequestData, byte[] unsignedRequestData)
    {
        Name = "dlrequest";
        RequestDateTime = startTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        RequestData = signedRequestData;
        RequestXml = unsignedRequestData;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="signedRequestData"></param>
    /// <param name="unsignedRequestData"></param>
    /// <returns></returns>
    public static DlRequestRedisMessage Create(DateTime startTime, byte[] signedRequestData, byte[] unsignedRequestData)
    {
        return new DlRequestRedisMessage(startTime, signedRequestData, unsignedRequestData);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public DlRequestRedisMessage SetError(string code, string? message)
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
    public DlRequestRedisMessage SetResponseTime(DateTime responseTime)
    {
        ResponseDateTime ??= responseTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public override DlRequestRedisMessage SetResponseCode(HttpStatusCode? code)
    {
        HttpResponseCode = ((int?)code).ToString();
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="signedResponseData"></param>
    /// <returns></returns>
    public override DlRequestRedisMessage SetSignedResponse(byte[] signedResponseData)
    {
        ResponseData = signedResponseData;
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unsignedResponseXML"></param>
    /// <returns></returns>
    public override DlRequestRedisMessage SetResponseXml(byte[] unsignedResponseXML)
    {
        ResponseXml = unsignedResponseXML;
        return this;
    }
}
