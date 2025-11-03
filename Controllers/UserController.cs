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
using erp.Data;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<ApplicationRole> _roles;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles, ApplicationDbContext context)
        {
            _users = users;
            _roles = roles;
            _context = context;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            // Força uma nova consulta ao banco usando ToListAsync para garantir dados atualizados
            var allUsers = await _users.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Position)
                .ToListAsync();
                
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
                    IsActive = u.IsActive,
                    FullName = u.FullName,
                    Cpf = u.Cpf,
                    Rg = u.Rg,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    MaritalStatus = u.MaritalStatus,
                    ProfilePhotoUrl = u.ProfilePhotoUrl,
                    PostalCode = u.PostalCode,
                    Street = u.Street,
                    Number = u.Number,
                    Complement = u.Complement,
                    Neighborhood = u.Neighborhood,
                    City = u.City,
                    State = u.State,
                    Country = u.Country,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department?.Name,
                    PositionId = u.PositionId,
                    PositionTitle = u.Position?.Title,
                    Salary = u.Salary,
                    HireDate = u.HireDate,
                    TerminationDate = u.TerminationDate,
                    ContractType = u.ContractType,
                    EmploymentStatus = u.EmploymentStatus,
                    BankCode = u.BankCode,
                    BankName = u.BankName,
                    BankAgency = u.BankAgency,
                    BankAccount = u.BankAccount,
                    BankAccountType = u.BankAccountType,
                    EmergencyContactName = u.EmergencyContactName,
                    EmergencyContactRelationship = u.EmergencyContactRelationship,
                    EmergencyContactPhone = u.EmergencyContactPhone,
                    WorkCard = u.WorkCard,
                    PisNumber = u.PisNumber,
                    EducationLevel = u.EducationLevel,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                });
            }
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _users.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .FirstOrDefaultAsync(u => u.Id == id);
                
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
                IsActive = user.IsActive,
                FullName = user.FullName,
                Cpf = user.Cpf,
                Rg = user.Rg,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                MaritalStatus = user.MaritalStatus,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                PostalCode = user.PostalCode,
                Street = user.Street,
                Number = user.Number,
                Complement = user.Complement,
                Neighborhood = user.Neighborhood,
                City = user.City,
                State = user.State,
                Country = user.Country,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                PositionId = user.PositionId,
                PositionTitle = user.Position?.Title,
                Salary = user.Salary,
                HireDate = user.HireDate,
                TerminationDate = user.TerminationDate,
                ContractType = user.ContractType,
                EmploymentStatus = user.EmploymentStatus,
                BankCode = user.BankCode,
                BankName = user.BankName,
                BankAgency = user.BankAgency,
                BankAccount = user.BankAccount,
                BankAccountType = user.BankAccountType,
                EmergencyContactName = user.EmergencyContactName,
                EmergencyContactRelationship = user.EmergencyContactRelationship,
                EmergencyContactPhone = user.EmergencyContactPhone,
                WorkCard = user.WorkCard,
                PisNumber = user.PisNumber,
                EducationLevel = user.EducationLevel,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
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
                IsActive = true,
                FullName = createUserDto.FullName,
                Cpf = createUserDto.Cpf,
                Rg = createUserDto.Rg,
                DateOfBirth = createUserDto.DateOfBirth,
                Gender = createUserDto.Gender,
                MaritalStatus = createUserDto.MaritalStatus,
                PostalCode = createUserDto.PostalCode,
                Street = createUserDto.Street,
                Number = createUserDto.Number,
                Complement = createUserDto.Complement,
                Neighborhood = createUserDto.Neighborhood,
                City = createUserDto.City,
                State = createUserDto.State,
                Country = createUserDto.Country,
                DepartmentId = createUserDto.DepartmentId,
                PositionId = createUserDto.PositionId,
                Salary = createUserDto.Salary,
                HireDate = createUserDto.HireDate,
                ContractType = createUserDto.ContractType,
                EmploymentStatus = createUserDto.EmploymentStatus ?? "Ativo",
                BankCode = createUserDto.BankCode,
                BankName = createUserDto.BankName,
                BankAgency = createUserDto.BankAgency,
                BankAccount = createUserDto.BankAccount,
                BankAccountType = createUserDto.BankAccountType,
                EmergencyContactName = createUserDto.EmergencyContactName,
                EmergencyContactRelationship = createUserDto.EmergencyContactRelationship,
                EmergencyContactPhone = createUserDto.EmergencyContactPhone,
                WorkCard = createUserDto.WorkCard,
                PisNumber = createUserDto.PisNumber,
                EducationLevel = createUserDto.EducationLevel,
                CreatedAt = DateTime.UtcNow
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
            user.IsActive = updateUserDto.IsActive;
            user.FullName = updateUserDto.FullName;
            user.Cpf = updateUserDto.Cpf;
            user.Rg = updateUserDto.Rg;
            user.DateOfBirth = updateUserDto.DateOfBirth;
            user.Gender = updateUserDto.Gender;
            user.MaritalStatus = updateUserDto.MaritalStatus;
            user.ProfilePhotoUrl = updateUserDto.ProfilePhotoUrl;
            user.PostalCode = updateUserDto.PostalCode;
            user.Street = updateUserDto.Street;
            user.Number = updateUserDto.Number;
            user.Complement = updateUserDto.Complement;
            user.Neighborhood = updateUserDto.Neighborhood;
            user.City = updateUserDto.City;
            user.State = updateUserDto.State;
            user.Country = updateUserDto.Country;
            user.DepartmentId = updateUserDto.DepartmentId;
            user.PositionId = updateUserDto.PositionId;
            user.Salary = updateUserDto.Salary;
            user.HireDate = updateUserDto.HireDate;
            user.TerminationDate = updateUserDto.TerminationDate;
            user.ContractType = updateUserDto.ContractType;
            user.EmploymentStatus = updateUserDto.EmploymentStatus;
            user.BankCode = updateUserDto.BankCode;
            user.BankName = updateUserDto.BankName;
            user.BankAgency = updateUserDto.BankAgency;
            user.BankAccount = updateUserDto.BankAccount;
            user.BankAccountType = updateUserDto.BankAccountType;
            user.EmergencyContactName = updateUserDto.EmergencyContactName;
            user.EmergencyContactRelationship = updateUserDto.EmergencyContactRelationship;
            user.EmergencyContactPhone = updateUserDto.EmergencyContactPhone;
            user.WorkCard = updateUserDto.WorkCard;
            user.PisNumber = updateUserDto.PisNumber;
            user.EducationLevel = updateUserDto.EducationLevel;
            user.Notes = updateUserDto.Notes;
            user.UpdatedAt = DateTime.UtcNow;

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

        /// <summary>
        /// Valida se um email está disponível para uso
        /// </summary>
        [HttpGet("validate/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ValidationResponse>> ValidateEmail([FromQuery] string email, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new ValidationResponse { IsAvailable = false, Message = "Email é obrigatório" });

            var existingUser = await _users.FindByEmailAsync(email.Trim());
            
            // Se encontrou um usuário e não é o que estamos excluindo da validação
            if (existingUser != null && (!excludeUserId.HasValue || existingUser.Id != excludeUserId.Value))
            {
                return Conflict(new ValidationResponse { IsAvailable = false, Message = "Este email já está em uso" });
            }

            return Ok(new ValidationResponse { IsAvailable = true, Message = "Email disponível" });
        }

        /// <summary>
        /// Valida se um username está disponível para uso
        /// </summary>
        [HttpGet("validate/username")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ValidationResponse>> ValidateUsername([FromQuery] string username, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new ValidationResponse { IsAvailable = false, Message = "Username é obrigatório" });

            var existingUser = await _users.FindByNameAsync(username.Trim());
            
            // Se encontrou um usuário e não é o que estamos excluindo da validação
            if (existingUser != null && (!excludeUserId.HasValue || existingUser.Id != excludeUserId.Value))
            {
                return Conflict(new ValidationResponse { IsAvailable = false, Message = "Este nome de usuário já está em uso" });
            }

            return Ok(new ValidationResponse { IsAvailable = true, Message = "Username disponível" });
        }
    }

    public class ValidationResponse
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}