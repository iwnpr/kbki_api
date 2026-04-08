using QBCH.Lib.qcb_xml.v3_0;
using System;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_lib.domain.entities.V3;

/// <summary>
/// Обертка клиентского запроса для API 3.0.
/// </summary>
public class ClentRequestV3
{
    private readonly ClentRequest _inner;

    private ClentRequestV3(ClentRequest inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Внутренний объект запроса.
    /// </summary>
    public ClentRequest Inner => _inner;

    public string? RequestId => _inner.RequestId;
    public string? RequestMethod => _inner.RequestMethod;
    public string? RequestTime => _inner.RequestTime;
    public string? IpAddress => _inner.IpAddress;
    public X509Certificate2? Certificate => _inner.Certificate;
    public string? RequestOGRN => _inner.RequestOGRN;
    public string? RequestINN => _inner.RequestINN;
    public object? RequestPayload => _inner.RequestPayload;

    /// <summary>
    /// Десериализованная модель запроса версии 3.0.
    /// </summary>
    public ЗапросСведений? RequestV3 =>
        _inner.RequestPayload as ЗапросСведений;

    /// <summary>
    /// Создать клиентский запрос для API 3.0.
    /// </summary>
    public static ClentRequestV3 Create(string requestMethod, DateTime requestTime, string? ipAddress, X509Certificate2? certificate)
    {
        return new ClentRequestV3(ClentRequest.Create(requestMethod, requestTime, ipAddress, certificate));
    }

    /// <summary>
    /// Обернуть существующий клиентский запрос.
    /// </summary>
    public static ClentRequestV3 From(ClentRequest request)
    {
        return new ClentRequestV3(request);
    }

    /// <summary>
    /// Установить десериализованную модель запроса версии 3.0.
    /// </summary>
    public void SetRequestV3(ЗапросСведений request)
    {
        _inner.SetRequest(request);
    }

    public void SetRequestCertificateData(string? requestThumbprint, string? requestInn, string? requestOgrn)
    {
        _inner.SetRequestCertificateData(requestThumbprint, requestInn, requestOgrn);
    }

    public void SetRequestId(string requestId)
    {
        _inner.SetRequestId(requestId);
    }
}
