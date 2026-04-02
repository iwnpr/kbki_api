using Microsoft.Extensions.Configuration;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_result;
using QBCH_lib.Services.Interfaces;
using System;

namespace QBCH_lib.Services.Implementations
{
    public class TicketService(IConfiguration config) : ITicketService
    {
        private readonly string _bureauPsrn = config.GetValue<string>("Bureau:PSRN") ?? string.Empty;

        public Результат CreateReceiptWithAnswerId(string requestId, string answerId, DateTime requestDate, long? readyInMs = null)
        {
            var receipt = new РезультатИдентификаторОтвета
            {
                ИдентификаторЗапроса = requestId,
                ДатаЗапроса = requestDate.Date,
                Value = answerId
            };

            if (readyInMs.HasValue)
            {
                receipt.ВремяГотовности = readyInMs.Value;
                receipt.ВремяГотовностиSpecified = true;
            }
            return BuildResult(receipt);
        }

        public Результат CreateSuccessReceipt(string requestId, DateTime requestDate)
        {
            return BuildResult(new РезультатУспешно
            {
                ИдентификаторЗапроса = requestId,
                ДатаЗапроса = requestDate.Date
            });
        }

        public Результат CreateErrorReceipt(string code, string message)
        {
            return BuildResult(new РезультатОшибка
            {
                Код = code,
                Value = message
            });
        }

        public Результат CreateErrorReceipt(core.Error error)
        {
            return CreateErrorReceipt(error.Code.ToString(), error.Message);
        }

        public Результат CreateResult(ResponseType type, string? code = null, string? text = null, string? requestId = null, string? guid = null)
        {
            return type switch
            {
                ResponseType.Ticket => CreateReceiptWithAnswerId(requestId ?? string.Empty, guid ?? string.Empty, DateTime.Today),
                ResponseType.Success => CreateSuccessReceipt(requestId ?? string.Empty, DateTime.Today),
                ResponseType.Error => CreateErrorReceipt(code ?? string.Empty, text ?? string.Empty),
                _ => throw new Exception("type не определен")
            };
        }
        public Результат CreateResultV2Common(string requestId, string guid)
            => CreateReceiptWithAnswerId(requestId, guid, DateTime.Today);

        public Результат CreateResultV2Common(string requestId, string guid, DateTime dateTime)
            => CreateReceiptWithAnswerId(requestId, guid, dateTime);

        public Результат CreateResultV2Error(core.Error error)
            => CreateErrorReceipt(error);

        public Результат CreateResultV2Success(string requestId)
            => CreateSuccessReceipt(requestId, DateTime.Today);

        private Результат BuildResult(object payload)
        {
            return new Результат
            {
                ОГРН = _bureauPsrn,
                Версия = "3.0",
                РезультатДанные = payload
            };
        }
    }
}