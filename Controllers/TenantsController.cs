using erp.DTOs.Tenancy;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantBrandingService _brandingService;

    public TenantsController(ITenantService tenantService, ITenantBrandingService brandingService)
    {
        _tenantService = tenantService;
        _brandingService = brandingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants);
    }

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

    [HttpGet("{id:int}/connection-info")]
    public async Task<ActionResult<TenantConnectionInfoDto>> GetConnectionInfoAsync(int id, CancellationToken cancellationToken)
    {
        var info = await _tenantService.GetConnectionInfoAsync(id, cancellationToken);
        if (info is null)
        {
            return NotFound();
        }

        return Ok(info);
    }

    [HttpGet("slug/{slug}/exists")]
    public async Task<ActionResult> CheckSlugAsync(string slug, CancellationToken cancellationToken)
    {
        var exists = await _tenantService.SlugExistsAsync(slug, null, cancellationToken);
        return Ok(new { slug = slug.ToLowerInvariant(), exists });
    }

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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/provision")]
    public async Task<IActionResult> ProvisionAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _tenantService.ProvisionDatabaseAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
