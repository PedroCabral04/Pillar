using erp.DTOs.Tenancy;
using erp.Services.Tenancy;
using erp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
/// <summary>
/// Controller responsável por gerenciar tenants (locatários) do sistema.
/// Exponha endpoints de listagem, consulta por id/slug, criação, atualização,
/// exclusão e operações de branding (logo/favicon).
/// Requer autorização com a role "Administrador".
/// </summary>
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantBrandingService _brandingService;
    private readonly IFileValidationService _fileValidationService;

    public TenantsController(
        ITenantService tenantService, 
        ITenantBrandingService brandingService,
        IFileValidationService fileValidationService)
    {
        _tenantService = tenantService;
        _brandingService = brandingService;
        _fileValidationService = fileValidationService;
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
    [HttpGet("{id:int}")]
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
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
