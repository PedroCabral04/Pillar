using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Role; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController(RoleManager<ApplicationRole> roleManager) : ControllerBase {

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var roles = roleManager.Roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Abbreviation = r.Abbreviation ?? r.Name!
            }).ToList();
            return Ok(await Task.FromResult(roles));
        }
    }
}