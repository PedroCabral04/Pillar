using erp.DTOs.Tenancy;
using erp.DTOs.User;
using erp.Models.Identity;
using erp.Services.Tenancy;
using erp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using erp.Data;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = RoleNames.SuperAdmin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
/// <summary>
/// Controller responsável por gerenciar tenants (locatários) do sistema.
/// Exponha endpoints de listagem, consulta por id/slug, criação, atualização,
/// exclusão e operações de branding (logo/favicon).
/// Requer autorização com a role "SuperAdmin".
/// </summary>
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantBrandingService _brandingService;
    private readonly IFileValidationService _fileValidationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        ITenantBrandingService brandingService,
        IFileValidationService fileValidationService,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _brandingService = brandingService;
        _fileValidationService = fileValidationService;
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Recupera todos os tenants registrados no sistema.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de <see cref="TenantDto"/> contendo os tenants.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants);
    }

    /// <summary>
    /// Recupera um tenant pelo seu identificador numérico.
    /// </summary>
    /// <param name="id">Identificador do tenant.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>O <see cref="TenantDto"/> correspondente ou 404 se não encontrado.</returns>
    [HttpGet("{id:int}", Name = "GetTenantById")]
    public async Task<ActionResult<TenantDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    /// <summary>
    /// Recupera um tenant pelo seu slug (identificador legível na URL).
    /// </summary>
    /// <param name="slug">Slug do tenant.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>O <see cref="TenantDto"/> correspondente ou 404 se não encontrado.</returns>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<TenantDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetBySlugAsync(slug, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    /// <summary>
    /// Recupera os tenants que o usuário atual tem acesso via TenantMemberships.
    /// Endpoint para permitir tenant switching.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de <see cref="TenantDto"/> que o usuário pode acessar.</returns>
    [HttpGet("my-tenants")]
    [AllowAnonymous] // Permite acesso sem ser admin, mas requer autenticação
    [Authorize] // Requer autenticação
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetMyTenantsAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var tenants = await _tenantService.GetUserTenantsAsync(userId, cancellationToken);
        return Ok(tenants);
    }

    /// <summary>
    /// Verifica se um slug já existe para outro tenant.
    /// </summary>
    /// <param name="slug">Slug a ser verificado.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Objeto indicando o slug normalizado e se existe.</returns>
    [HttpGet("slug/{slug}/exists")]
    public async Task<ActionResult> CheckSlugAsync(string slug, CancellationToken cancellationToken)
    {
        var exists = await _tenantService.SlugExistsAsync(slug, null, cancellationToken);
        return Ok(new { slug = slug.ToLowerInvariant(), exists });
    }

    /// <summary>
    /// Cria um novo tenant.
    /// </summary>
    /// <param name="dto">Dados necessários para criação do tenant.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>O <see cref="TenantDto"/> criado (HTTP 201) ou erro de validação.</returns>
    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateAsync([FromBody] CreateTenantDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();
        var created = await _tenantService.CreateAsync(dto, userId, cancellationToken);
        return CreatedAtRoute("GetTenantById", new { id = created.Id }, created);
    }

    /// <summary>
    /// Atualiza os dados de um tenant existente.
    /// </summary>
    /// <param name="id">Identificador do tenant a ser atualizado.</param>
    /// <param name="dto">Dados atualizados do tenant.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>O <see cref="TenantDto"/> atualizado ou 404 se não encontrado.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantDto>> UpdateAsync(int id, [FromBody] UpdateTenantDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _tenantService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Remove um tenant do sistema.
    /// </summary>
    /// <param name="id">Identificador do tenant a ser removido.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>HTTP 204 quando removido com sucesso.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<IEnumerable<TenantMemberDto>>> GetMembersAsync(int id, CancellationToken cancellationToken)
    {
        var members = await _tenantService.GetMembersAsync(id, cancellationToken);
        return Ok(members);
    }

    [HttpPost("{id:int}/members/{userId:int}")]
    public async Task<ActionResult<TenantMemberDto>> AssignMemberAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var assignedBy = User.Identity?.Name;
        try
        {
            var member = await _tenantService.AssignMemberAsync(id, userId, assignedBy, cancellationToken);
            return Ok(member);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RevokeMemberAsync(int id, int userId, CancellationToken cancellationToken)
    {
        await _tenantService.RevokeMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Upload a branding image (logo or favicon) for a tenant.
    /// Images exceeding maximum dimensions will be auto-resized.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="imageType">Type of image: "logo" or "favicon"</param>
    /// <param name="file">The image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{id:int}/branding/{imageType}")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB max request size
    public async Task<ActionResult<BrandingUploadResult>> UploadBrandingImageAsync(
        int id,
        string imageType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new BrandingUploadResult(false, null, "Nenhum arquivo enviado.", false, 0, 0));
        }

        // Validação de segurança do arquivo (tipo, magic bytes, tamanho)
        var validationResult = await _fileValidationService.ValidateFileAsync(file, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new BrandingUploadResult(false, null, validationResult.ErrorMessage, false, 0, 0));
        }

        await using var stream = file.OpenReadStream();
        var result = await _brandingService.UploadImageAsync(id, imageType, file.FileName, stream, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get dimension recommendations for a branding image type.
    /// </summary>
    /// <param name="imageType">Type of image: "logo" or "favicon"</param>
    [HttpGet("branding/{imageType}/recommendations")]
    public ActionResult<ImageDimensionRecommendation> GetImageRecommendations(string imageType)
    {
        try
        {
            var recommendation = _brandingService.GetDimensionRecommendation(imageType);
            return Ok(recommendation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a branding image for a tenant.
    /// </summary>
    [HttpDelete("{id:int}/branding/{imageType}")]
    public async Task<IActionResult> DeleteBrandingImageAsync(int id, string imageType, CancellationToken cancellationToken)
    {
        await _brandingService.DeleteImageAsync(id, imageType, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Recupera todos os usuários de um tenant específico.
    /// </summary>
    /// <param name="id">ID do tenant.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Lista de usuários do tenant com suas roles.</returns>
    [HttpGet("{id:int}/users", Name = "GetTenantUsers")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetTenantUsersAsync(int id, CancellationToken cancellationToken)
    {
        // Verifica se o tenant existe
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound($"Tenant com ID {id} não encontrado.");
        }

        // Busca usuários do tenant
        var users = await _userManager.Users
            .Where(u => u.TenantId == id)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return Ok(Enumerable.Empty<UserDto>());
        }

        // Carrega roles em batch para evitar N+1
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        var rolesByUser = userRoles.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            Phone = u.PhoneNumber ?? string.Empty,
            RoleNames = rolesByUser.GetValueOrDefault(u.Id, new List<string>()),
            RoleAbbreviations = rolesByUser.GetValueOrDefault(u.Id, new List<string>()),
            IsActive = u.IsActive,
            FullName = u.FullName,
            Cpf = u.Cpf,
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department?.Name,
            PositionId = u.PositionId,
            PositionTitle = u.Position?.Title,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        return Ok(userDtos);
    }

    /// <summary>
    /// Cria um novo usuário em um tenant específico.
    /// </summary>
    /// <param name="id">ID do tenant.</param>
    /// <param name="dto">Dados do novo usuário.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Usuário criado.</returns>
    [HttpPost("{id:int}/users")]
    public async Task<ActionResult<UserDto>> CreateTenantUserAsync(int id, [FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        // Verifica se o tenant existe
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound($"Tenant com ID {id} não encontrado.");
        }

        if (dto.RoleIds == null || dto.RoleIds.Count == 0)
        {
            return BadRequest("É necessário selecionar pelo menos uma role.");
        }

        // Busca as roles e valida que pertencem ao tenant ou são globais
        var allRoles = await _roleManager.Roles
            .Where(r => r.TenantId == id || r.TenantId == null)
            .ToListAsync(cancellationToken);

        var selectedRoles = allRoles.Where(r => dto.RoleIds.Contains(r.Id)).ToList();
        if (selectedRoles.Count != dto.RoleIds.Count)
        {
            return BadRequest("Uma ou mais roles selecionadas não são válidas para este tenant.");
        }

        // Gera senha segura se não fornecida
        var password = string.IsNullOrWhiteSpace(dto.Password)
            ? GenerateSecureRandomPassword()
            : dto.Password;

        // Cria o usuário
        var user = new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.Phone,
            TenantId = id,
            IsActive = true,
            FullName = dto.FullName,
            Cpf = dto.Cpf,
            Rg = dto.Rg,
            DateOfBirth = dto.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
                : null,
            Gender = dto.Gender,
            MaritalStatus = dto.MaritalStatus,
            PostalCode = dto.PostalCode,
            Street = dto.Street,
            Number = dto.Number,
            Complement = dto.Complement,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            Salary = dto.Salary,
            HireDate = dto.HireDate.HasValue
                ? DateTime.SpecifyKind(dto.HireDate.Value, DateTimeKind.Utc)
                : null,
            ContractType = dto.ContractType,
            EmploymentStatus = dto.EmploymentStatus ?? "Ativo",
            BankCode = dto.BankCode,
            BankName = dto.BankName,
            BankAgency = dto.BankAgency,
            BankAccount = dto.BankAccount,
            BankAccountType = dto.BankAccountType,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactRelationship = dto.EmergencyContactRelationship,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            WorkCard = dto.WorkCard,
            PisNumber = dto.PisNumber,
            EducationLevel = dto.EducationLevel,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Loga senha gerada (em produção deveria enviar por email)
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            _logger.LogInformation("Senha temporária gerada para {Email}: {Password}", user.Email, password);
        }

        // Adiciona as roles
        var roleNames = selectedRoles.Select(r => r.Name!).ToList();
        var roleResult = await _userManager.AddToRolesAsync(user, roleNames);
        if (!roleResult.Succeeded)
        {
            return BadRequest(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber ?? string.Empty,
            RoleNames = roleNames,
            RoleAbbreviations = roleNames,
            IsActive = user.IsActive,
            FullName = user.FullName
        };

        return CreatedAtRoute("GetTenantUsers", new { id }, userDto);
    }

    /// <summary>
    /// Atualiza um usuário de um tenant específico.
    /// </summary>
    /// <param name="id">ID do tenant.</param>
    /// <param name="userId">ID do usuário.</param>
    /// <param name="dto">Dados atualizados do usuário.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Sem conteúdo em caso de sucesso.</returns>
    [HttpPut("{id:int}/users/{userId:int}")]
    public async Task<IActionResult> UpdateTenantUserAsync(int id, int userId, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        // Verifica se o tenant existe
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound($"Tenant com ID {id} não encontrado.");
        }

        // Busca o usuário e verifica se pertence ao tenant
        var user = await _userManager.Users
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound($"Usuário com ID {userId} não encontrado.");
        }

        if (user.TenantId != id)
        {
            return BadRequest("Este usuário não pertence ao tenant especificado.");
        }

        // Atualiza os dados do usuário
        user.UserName = dto.Username ?? user.UserName;
        user.Email = dto.Email ?? user.Email;
        user.PhoneNumber = dto.Phone ?? user.PhoneNumber;
        user.IsActive = dto.IsActive;
        user.FullName = dto.FullName;
        user.Cpf = dto.Cpf;
        user.Rg = dto.Rg;
        user.DateOfBirth = dto.DateOfBirth.HasValue
            ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc)
            : null;
        user.Gender = dto.Gender;
        user.MaritalStatus = dto.MaritalStatus;
        user.ProfilePhotoUrl = dto.ProfilePhotoUrl;
        user.PostalCode = dto.PostalCode;
        user.Street = dto.Street;
        user.Number = dto.Number;
        user.Complement = dto.Complement;
        user.Neighborhood = dto.Neighborhood;
        user.City = dto.City;
        user.State = dto.State;
        user.Country = dto.Country;
        user.DepartmentId = dto.DepartmentId;
        user.PositionId = dto.PositionId;
        user.Salary = dto.Salary;
        user.HireDate = dto.HireDate.HasValue
            ? DateTime.SpecifyKind(dto.HireDate.Value, DateTimeKind.Utc)
            : null;
        user.TerminationDate = dto.TerminationDate.HasValue
            ? DateTime.SpecifyKind(dto.TerminationDate.Value, DateTimeKind.Utc)
            : null;
        user.ContractType = dto.ContractType;
        user.EmploymentStatus = dto.EmploymentStatus;
        user.BankCode = dto.BankCode;
        user.BankName = dto.BankName;
        user.BankAgency = dto.BankAgency;
        user.BankAccount = dto.BankAccount;
        user.BankAccountType = dto.BankAccountType;
        user.EmergencyContactName = dto.EmergencyContactName;
        user.EmergencyContactRelationship = dto.EmergencyContactRelationship;
        user.EmergencyContactPhone = dto.EmergencyContactPhone;
        user.WorkCard = dto.WorkCard;
        user.PisNumber = dto.PisNumber;
        user.EducationLevel = dto.EducationLevel;
        user.Notes = dto.Notes;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        // Atualiza senha se fornecida
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passRes = await _userManager.ResetPasswordAsync(user, resetToken, dto.Password);
            if (!passRes.Succeeded)
            {
                return BadRequest(string.Join("; ", passRes.Errors.Select(e => e.Description)));
            }
        }

        // Atualiza roles
        var allRoles = await _roleManager.Roles
            .Where(r => r.TenantId == id || r.TenantId == null)
            .ToListAsync(cancellationToken);

        var desiredRoles = allRoles.Where(r => dto.RoleIds.Contains(r.Id)).Select(r => r.Name!).ToList();
        var currentRoles = await _userManager.GetRolesAsync(user);
        var toAdd = desiredRoles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(desiredRoles).ToList();

        if (toAdd.Count > 0)
        {
            var addRes = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addRes.Succeeded)
            {
                return BadRequest(string.Join("; ", addRes.Errors.Select(e => e.Description)));
            }
        }

        if (toRemove.Count > 0)
        {
            var remRes = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!remRes.Succeeded)
            {
                return BadRequest(string.Join("; ", remRes.Errors.Select(e => e.Description)));
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Atualiza apenas as roles de um usuário do tenant.
    /// </summary>
    /// <param name="id">ID do tenant.</param>
    /// <param name="userId">ID do usuário.</param>
    /// <param name="dto">Lista de IDs das roles a serem atribuídas.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Sem conteúdo em caso de sucesso.</returns>
    [HttpPut("{id:int}/users/{userId:int}/roles")]
    public async Task<IActionResult> UpdateUserRolesAsync(int id, int userId, [FromBody] UpdateUserRolesDto dto, CancellationToken cancellationToken)
    {
        // Verifica se o tenant existe
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound($"Tenant com ID {id} não encontrado.");
        }

        // Busca o usuário e verifica se pertence ao tenant
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound($"Usuário com ID {userId} não encontrado.");
        }

        if (user.TenantId != id)
        {
            return BadRequest("Este usuário não pertence ao tenant especificado.");
        }

        // Valida que as roles selecionadas pertencem ao tenant ou são globais
        var allRoles = await _roleManager.Roles
            .Where(r => r.TenantId == id || r.TenantId == null)
            .ToListAsync(cancellationToken);

        var selectedRoles = allRoles.Where(r => dto.RoleIds.Contains(r.Id)).ToList();
        if (selectedRoles.Count != dto.RoleIds.Count)
        {
            return BadRequest("Uma ou mais roles selecionadas não são válidas para este tenant.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var desiredRoles = selectedRoles.Select(r => r.Name!).ToList();
        var toAdd = desiredRoles.Except(currentRoles).ToList();
        var toRemove = currentRoles.Except(desiredRoles).ToList();

        if (toAdd.Count > 0)
        {
            var addRes = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addRes.Succeeded)
            {
                return BadRequest(string.Join("; ", addRes.Errors.Select(e => e.Description)));
            }
        }

        if (toRemove.Count > 0)
        {
            var remRes = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!remRes.Succeeded)
            {
                return BadRequest(string.Join("; ", remRes.Errors.Select(e => e.Description)));
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    private static string GenerateSecureRandomPassword()
    {
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*?_-";

        var passwordChars = new char[16];

        // Ensure at least one of each required character type
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        passwordChars[0] = GetRandomChar(lowercase, rng);
        passwordChars[1] = GetRandomChar(uppercase, rng);
        passwordChars[2] = GetRandomChar(digits, rng);
        passwordChars[3] = GetRandomChar(special, rng);

        // Fill the rest with random characters from all pools
        const string allChars = lowercase + uppercase + digits + special;
        for (int i = 4; i < passwordChars.Length; i++)
        {
            passwordChars[i] = GetRandomChar(allChars, rng);
        }

        // Shuffle the password to avoid predictable pattern
        Shuffle(passwordChars, rng);

        return new string(passwordChars);
    }

    private static char GetRandomChar(string charSet, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        var buffer = new byte[4];
        rng.GetBytes(buffer);
        var randomValue = BitConverter.ToUInt32(buffer, 0);
        return charSet[(int)(randomValue % (uint)charSet.Length)];
    }

    private static void Shuffle(char[] array, System.Security.Cryptography.RandomNumberGenerator rng)
    {
        int n = array.Length;
        var buffer = new byte[4];

        for (int i = n - 1; i > 0; i--)
        {
            rng.GetBytes(buffer);
            var randomValue = BitConverter.ToUInt32(buffer, 0);
            int j = (int)(randomValue % (uint)(i + 1));
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
