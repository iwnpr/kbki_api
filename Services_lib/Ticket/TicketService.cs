using Application_lib;
using Domain.QBCHModels.CommonTypes;
using Domain.QBCHModels.qcb_xml.v2_0.Enums;
using Domain.QBCHModels.qcb_xml.v2_0.qcb_result;
using Microsoft.Extensions.Configuration;

namespace Services_lib.Ticket
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
        private readonly string _bureauPSRN = config["Bureau:PSRN"];

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
                    ОГРН = _bureauPSRN,
                    Версия = "2.0",
                    РезультатДанные = new РезультатИдентификаторОтвета()
                    {
                        ИдентификаторЗапроса = requestId,
                        Value = guid
                    }

                },
                ResponseType.Error => new Результат()
                {
                    ОГРН = _bureauPSRN,
                    Версия = "2.0",
                    РезультатДанные = new РезультатОшибка()
                    {
                        Код = code,
                        Value = text
                    }

                },
                ResponseType.Success => new Результат()
                {
                    ОГРН = _bureauPSRN,
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
                ОГРН = _bureauPSRN,
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
                ОГРН = _bureauPSRN,
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
        public Результат CreateResultV2Error(Error error)
        {
            return new Результат
            {
                ОГРН = _bureauPSRN,
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
                ОГРН = _bureauPSRN,
                Версия = "2.0",
                РезультатДанные = new РезультатУспешно()
                {
                    ИдентификаторЗапроса = requestId
                }
            };
        }
    }
}