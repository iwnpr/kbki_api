using System;
using System.Security.Cryptography.X509Certificates;
using QBCH_lib.core;
using QBCH_lib.qcb_xml.v2_0.qcb_put;
using QBCH_lib.qcb_xml.v2_0.qcb_request;

namespace QBCH_lib.domain.entities;

/// <summary>
/// клиентский запрос
/// </summary>
public class ClentRequest : Entity
{
    /// <summary>
    /// запроса
    /// </summary>
    public string? RequestId { get; private set; }
    /// <summary>
    /// Метод запроса
    /// </summary>
    public string? RequestMethod { get; private set; }
    /// <summary>
    /// Дата-время запроса
    /// </summary>
    public string? RequestTime { get; private set; }
    /// <summary>
    /// ip адрес
    /// </summary>
    public string? IpAddress { get; private set; }
    /// <summary>
    /// Сертификат с которым установлено соединение
    /// </summary>
    public X509Certificate2? Certificate { get; private set; }
    /// <summary>
    /// ОГРН из запроса
    /// </summary>
    public string? RequestOGRN { get; private set; }
    /// <summary>
    /// ИНН из запроса
    /// </summary>
    public string? RequestINN { get; private set; }

    /// <summary>
    /// xml запроса
    /// </summary>
    public ЗапросСведений? Request { get; private set; }

    public ПредставлениеСведенийОПлатежах? PutRequest { get; private set; }

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="id"></param>
    /// <param name="requestMethod"></param>
    /// <param name="requestTime"></param>
    /// <param name="ipAddress"></param>
    /// <param name="certificate"></param>
    private ClentRequest(Guid id,
                        string requestMethod,
                        DateTime requestTime,
                        string? ipAddress,
                        X509Certificate2? certificate) : base(id)
    {
        RequestMethod = requestMethod;
        RequestTime = requestTime.ToString("dd.MM.yyyy HH:mm:ss:ffff");
        IpAddress = ipAddress;
        Certificate = certificate;
    }

    /// <summary>
    /// Создать клиентский запрос
    /// </summary>
    /// <param name="requestMethod">Имя запроса</param>
    /// <param name="requestTime">Время запроса</param>
    /// <param name="ipAddress">ip адрес клиента</param>
    /// <param name="certificate">Сертификат запроса</param>
    /// <returns></returns>
    public static ClentRequest Create(string requestMethod, DateTime requestTime, string? ipAddress, X509Certificate2? certificate)
    {
        return new ClentRequest(Guid.NewGuid(),
                                requestMethod,
                                requestTime,
                                ipAddress,
                                certificate);
    }

    /// <summary>
    /// Уставновить запрос
    /// </summary>
    /// <param name="request">Запрос</param>
    public void SetRequest(ЗапросСведений request)
    {
        Request ??= request;
    }

    /// <summary>
    /// Уставновить запрос
    /// </summary>
    /// <param name="request">Запрос</param>
    public void SetRequestDlPut(ПредставлениеСведенийОПлатежах request)
    {
        PutRequest ??= request;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestThumbprint"></param>
    /// <param name="requestInn"></param>
    /// <param name="requestOgrn"></param>
    public void SetRequestCertificateData(string? requestThumbprint, string? requestInn, string? requestOgrn)
    {
        RequestINN = requestInn;
        RequestOGRN = requestOgrn;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestId"></param>
    public void SetRequestId(string requestId)
    {
        RequestId ??= requestId;
    }


}
