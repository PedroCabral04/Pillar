using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using erp.DTOs.Preferences;
using erp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;

        public PreferencesController(UserManager<ApplicationUser> users)
            => _users = users;

        [HttpGet("me")]
        public async Task<ActionResult<UserPreferences>> GetMy()
        {
            var user = await _users.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var prefs = string.IsNullOrWhiteSpace(user.PreferencesJson)
                ? new UserPreferences()
                : JsonSerializer.Deserialize<UserPreferences>(user.PreferencesJson) ?? new UserPreferences();

            return Ok(prefs);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMy([FromBody] UserPreferences prefs)
        {
            var user = await _users.GetUserAsync(User);
            if (user is null) return Unauthorized();

            user.PreferencesJson = JsonSerializer.Serialize(prefs);
            var res = await _users.UpdateAsync(user);
            if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

            return NoContent();
        }
    }
}
