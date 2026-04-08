using QBCH_lib.qcb_xml.v1_3.qcb_result;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// Базовый класс ошибки
    /// </summary>
    public class BaseResult
    {
        /// <summary>
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
        public Результат? Ticket { get; set; }

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
        public string guid { get; set; } = Guid.NewGuid().ToString();

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
        ///  Десериализованная модель входного запроса
        /// </summary>
        public object? RequestPayload { get; set; } = null;

        /// <summary>
        /// Десериализованная модель запроса версии 3.0.
        /// </summary>
        public ЗапросСведений? Request
        {
            get => RequestPayload as ЗапросСведений;
            set => RequestPayload = value;
        }
    }
}
