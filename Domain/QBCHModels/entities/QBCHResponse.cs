using System;

namespace Domain.QBCHModels.entities;

public class QBCHResponse : Entity
{
    public byte[]? TicketXML { get; private set; } //Тикет ошибки 
    public byte[]? SignedTicket { get; private set; } //Подпись
    public byte[]? ResponseXML { get; private set; } //Ответ или Accept Ticket
    public byte[]? SignedResponse { get; private set; } //Подпись
    private QBCHResponse(Guid id) : base(id)
    {

    }

    public static QBCHResponse Create()
    {
        return new QBCHResponse(Guid.NewGuid());
    }

    /// <summary>
    /// Установить тикет для ошибки или для Accepted
    /// </summary>
    /// <param name="ticketXml"></param>
    /// <param name="signedTicket"></param>
    public void SetTicket(byte[] ticketXml, byte[] signedTicket)
    {
        TicketXML = ticketXml;
        SignedTicket = signedTicket;
    }

    /// <summary>
    /// Установить данные для ответа
    /// </summary>
    /// <param name="responseXml"></param>
    /// <param name="signedResponse"></param>
    public void SetResult(byte[] responseXml, byte[] signedResponse)
    {
        ResponseXML = responseXml;
        SignedResponse = signedResponse;
    }
}
