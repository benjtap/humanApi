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
    public class WeeklyPlanController : ControllerBase
    {
        private readonly WeeklyPlanService _weeklyPlanService;

        public WeeklyPlanController(WeeklyPlanService weeklyPlanService)
        {
            _weeklyPlanService = weeklyPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyPlans()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var plans = await _weeklyPlanService.GetWeeklyPlansAsync(userId);
            return Ok(plans);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWeeklyPlan([FromBody] WeeklyPlan plan)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            plan.UserId = userId;
            
            var createdPlan = await _weeklyPlanService.CreateWeeklyPlanAsync(plan);
            return CreatedAtAction(nameof(GetWeeklyPlan), new { id = createdPlan.Id }, createdPlan);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWeeklyPlan(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var plan = await _weeklyPlanService.GetWeeklyPlanAsync(id);
            if (plan == null) return NotFound();
            if (plan.UserId != userId) return Forbid();

            return Ok(plan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWeeklyPlan(string id, [FromBody] WeeklyPlan planIn)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existingPlan = await _weeklyPlanService.GetWeeklyPlanAsync(id);
            if (existingPlan == null) return NotFound();
            if (existingPlan.UserId != userId) return Forbid();

            planIn.Id = id;
            planIn.UserId = userId;

            await _weeklyPlanService.UpdateWeeklyPlanAsync(id, planIn);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeeklyPlan(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var existingPlan = await _weeklyPlanService.GetWeeklyPlanAsync(id);
            if (existingPlan == null) return NotFound();
            if (existingPlan.UserId != userId) return Forbid();

            await _weeklyPlanService.RemoveWeeklyPlanAsync(id);
            return NoContent();
        }
    }
}
