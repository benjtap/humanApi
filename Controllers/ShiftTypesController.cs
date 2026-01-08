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
    public class ShiftTypesController : ControllerBase
    {
        private readonly ShiftTypeService _service;

        public ShiftTypesController(ShiftTypeService service)
        {
            _service = service;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<ActionResult<List<ShiftType>>> Get()
        {
            return await _service.GetShiftTypesAsync(GetUserId());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShiftType shiftType)
        {
            shiftType.UserId = GetUserId();
            // Handle numericId generation if needed or assume frontend sends unique one?
            // Actually frontend generates it sequentially.
            // But usually backing store ID is ObjectId.
            // We just store numericId as property.
            await _service.CreateShiftTypeAsync(shiftType);
            return Ok(shiftType);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, ShiftType shiftType)
        {
            shiftType.Id = id; // This might be the GLOBAL ID if it's an override attempt
            shiftType.UserId = GetUserId();
            
            var updated = await _service.UpdateShiftTypeAsync(id, shiftType);
            if (updated == null) return NotFound();
            
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteShiftTypeAsync(id, GetUserId());
            return Ok();
        }
    }
}
