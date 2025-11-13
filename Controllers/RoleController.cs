using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Role; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;

namespace erp.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de perfis/funções (roles) do sistema
    /// </summary>
    [ApiController]
    [Route("api/roles")]
    public class RoleController(RoleManager<ApplicationRole> roleManager) : ControllerBase {

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