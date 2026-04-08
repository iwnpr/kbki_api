using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QBCH_lib.CommonTypes.Api;

namespace QBCH_api.Controllers
{
    [ApiVersion("3.0")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    public class QBCHIIIController : ControllerBase
    {
        [HttpPost("dlrequest")]
        [MapToApiVersion("3.0")]
        public Task<IActionResult> DlRequest_v_3(ApiVersion apiVersion)
        {
            throw new System.NotImplementedException();
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
}