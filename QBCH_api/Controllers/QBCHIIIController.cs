using Asp.Versioning;
using Cache_lib.Interfaces;
using Crypto_lib.Service;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QBCH_api.QBCHProcessing.V2.StoreProcessingData.Event;
using QBCH_api.QBCHProcessing.V3.CreateAndValidation.Command;
using QBCH_api.QBCHProcessing.V3.ResponseDataCollect.Command;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCH_lib.Services.Interfaces.V3;
using XmlService_lib.Services.Interfaces.V3;
using АбонентИПV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИндивидуальныйПредприниматель;
using АбонентИЮЛV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентЮридическоеЛицо;
using АбонентИноV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведенийАбонентИностранноеЛицо;
using ЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ЗапросСведений;
namespace QBCH_api.Controllers;

[ApiVersion("3.0")]
[Route("v{version:apiVersion}")]
[ApiController]
public class QBCHIIIController(
        IMediator mediator,
        ICryptoService cryptoService,
        ILogger<QBCHIIIController> logger,
        IXmlServiceV3 xmlServiceV3,
        ICacheService redisСache,
        ITicketServiceV3 ticketServiceV3,
        IConfiguration config) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICryptoService _cryptoService = cryptoService;
    private readonly ILogger<QBCHIIIController> _logger = logger;
    private readonly IXmlServiceV3 _xmlServiceV3 = xmlServiceV3;
    private readonly ICacheService _redisCache = redisСache;
    private readonly ITicketServiceV3 _ticketServiceV3 = ticketServiceV3;
    private readonly IConfiguration _config = config;
    private readonly string? _ourBureauPSRN = config.GetValue<string>("Bureau:PSRN");


    [HttpPost("dlrequest")]
    [MapToApiVersion("3.0")]
    public async Task<IActionResult> DlRequest_v_3(ApiVersion apiVersion)
    {
        var requestTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:ffff");

        var transaction = await _mediator.Send(new CreateToValidateCommandV3(apiVersion, Request));
        _logger.LogDebug("{guid} Request: {dt}", transaction.Id, requestTime);

        if (transaction.ProcessingErrors.Count != 0)
        {
            var errorTicket = _ticketServiceV3.CreateResultV3Error(transaction.ProcessingErrors.First());
            var errorResult = _xmlServiceV3.SerializeAsByteV3(errorTicket);
            var signedResp = _cryptoService.SignMsg(errorResult);

            transaction.Complete(errorResult, signedResp);

            await _mediator.Publish(new QBCHProcessingComplete(transaction));

            return BadRequest(new MemoryStream(transaction.Response.SignedTicket!));
        }

        try
        {
            var requestV3 = transaction.GetRequest<ЗапросСведенийV3>();
            var requestId = requestV3?.ИдентификаторЗапроса;
            var requestDate = requestV3?.ДатаЗапроса;
            var requestOgrn = requestV3?.Абонент?.Item switch
            {
                АбонентИЮЛV3 юрЛицо => юрЛицо.ОГРН,
                АбонентИПV3 ип => ип.ОГРНИП,
                АбонентИноV3 ино => ино.РегНомер,
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(requestId) &&
                !string.IsNullOrWhiteSpace(requestOgrn) &&
                requestDate.HasValue)
            {
                var uniqueScope = $"{transaction.ServiceName}:v{apiVersion}";
                await _redisCache.AddUniqueRequestId(uniqueScope, requestId, requestOgrn, requestDate.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи уникальнго requestId в redis");
        }

        transaction.TimeElapsedForValidation.Stop();
        _logger.LogDebug("{guid} Validation time elapsed: {elapsed}", transaction.Id, transaction.TimeElapsedForValidation.Elapsed);

        try
        {
            var processingResult = await _mediator
                .Send(new QBCHProcessedStartV3(
                    transaction,
                    _config.GetValue<int>("APIConfiguration:QBCHResponseTimeoutMs"),
                    _ourBureauPSRN ?? string.Empty));

            Response.OnCompleted(async () => await _mediator.Publish(new QBCHProcessingComplete(processingResult)));

            return processingResult.Status switch
            {
                QBCHProcessingStatus.Accepted =>
                    Accepted(new MemoryStream(processingResult.Response.SignedTicket!)),
                _ => File(processingResult.Response.SignedResponse!, "application/octet-stream")
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Возникла критическая ошибка");
            return StatusCode(500);
        }
    }

    [HttpGet("dlanswer")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> DlAnswer_v_3(string? id = null)
    {
        throw new System.NotImplementedException();
    }

    [HttpPost("dlput")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> DlPut_v_3(ApiVersion apiVersion)
    {
        throw new System.NotImplementedException();
    }

    [HttpGet("dlputanswer")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> DlPutAnswer_v_3(ApiVersion version, string? id = null)
    {
        throw new System.NotImplementedException();
    }

    [HttpPost("certadd")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> CertAdd_v_3([FromForm] CertForm form)
    {
        throw new System.NotImplementedException();
    }

    [HttpPost("certrevoke")]
    [MapToApiVersion("3.0")]
    public Task<IActionResult> CertRevoke_v_3([FromForm] CertForm form)
    {
        throw new System.NotImplementedException();
    }
}