using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Role; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;
using erp.Services.Tenancy;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace erp.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de perfis/funções (roles) do sistema
    /// </summary>
    [ApiController]
    [Route("api/roles")]
    public class RoleController(RoleManager<ApplicationRole> roleManager, ITenantContextAccessor tenantContextAccessor) : ControllerBase {

        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;

        private int? GetScopedTenantId()
        {
            var tenantId = _tenantContextAccessor.Current?.TenantId;
            if (tenantId.HasValue)
            {
                return tenantId;
            }

            var claimValue = User?.FindFirstValue(TenantClaimTypes.TenantId);
            return int.TryParse(claimValue, out var parsed) ? parsed : null;
        }

        private IQueryable<ApplicationRole> ApplyTenantScope(IQueryable<ApplicationRole> query)
        {
            var tenantId = GetScopedTenantId();
            return tenantId.HasValue ? query.Where(role => role.TenantId == tenantId.Value || role.TenantId == null) : query;
        }

        /// <summary>
        /// Retorna todos os perfis/funções disponíveis no sistema
        /// </summary>
        /// <returns>Lista de roles com ID, nome e abreviação</returns>
        /// <response code="200">Lista de roles retornada com sucesso</response>
        /// <remarks>
        /// Exemplo de resposta:
        /// 
        ///     [
        ///         { "id": 1, "name": "Administrador", "abbreviation": "Admin" },
        ///         { "id": 2, "name": "Gerente", "abbreviation": "Ger" },
        ///         { "id": 3, "name": "Vendedor", "abbreviation": "Vend" }
        ///     ]
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var scopedRoles = ApplyTenantScope(_roleManager.Roles);

            var roles = await scopedRoles
                .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Abbreviation = r.Abbreviation ?? r.Name!
            }).ToListAsync();

            return Ok(roles);
        }
    }
}