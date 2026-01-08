using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaieApi.Models;
using PaieApi.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaieApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _service;

        public SettingsController(SettingsService service)
        {
            _service = service;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<ActionResult<UserSetting>> Get()
        {
            var settings = await _service.GetSettingsAsync(GetUserId());
            return Ok(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(UserSetting settings)
        {
            settings.UserId = GetUserId();
            await _service.SaveSettingsAsync(settings);
            return Ok(settings);
        }
    }
}
