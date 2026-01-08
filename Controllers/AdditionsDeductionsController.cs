using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaieApi.Models;
using PaieApi.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PaieApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdditionsDeductionsController : ControllerBase
    {
        private readonly AdditionDeductionService _service;

        public AdditionsDeductionsController(AdditionDeductionService service)
        {
            _service = service;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<ActionResult<List<AdditionDeduction>>> Get()
        {
            return await _service.GetAllAsync(GetUserId());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AdditionDeduction item)
        {
            item.UserId = GetUserId();
            await _service.CreateAsync(item);
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, AdditionDeduction item)
        {
            item.Id = id;
            item.UserId = GetUserId();
            await _service.UpdateAsync(id, item);
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id, GetUserId());
            return Ok();
        }
    }
}
