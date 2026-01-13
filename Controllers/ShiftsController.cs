using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaieApi.Models;
using PaieApi.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaieApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftsController : ControllerBase
    {
        private readonly ShiftService _shiftService;

        public ShiftsController(ShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        [HttpGet]
        public async Task<IActionResult> GetShifts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var shifts = await _shiftService.GetShiftsAsync(userId);
            return Ok(shifts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateShift([FromBody] Shift shift)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            shift.UserId = userId; // Ensure the shift belongs to the user
            
            // Generate ID if missing (Mongo usually handles it but good to be safe if model requires it)
            // But Mongo driver handles null Id by generating one.
            
            try
            {
                var createdShift = await _shiftService.CreateShiftAsync(shift);
                return CreatedAtAction(nameof(GetShift), new { id = createdShift.Id }, createdShift);
            }
            catch (System.InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetShift(string id)
        {
             var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var shift = await _shiftService.GetShiftAsync(id);
            if (shift == null) return NotFound();
            if (shift.UserId != userId) return Forbid();

            return Ok(shift);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShift(string id, [FromBody] Shift shiftIn)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existingShift = await _shiftService.GetShiftAsync(id);
            if (existingShift == null) return NotFound();
            if (existingShift.UserId != userId) return Forbid();

            shiftIn.Id = id;
            shiftIn.UserId = userId; // Prevent changing owner

            await _shiftService.UpdateShiftAsync(id, shiftIn);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existingShift = await _shiftService.GetShiftAsync(id);
            if (existingShift == null) return NotFound();
            if (existingShift.UserId != userId) return Forbid();

            await _shiftService.RemoveShiftAsync(id);
            return NoContent();
        }
    }
}
