using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Role; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController(ApplicationDbContext context) : ControllerBase {

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var roles = await context.Roles
                                      .AsNoTracking() // Good practice for read-only queries
                                      .Select(r => new RoleDto { Id = r.Id, Name = r.Name })
                                      .ToListAsync();
            return Ok(roles);
        }
    }
}