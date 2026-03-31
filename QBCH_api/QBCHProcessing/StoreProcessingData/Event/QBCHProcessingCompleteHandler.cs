using Cache_lib.Interfaces;
using Confluent.Kafka;
using KafkaService_lib.Services.Interfaces;
using MediatR;
using QBCH_lib.domain.aggregate;
using System.Text;
using System.Text.Json;

namespace QBCH_api.QBCHProcessing.StoreProcessingData.Commands;

public class QBCHProcessingCompleteHandler : INotificationHandler<QBCHProcessingComplete>
{
    private readonly ILogger<QBCHProcessingCompleteHandler> _logger;
    private readonly ICacheService _redisCache;
    private readonly IKafkaService _kafka;
    public QBCHProcessingCompleteHandler(ILogger<QBCHProcessingCompleteHandler> logger, ICacheService redisCache, IKafkaService kafka)
    {
        _logger = logger;
        _redisCache = redisCache;
        _kafka = kafka;
    }
    public async Task Handle(QBCHProcessingComplete notification, CancellationToken cancellationToken)
    {
        var transaction = notification.Transaction;
        try
        {
            var key = $"QBCH:{transaction.ServiceName}:{transaction.Id}";
            var processigResultData = await ConstractResultData(transaction);
            await _redisCache.AddHashArray(transaction.ServiceName, transaction.Id.ToString(), processigResultData);

            //if (transaction.PackageValidationErrors.Count != 0)
            //{

            //    //
            //    var errors = ConstractResultPackageErrorsData(transaction);
            //    await _redisCache.AddHashArray(transaction.ServiceName, $"{transaction.Id}:package_error", errors);
            //}

            // Попытка отправки в кафку                    
            if (!await _kafka.Produce(new Message<Null, string> { Value = key })) // 1.3 - 2.0 разделить
                _logger.LogCritical("Lost key:{key}", key);
        }
        catch (Exception ex)
        {
            //var message = JsonSerializer.Serialize(new
            //{
            //    ServiceName = transaction.ServiceName,
            //    transaction.Id,
            //    IpAddress = transaction.ClentRequest.IpAddress?.ToString(),
            //    transaction.ClentRequest.Certificate?.Thumbprint,
            //    transaction.RequestTime,
            //    ResponseTime = transaction.ResponseTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff"),  
            //    ex.Message
            //});
            _logger.LogCritical(ex, "Критическая ошибка при сохранении в redis");

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(JsonSerializer.Serialize(new
                {
                    RequestTime = transaction.RequestTime,
                    IpAddress = transaction.ClentRequest.IpAddress,
                    Thumbprint = transaction.ClentRequest?.Certificate?.Thumbprint,
                    ErrorCode = transaction.ProcessingErrors?.FirstOrDefault()?.Code,
                    ErrorMessage = transaction.ProcessingErrors?.FirstOrDefault()?.Message,
                    SignedRequest = transaction.Attachment.SignedRequestBody,
                    request = transaction.Attachment.RequestBody,
                    RequestId = transaction.Id,
                    RequestType = transaction.ClentRequest.Request.ТипЗапроса,
                    SignedResponse_Ticket = transaction.Response.SignedTicket,
                    ResponseXml_Ticket = transaction.Response.TicketXML,
                    SignedResponse = transaction.Response.SignedResponse,
                    ResponseXml = transaction.Response.ResponseXML,
                    ValidationTime = transaction.ValidateTime,
                    ResponseTime = transaction.ResponseTime
                }));

                Directory.CreateDirectory("backup");
                await File.WriteAllTextAsync(Path.Combine("backup", $"{transaction.Id}.json"), sb.ToString());
            }
            catch
            {
                _logger.LogCritical(ex, "Критическая ошибка при сохранении в файлик {guid}", transaction.Id);
            }
        }
    }

    public async Task<Dictionary<string, byte[]>> ConstractResultData(QBCHProcessingTransaction transaction)
    {
        var dict = new Dictionary<string, byte[]> //TODO key: hasError, true
        {
            { "request_date_time", Encoding.UTF8.GetBytes(transaction.RequestTime) },
            { "request_certificate_thumbprint", Encoding.UTF8.GetBytes(transaction.ClentRequest.Certificate?.Thumbprint ?? "-") },
            { "response_date_time", Encoding.UTF8.GetBytes(transaction.ResponseTime) },
            { "response_guid", Encoding.UTF8.GetBytes(transaction.Id.ToString()) }
        };

        // Ip адрес
        if (!string.IsNullOrWhiteSpace(transaction.ClentRequest.IpAddress))
            dict.Add("ip_address", Encoding.UTF8.GetBytes(transaction.ClentRequest.IpAddress));

        if (transaction.ClentRequest.Certificate?.RawData is not null)
            dict.Add("request_certificate_data", transaction.ClentRequest.Certificate.RawData);

        // Код ошибки Текст ошибки
        if (transaction.ProcessingErrors.Count > 0)
        {
            dict.Add("error_code", Encoding.UTF8.GetBytes(transaction.ProcessingErrors.First().Code.ToString()));
            dict.Add("error_message", Encoding.UTF8.GetBytes(transaction.ProcessingErrors.First().Message));
        }

        // Подписанное тело запроса
        if (transaction.Attachment.SignedRequestBody is not null)
            dict.Add("request_signed_data", transaction.Attachment.SignedRequestBody);

        // Тело запроса без подписи
        if (transaction.Attachment.RequestBody is not null)
            dict.Add("request_xml", transaction.Attachment.RequestBody);

        // Идентфикатор запроса request id
        if (transaction.ClentRequest.RequestId is not null)
            dict.Add("request_id", Encoding.UTF8.GetBytes(transaction.ClentRequest.RequestId));

        if (transaction.PackageValidationErrors.Count != 0)
            dict.Add("package_error", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transaction.PackageValidationErrors))); //TODO

        // Тип запроса, в одно окно/не в одно окно
        if (transaction.Response.SignedTicket is not null && transaction.Response.TicketXML is not null)
        {
            //dict.Add("error_code", Encoding.UTF8.GetBytes("12"));
            //dict.Add("error_message", Encoding.UTF8.GetBytes("Тикет"));
            dict.Add("response_signed_data", transaction.Response.SignedTicket);
            dict.Add("response_xml", transaction.Response.TicketXML);
        }
        else
        {
            if (transaction.Response.SignedResponse is not null)
                dict.Add("response_signed_data", transaction.Response.SignedResponse);

            if (transaction.Response.ResponseXML is not null)
                dict.Add("response_xml", transaction.Response.ResponseXML);
        }

        /* Если время окончания валидации не существует, значит проверка не пройдена и результат таски возвращен не будет
        */
        if (!await _redisCache.HashFieldExists(transaction.ServiceName, transaction.Id.ToString(), "ValidationTime"))
            dict.Add("validation_date_time", Encoding.UTF8.GetBytes(transaction.ValidateTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff")));

        return dict;
    }

    //public Dictionary<string, byte[]> ConstractResultPackageErrorsData(QBCHProcessingTransaction transaction)
    //{
    //    var dict = new Dictionary<string, byte[]>();

    //    if (transaction.PackageValidationErrors.Count > 0)
    //    {
    //        transaction.PackageValidationErrors.ToList().ForEach(e => dict.Add(e.Key.ToString(), Encoding.UTF8.GetBytes(e.Value.Message)));
    //    }

    //    return dict;
    //}
}
