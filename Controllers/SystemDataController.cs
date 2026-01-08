using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaieApi.Models;
using PaieApi.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PaieApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemDataController : ControllerBase
    {
        private readonly SystemDataService _service;

        public SystemDataController(SystemDataService service)
        {
            _service = service;
        }

        [HttpGet("payment-types")]
        public async Task<ActionResult<List<PaymentType>>> GetPaymentTypes()
        {
            return await _service.GetPaymentTypesAsync();
        }

        [HttpGet("sick-types")]
        public async Task<ActionResult<List<SickType>>> GetSickTypes()
        {
            return await _service.GetSickTypesAsync();
        }
    }
}
