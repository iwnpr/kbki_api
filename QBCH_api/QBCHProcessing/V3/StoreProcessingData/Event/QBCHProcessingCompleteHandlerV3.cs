using Cache_lib.Interfaces;
using Confluent.Kafka;
using KafkaService_lib.Services.Interfaces;
using MediatR;
using QBCH_lib.domain.aggregate;
using System.Text;
using System.Text.Json;

namespace QBCH_api.QBCHProcessing.V3.StoreProcessingData.Event;

public class QBCHProcessingCompleteHandlerV3(ILogger<QBCHProcessingCompleteHandlerV3> logger, ICacheService redisCache, IKafkaService kafka) : INotificationHandler<QBCHProcessingCompleteV3>
{
    private const string ApiVersion = "3.0";

    private readonly ILogger<QBCHProcessingCompleteHandlerV3> _logger = logger;
    private readonly ICacheService _redisCache = redisCache;
    private readonly IKafkaService _kafka = kafka;

    public async Task Handle(QBCHProcessingCompleteV3 notification, CancellationToken cancellationToken)
    {
        var transaction = notification.Transaction;

        try
        {
            var resultData = await ConstructResultData(transaction);
            await _redisCache.AddHashArray(transaction.ServiceName, transaction.Id.ToString(), resultData);

            var (responseKind, schemaFamily) = ResolveResponseShape(transaction);

            var kafkaPayload = JsonSerializer.Serialize(new
            {
                api_version = ApiVersion,
                service = transaction.ServiceName,
                id = transaction.Id,
                redis_key = $"QBCH:{transaction.ServiceName}:{transaction.Id}",
                versioned_key = $"QBCH:v{ApiVersion}:{transaction.ServiceName}:{transaction.Id}",
                response_kind = responseKind,
                schema_family = schemaFamily
            });

            if (!await _kafka.Produce(new Message<Null, string> { Value = kafkaPayload }))
            {
                _logger.LogCritical("Lost V3 kafka payload for key QBCH:{service}:{id}", transaction.ServiceName, transaction.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка при сохранении API 3.0 результата в redis/kafka");
        }
    }

    private async Task<Dictionary<string, byte[]>> ConstructResultData(QBCHProcessingTransaction transaction)
    {
        var (responseKind, schemaFamily) = ResolveResponseShape(transaction);

        var dict = new Dictionary<string, byte[]>
        {
            { "api_version", Encoding.UTF8.GetBytes(ApiVersion) },
            { "contract_version", Encoding.UTF8.GetBytes(ApiVersion) },
            { "response_kind", Encoding.UTF8.GetBytes(responseKind) },
            { "schema_family", Encoding.UTF8.GetBytes(schemaFamily) },
            { "request_date_time", Encoding.UTF8.GetBytes(transaction.RequestTime) },
            { "request_certificate_thumbprint", Encoding.UTF8.GetBytes(transaction.ClentRequest.Certificate?.Thumbprint ?? "-") },
            { "response_date_time", Encoding.UTF8.GetBytes(transaction.ResponseTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff")) },
            { "response_guid", Encoding.UTF8.GetBytes(transaction.Id.ToString()) }
        };

        if (!string.IsNullOrWhiteSpace(transaction.ClentRequest.IpAddress))
            dict.Add("ip_address", Encoding.UTF8.GetBytes(transaction.ClentRequest.IpAddress));

        if (transaction.ClentRequest.Certificate?.RawData is not null)
            dict.Add("request_certificate_data", transaction.ClentRequest.Certificate.RawData);

        if (transaction.ProcessingErrors.Count > 0)
        {
            dict.Add("error_code", Encoding.UTF8.GetBytes(transaction.ProcessingErrors.First().Code.ToString()));
            dict.Add("error_message", Encoding.UTF8.GetBytes(transaction.ProcessingErrors.First().Message));
        }

        if (transaction.Attachment.SignedRequestBody is not null)
            dict.Add("request_signed_data", transaction.Attachment.SignedRequestBody);

        if (transaction.Attachment.RequestBody is not null)
            dict.Add("request_xml", transaction.Attachment.RequestBody);

        if (transaction.ClentRequest.RequestId is not null)
            dict.Add("request_id", Encoding.UTF8.GetBytes(transaction.ClentRequest.RequestId));

        if (transaction.PackageValidationErrors.Count != 0)
            dict.Add("package_error", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transaction.PackageValidationErrors)));

        if (transaction.Response.SignedTicket is not null && transaction.Response.TicketXML is not null)
        {
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

        if (!await _redisCache.HashFieldExists(transaction.ServiceName, transaction.Id.ToString(), "ValidationTime"))
            dict.Add("validation_date_time", Encoding.UTF8.GetBytes(transaction.ValidateTime ?? DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff")));

        return dict;
    }

    private static (string responseKind, string schemaFamily) ResolveResponseShape(QBCHProcessingTransaction transaction)
    {
        if (string.Equals(transaction.ServiceName, "dlput", StringComparison.OrdinalIgnoreCase))
            return ("putanswer", "qcb_putanswer");

        var hasTicket = transaction.Response.SignedTicket is not null && transaction.Response.TicketXML is not null;
        if (hasTicket || transaction.Status == QBCHProcessingStatus.Accepted || transaction.Status == QBCHProcessingStatus.Failure)
            return ("ticket", "qcb_result");

        return ("answer", "qcb_answer");
    }
}
