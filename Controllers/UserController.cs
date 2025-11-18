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
using erp.Models.Audit;
using erp.Services.Tenancy;
using System.Security.Claims;

namespace erp.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de usuários do sistema
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<ApplicationRole> _roles;
        private readonly ApplicationDbContext _context;
        private readonly ITenantContextAccessor _tenantContextAccessor;

        public UsersController(
            UserManager<ApplicationUser> users,
            RoleManager<ApplicationRole> roles,
            ApplicationDbContext context,
            ITenantContextAccessor tenantContextAccessor)
        {
            _users = users;
            _roles = roles;
            _context = context;
            _tenantContextAccessor = tenantContextAccessor;
        }

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

        private IQueryable<ApplicationUser> ApplyTenantScope(IQueryable<ApplicationUser> query)
        {
            var tenantId = GetScopedTenantId();
            return tenantId.HasValue ? query.Where(u => u.TenantId == tenantId.Value) : query;
        }

        private IQueryable<ApplicationRole> ApplyTenantScope(IQueryable<ApplicationRole> query)
        {
            var tenantId = GetScopedTenantId();
            return tenantId.HasValue ? query.Where(r => r.TenantId == tenantId.Value) : query;
        }

        private bool UserVisible(ApplicationUser? user)
        {
            if (user is null)
            {
                return false;
            }

            var tenantId = GetScopedTenantId();
            return !tenantId.HasValue || user.TenantId == tenantId.Value;
        }

        /// <summary>
        /// Retorna a lista de todos os usuários do sistema
        /// </summary>
        /// <returns>Lista completa de usuários com suas informações básicas e de RH</returns>
        /// <response code="200">Lista de usuários retornada com sucesso</response>
        /// <response code="401">Usuário não autenticado</response>
        /// <remarks>
        /// Retorna todos os usuários incluindo informações de departamento, cargo, dados pessoais e bancários.
        /// A resposta não é cacheada para garantir dados sempre atualizados.
        /// </remarks>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            // Força uma nova consulta ao banco usando ToListAsync para garantir dados atualizados
            var allUsers = await ApplyTenantScope(_users.Users)
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

        /// <summary>
        /// Busca um usuário específico por ID
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Dados completos do usuário incluindo informações sensíveis</returns>
        /// <response code="200">Usuário encontrado e retornado com sucesso</response>
        /// <response code="404">Usuário não encontrado</response>
        /// <response code="401">Usuário não autenticado</response>
        /// <remarks>
        /// **ATENÇÃO:** Este endpoint retorna dados sensíveis (CPF, RG, dados bancários) e é auditado.
        /// Toda consulta é registrada no log de auditoria com nível de sensibilidade ALTO.
        /// </remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [AuditRead("ApplicationUser", DataSensitivity.High, Description = "Visualização de dados pessoais do usuário (CPF, RG, dados bancários)")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await ApplyTenantScope(_users.Users)
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

        /// <summary>
        /// Cria um novo usuário no sistema
        /// </summary>
        /// <param name="createUserDto">Dados do novo usuário incluindo informações pessoais, de RH e roles</param>
        /// <returns>Usuário criado com ID gerado</returns>
        /// <response code="201">Usuário criado com sucesso</response>
        /// <response code="400">Dados inválidos ou erro ao criar usuário</response>
        /// <response code="401">Usuário não autenticado</response>
        /// <remarks>
        /// Cria um novo usuário com senha padrão "User@123!" se não fornecida.
        /// É obrigatório informar pelo menos uma role/permissão.
        /// 
        /// Exemplo de requisição:
        /// 
        ///     POST /api/users
        ///     {
        ///         "username": "joao.silva",
        ///         "email": "joao.silva@empresa.com",
        ///         "fullName": "João Silva",
        ///         "cpf": "123.456.789-00",
        ///         "phone": "+55 11 98765-4321",
        ///         "roleIds": [1, 2],
        ///         "departmentId": 5,
        ///         "positionId": 3,
        ///         "password": "SenhaSegura123!"
        ///     }
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto.RoleIds == null || createUserDto.RoleIds.Count == 0)
            {
                return BadRequest("Escolha pelo menos uma função/permissão.");
            }
            // Criar ApplicationUser
            var scopedTenantId = GetScopedTenantId();

            var user = new ApplicationUser
            {
                UserName = createUserDto.Username,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.Phone,
                IsActive = true,
                TenantId = scopedTenantId,
                FullName = createUserDto.FullName,
                Cpf = createUserDto.Cpf,
                Rg = createUserDto.Rg,
                DateOfBirth = createUserDto.DateOfBirth.HasValue 
                    ? DateTime.SpecifyKind(createUserDto.DateOfBirth.Value, DateTimeKind.Utc)
                    : null,
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
                HireDate = createUserDto.HireDate.HasValue
                    ? DateTime.SpecifyKind(createUserDto.HireDate.Value, DateTimeKind.Utc)
                    : null,
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
            var allRoles = ApplyTenantScope(_roles.Roles).ToList();
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

        /// <summary>
        /// Atualiza um usuário existente
        /// </summary>
        /// <param name="id">ID do usuário a ser atualizado</param>
        /// <param name="updateUserDto">Dados atualizados do usuário</param>
        /// <returns>Sem conteúdo em caso de sucesso</returns>
        /// <response code="204">Usuário atualizado com sucesso</response>
        /// <response code="400">Dados inválidos ou erro ao atualizar</response>
        /// <response code="404">Usuário não encontrado</response>
        /// <response code="401">Usuário não autenticado</response>
        /// <remarks>
        /// Permite atualizar todos os campos do usuário incluindo:
        /// - Informações básicas (nome, email, telefone)
        /// - Dados pessoais (CPF, RG, data de nascimento)
        /// - Endereço completo
        /// - Informações de RH (departamento, cargo, salário, datas)
        /// - Dados bancários
        /// - Contato de emergência
        /// - Senha (se fornecida)
        /// - Roles/permissões
        /// 
        /// **Nota:** Se uma nova senha for fornecida, ela será aplicada imediatamente.
        /// </remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _users.FindByIdAsync(id.ToString());
            if (!UserVisible(user))
                return NotFound($"Usuário com ID {id} não encontrado.");

            user.UserName = updateUserDto.Username ?? user.UserName;
            user.Email = updateUserDto.Email ?? user.Email;
            user.PhoneNumber = updateUserDto.Phone ?? user.PhoneNumber;
            user.IsActive = updateUserDto.IsActive;
            user.FullName = updateUserDto.FullName;
            user.Cpf = updateUserDto.Cpf;
            user.Rg = updateUserDto.Rg;
            user.DateOfBirth = updateUserDto.DateOfBirth.HasValue 
                ? DateTime.SpecifyKind(updateUserDto.DateOfBirth.Value, DateTimeKind.Utc)
                : null;
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
            user.HireDate = updateUserDto.HireDate.HasValue
                ? DateTime.SpecifyKind(updateUserDto.HireDate.Value, DateTimeKind.Utc)
                : null;
            user.TerminationDate = updateUserDto.TerminationDate.HasValue
                ? DateTime.SpecifyKind(updateUserDto.TerminationDate.Value, DateTimeKind.Utc)
                : null;
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
            var allRoles = ApplyTenantScope(_roles.Roles).ToList();
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

        /// <summary>
        /// Exclui permanentemente um usuário do sistema
        /// </summary>
        /// <param name="id">ID do usuário a ser excluído</param>
        /// <returns>Sem conteúdo em caso de sucesso</returns>
        /// <response code="204">Usuário excluído com sucesso</response>
        /// <response code="404">Usuário não encontrado</response>
        /// <response code="400">Erro ao excluir usuário (ex: violação de integridade referencial)</response>
        /// <response code="401">Usuário não autenticado</response>
        /// <remarks>
        /// **ATENÇÃO:** Esta operação é irreversível e exclui permanentemente o usuário.
        /// Pode falhar se houver registros relacionados (vendas, movimentações, etc.).
        /// Considere desativar o usuário (IsActive=false) ao invés de excluir.
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _users.FindByIdAsync(id.ToString());
            if (!UserVisible(user))
                return NotFound($"Usuário com ID {id} não encontrado.");

            var res = await _users.DeleteAsync(user);
            if (!res.Succeeded)
                return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

            return NoContent();
        }

        /// <summary>
        /// Valida se um email está disponível para uso
        /// </summary>
        /// <param name="email">Email a ser validado</param>
        /// <param name="excludeUserId">ID do usuário a ser excluído da validação (útil para edição)</param>
        /// <returns>Indica se o email está disponível</returns>
        /// <response code="200">Email disponível</response>
        /// <response code="409">Email já está em uso</response>
        /// <response code="400">Email não fornecido</response>
        /// <remarks>
        /// Útil para validação em tempo real durante cadastro ou edição de usuários.
        /// Ao editar um usuário, passe o excludeUserId para permitir manter o email atual.
        /// 
        /// Exemplo de uso:
        /// 
        ///     GET /api/users/validate/email?email=teste@exemplo.com
        ///     GET /api/users/validate/email?email=teste@exemplo.com&amp;excludeUserId=5
        /// </remarks>
        [HttpGet("validate/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ValidationResponse>> ValidateEmail([FromQuery] string email, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new ValidationResponse { IsAvailable = false, Message = "Email é obrigatório" });

            var normalizedEmail = email.Trim().ToUpperInvariant();
            var existingUser = await ApplyTenantScope(_users.Users)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            
            // Se encontrou um usuário e não é o que estamos excluindo da validação
            if (existingUser != null && (!excludeUserId.HasValue || existingUser.Id != excludeUserId.Value))
            {
                return Conflict(new ValidationResponse { IsAvailable = false, Message = "Este email já está em uso" });
            }

            return Ok(new ValidationResponse { IsAvailable = true, Message = "Email disponível" });
        }

        /// <summary>
        /// Valida se um nome de usuário (username) está disponível para uso
        /// </summary>
        /// <param name="username">Nome de usuário a ser validado</param>
        /// <param name="excludeUserId">ID do usuário a ser excluído da validação (útil para edição)</param>
        /// <returns>Indica se o username está disponível</returns>
        /// <response code="200">Username disponível</response>
        /// <response code="409">Username já está em uso</response>
        /// <response code="400">Username não fornecido</response>
        /// <remarks>
        /// Útil para validação em tempo real durante cadastro ou edição de usuários.
        /// Ao editar um usuário, passe o excludeUserId para permitir manter o username atual.
        /// 
        /// Exemplo de uso:
        /// 
        ///     GET /api/users/validate/username?username=joao.silva
        ///     GET /api/users/validate/username?username=joao.silva&amp;excludeUserId=5
        /// </remarks>
        [HttpGet("validate/username")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ValidationResponse>> ValidateUsername([FromQuery] string username, [FromQuery] int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new ValidationResponse { IsAvailable = false, Message = "Username é obrigatório" });

            var normalizedUsername = username.Trim().ToUpperInvariant();
            var existingUser = await ApplyTenantScope(_users.Users)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);
            
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