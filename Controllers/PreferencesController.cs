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
    /// <summary>
    /// Controller responsável por recuperar e atualizar preferências do usuário atual.
    /// Fornece endpoints para obter as preferências do usuário autenticado e atualizá-las.
    /// </summary>
    public class PreferencesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;

        public PreferencesController(UserManager<ApplicationUser> users)
            => _users = users;

        /// <summary>
        /// Recupera as preferências do usuário autenticado.
        /// </summary>
        /// <returns>Objeto <see cref="UserPreferences"/> representando as preferências do usuário.</returns>
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

        /// <summary>
        /// Atualiza as preferências do usuário autenticado.
        /// </summary>
        /// <param name="prefs">Objeto <see cref="UserPreferences"/> com as preferências a serem salvas.</param>
        /// <returns>Resposta HTTP 204 quando bem-sucedido, ou erro apropriado.</returns>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMy([FromBody] UserPreferences prefs)
        {
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // Always fetch fresh user to get latest ConcurrencyStamp
                var user = await _users.GetUserAsync(User);
                if (user is null) return Unauthorized();

                user.PreferencesJson = JsonSerializer.Serialize(prefs);
                var res = await _users.UpdateAsync(user);
                
                if (res.Succeeded)
                    return NoContent();

                // Check if this is a concurrency error - if so, retry
                var isConcurrencyError = res.Errors.Any(e => 
                    e.Code == "ConcurrencyFailure" || 
                    e.Description.Contains("concurrency", StringComparison.OrdinalIgnoreCase));
                
                if (!isConcurrencyError || attempt == maxRetries - 1)
                    return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
                
                // Small delay before retry to reduce collision likelihood
                await Task.Delay(50 * (attempt + 1));
            }

            return BadRequest("Failed to save preferences after multiple attempts");
        }
    }
}
