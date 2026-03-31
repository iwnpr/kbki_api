using Microsoft.Extensions.Configuration;
using QBCH_lib.qcb_xml.v1_3.Enums;
using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_result;
using QBCH_lib.Services.Interfaces;
using System;

namespace QBCH_lib.Services.Implementations
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="config"></param>
    public class TicketService(IConfiguration config) : ITicketService
    {
        private readonly string _BureauPSRN = config.GetValue<string>("Bureau:PSRN");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="text"></param>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public qcb_xml.v1_3.qcb_result.Результат CreateResult(ResultType type, string? code = null, string? text = null, string? requestId = null, string? guid = null)
        {
            return type switch
            {
                ResultType.Ticket => new qcb_xml.v1_3.qcb_result.Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "1.2",
                    Item = new qcb_xml.v1_3.qcb_result.РезультатИдентификаторОтвета()
                    {
                        ИдентификаторЗапроса = requestId,
                        Value = guid
                    }

                },
                ResultType.Error => new qcb_xml.v1_3.qcb_result.Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "1.2",
                    Item = new qcb_xml.v1_3.CommonTypes.Ошибка()
                    {
                        Код = code,
                        Value = text
                    }

                },
                ResultType.Success => new qcb_xml.v1_3.qcb_result.Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "1.2",
                    Item = new qcb_xml.v1_3.qcb_result.РезультатУспешно()
                    {
                        ИдентификаторЗапроса = requestId
                    }

                },
                _ => throw new Exception("type не определен"),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="text"></param>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Результат CreateResultv2(ResponseType type, string? code = null, string? text = null, string? requestId = null, string? guid = null)
        {
            return type switch
            {
                ResponseType.Ticket => new Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "2.0",
                    РезультатДанные = new РезультатИдентификаторОтвета()
                    {
                        ИдентификаторЗапроса = requestId,
                        Value = guid
                    }

                },
                ResponseType.Error => new Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "2.0",
                    РезультатДанные = new РезультатОшибка()
                    {
                        Код = code,
                        Value = text
                    }

                },
                ResponseType.Success => new Результат()
                {
                    ОГРН = _BureauPSRN,
                    Версия = "2.0",
                    РезультатДанные = new РезультатУспешно()
                    {
                        ИдентификаторЗапроса = requestId
                    }

                },
                _ => throw new Exception("type не определен"),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Результат CreateResultV2Common(string requestId, string guid)
        {
            return new Результат
            {
                ОГРН = _BureauPSRN,
                Версия = "2.0",
                РезультатДанные = new РезультатИдентификаторОтвета
                {
                    ИдентификаторЗапроса = requestId,
                    Value = guid,
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public Результат CreateResultV2Common(string requestId, string guid, DateTime dateTime)
        {
            return new Результат
            {
                ОГРН = _BureauPSRN,
                Версия = "2.0",
                РезультатДанные = new РезультатИдентификаторОтвета
                {
                    ИдентификаторЗапроса = requestId,
                    Value = guid,
                    ДатаЗапроса = dateTime
                }
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public Результат CreateResultV2Error(core.Error error)
        {
            return new Результат
            {
                ОГРН = _BureauPSRN,
                Версия = "2.0",
                РезультатДанные = new РезультатОшибка()
                {
                    Код = error.Code.ToString(),
                    Value = error.Message,
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public Результат CreateResultV2Success(string requestId)
        {
            return new Результат
            {
                ОГРН = _BureauPSRN,
                Версия = "2.0",
                РезультатДанные = new РезультатУспешно()
                {
                    ИдентификаторЗапроса = requestId
                }
            };
        }
    }
}