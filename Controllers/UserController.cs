using Microsoft.AspNetCore.Mvc;
using erp.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;
using erp.Mappings;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<ApplicationRole> _roles;

        public UsersController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            
            // Força uma nova consulta ao banco usando ToListAsync para garantir dados atualizados
            var allUsers = await _users.Users.AsNoTracking().ToListAsync();
            var userDtos = new List<UserDto>(allUsers.Count);
            foreach (var u in allUsers)
            {
                var roles = await _users.GetRolesAsync(u);
                userDtos.Add(new UserDto
                {
                    Id = u.Id,
                    Username = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Phone = u.PhoneNumber ?? string.Empty,
                    RoleNames = roles.ToList(),
                    RoleAbbreviations = roles.ToList(),
                    IsActive = (u as ApplicationUser)?.IsActive ?? true
                });
            }
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _users.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            var roles = await _users.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                RoleNames = roles.ToList(),
                RoleAbbreviations = roles.ToList(),
                IsActive = (user as ApplicationUser)?.IsActive ?? true
            };
            return Ok(userDto);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto.RoleIds == null || createUserDto.RoleIds.Count == 0)
            {
                return BadRequest("Escolha pelo menos uma função/permissão.");
            }
            // Criar ApplicationUser
            var user = new ApplicationUser
            {
                UserName = createUserDto.Username,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.Phone,
                IsActive = true
            };

            var password = string.IsNullOrWhiteSpace(createUserDto.Password) ? "User@123!" : createUserDto.Password!;
            var result = await _users.CreateAsync(user, password);
            if (!result.Succeeded)
                return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

            // Atribuir roles por Id => precisamos dos nomes
            var allRoles = _roles.Roles.ToList();
            var toAssign = allRoles.Where(r => createUserDto.RoleIds.Contains(r.Id)).Select(r => r.Name!).ToList();
            if (toAssign.Count > 0)
            {
                var r = await _users.AddToRolesAsync(user, toAssign);
                if (!r.Succeeded)
                    return BadRequest(string.Join("; ", r.Errors.Select(e => e.Description)));
            }

            var dto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                RoleNames = toAssign,
                RoleAbbreviations = toAssign,
                IsActive = user.IsActive
            };

            return CreatedAtAction(nameof(GetUserById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _users.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            user.UserName = updateUserDto.Username ?? user.UserName;
            user.Email = updateUserDto.Email ?? user.Email;
            user.PhoneNumber = updateUserDto.Phone ?? user.PhoneNumber;
            if (user is ApplicationUser au)
                au.IsActive = updateUserDto.IsActive;

            var update = await _users.UpdateAsync(user);
            if (!update.Succeeded)
                return BadRequest(string.Join("; ", update.Errors.Select(e => e.Description)));

            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                var resetToken = await _users.GeneratePasswordResetTokenAsync(user);
                var passRes = await _users.ResetPasswordAsync(user, resetToken, updateUserDto.Password);
                if (!passRes.Succeeded)
                    return BadRequest(string.Join("; ", passRes.Errors.Select(e => e.Description)));
            }

            // Roles
            var currentRoles = await _users.GetRolesAsync(user);
            var allRoles = _roles.Roles.ToList();
            var desiredRoles = allRoles.Where(r => updateUserDto.RoleIds.Contains(r.Id)).Select(r => r.Name!).ToList();
            var toAdd = desiredRoles.Except(currentRoles).ToList();
            var toRemove = currentRoles.Except(desiredRoles).ToList();

            if (toAdd.Count > 0)
            {
                var addRes = await _users.AddToRolesAsync(user, toAdd);
                if (!addRes.Succeeded)
                    return BadRequest(string.Join("; ", addRes.Errors.Select(e => e.Description)));
            }
            if (toRemove.Count > 0)
            {
                var remRes = await _users.RemoveFromRolesAsync(user, toRemove);
                if (!remRes.Succeeded)
                    return BadRequest(string.Join("; ", remRes.Errors.Select(e => e.Description)));
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _users.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            var res = await _users.DeleteAsync(user);
            if (!res.Succeeded)
                return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

            return NoContent();
        }

        // Validation endpoints for async validation in forms
        [HttpGet("validate/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ValidateEmail([FromQuery] string email, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email é obrigatório");

            var existingUser = await _users.FindByEmailAsync(email);
            
            if (existingUser == null)
                return Ok(new { available = true });

            // Se estamos editando e é o mesmo usuário, ok
            if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                return Ok(new { available = true });

            return Conflict(new { available = false, message = "Este email já está em uso" });
        }

        [HttpGet("validate/username")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ValidateUsername([FromQuery] string username, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username é obrigatório");

            var existingUser = await _users.FindByNameAsync(username);
            
            if (existingUser == null)
                return Ok(new { available = true });

            // Se estamos editando e é o mesmo usuário, ok
            if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                return Ok(new { available = true });

            return Conflict(new { available = false, message = "Este nome de usuário já está em uso" });
        }
    }
}