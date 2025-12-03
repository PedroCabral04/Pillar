using erp.Data;
using erp.Models.Tenancy;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;

namespace erp.Services.Tenancy;

/// <summary>
/// Service for handling tenant branding image uploads with auto-resize support.
/// Images are stored in wwwroot/uploads/tenants/{slug}/.
/// </summary>
public class TenantBrandingService : ITenantBrandingService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TenantBrandingService> _logger;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantBrandingProvider _brandingProvider;

    // Dimension configurations
    private static readonly Dictionary<string, ImageDimensionRecommendation> DimensionConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["logo"] = new ImageDimensionRecommendation(
            RecommendedWidth: 200,
            RecommendedHeight: 60,
            MaxWidth: 512,
            MaxHeight: 512,
            MaxFileSizeBytes: 2 * 1024 * 1024, // 2MB
            AllowedExtensions: [".png", ".jpg", ".jpeg", ".svg", ".webp"],
            Description: "Logo da empresa. Recomendado: 200×60px. Máximo: 512×512px."
        ),
        ["favicon"] = new ImageDimensionRecommendation(
            RecommendedWidth: 32,
            RecommendedHeight: 32,
            MaxWidth: 128,
            MaxHeight: 128,
            MaxFileSizeBytes: 512 * 1024, // 512KB
            AllowedExtensions: [".png", ".ico", ".svg"],
            Description: "Ícone do navegador. Recomendado: 32×32px ou 64×64px. Máximo: 128×128px."
        )
    };

    public TenantBrandingService(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        ILogger<TenantBrandingService> logger,
        ITenantContextAccessor tenantContextAccessor,
        ITenantBrandingProvider brandingProvider)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
        _tenantContextAccessor = tenantContextAccessor;
        _brandingProvider = brandingProvider;
    }

    public ImageDimensionRecommendation GetDimensionRecommendation(string imageType)
    {
        if (DimensionConfigs.TryGetValue(imageType, out var config))
        {
            return config;
        }

        throw new ArgumentException($"Tipo de imagem desconhecido: {imageType}", nameof(imageType));
    }

    public async Task<BrandingUploadResult> UploadImageAsync(
        int tenantId,
        string imageType,
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get tenant with branding (tracking enabled for update)
            var tenant = await _db.Tenants
                .Include(t => t.Branding)
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant is null)
            {
                return new BrandingUploadResult(false, null, $"Tenant {tenantId} não encontrado.", false, 0, 0);
            }

            // Validate image type
            if (!DimensionConfigs.TryGetValue(imageType, out var config))
            {
                return new BrandingUploadResult(false, null, $"Tipo de imagem inválido: {imageType}", false, 0, 0);
            }

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!config.AllowedExtensions.Contains(extension))
            {
                return new BrandingUploadResult(
                    false, null,
                    $"Extensão '{extension}' não permitida. Use: {string.Join(", ", config.AllowedExtensions)}",
                    false, 0, 0);
            }

            // Read file into memory to check size
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);

            if (memoryStream.Length > config.MaxFileSizeBytes)
            {
                var maxSizeMb = config.MaxFileSizeBytes / (1024.0 * 1024.0);
                return new BrandingUploadResult(
                    false, null,
                    $"Arquivo muito grande. Máximo: {maxSizeMb:F1}MB",
                    false, 0, 0);
            }

            memoryStream.Position = 0;

            BrandingUploadResult result;

            // Handle SVG files (no processing needed)
            if (extension == ".svg")
            {
                result = await SaveFileDirectlyAsync(tenant.Slug, imageType, extension, memoryStream, cancellationToken);
            }
            // Handle ICO files (limited processing)
            else if (extension == ".ico")
            {
                result = await SaveFileDirectlyAsync(tenant.Slug, imageType, extension, memoryStream, cancellationToken);
            }
            // Process raster images (PNG, JPG, WebP)
            else
            {
                result = await ProcessAndSaveImageAsync(tenant.Slug, imageType, extension, memoryStream, config, cancellationToken);
            }

            // Persist the URL in the database if upload was successful
            if (result.Success && !string.IsNullOrEmpty(result.Url))
            {
                await PersistBrandingUrlAsync(tenant, imageType, result.Url, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer upload de imagem {ImageType} para tenant {TenantId}", imageType, tenantId);
            return new BrandingUploadResult(false, null, $"Erro ao processar imagem: {ex.Message}", false, 0, 0);
        }
    }

    private async Task PersistBrandingUrlAsync(Tenant tenant, string imageType, string url, CancellationToken cancellationToken)
    {
        // Create branding if it doesn't exist
        if (tenant.Branding is null)
        {
            tenant.Branding = new TenantBranding();
        }

        // Update the appropriate URL field
        switch (imageType.ToLowerInvariant())
        {
            case "logo":
                tenant.Branding.LogoUrl = url;
                break;
            case "favicon":
                tenant.Branding.FaviconUrl = url;
                break;
        }

        tenant.Branding.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // Notify branding change if this is the current user's tenant
        if (_tenantContextAccessor.Current.TenantId == tenant.Id)
        {
            _tenantContextAccessor.SetTenant(tenant);
            _brandingProvider.NotifyBrandingChanged();
        }

        _logger.LogInformation("Persisted {ImageType} URL for tenant {TenantId}: {Url}", imageType, tenant.Id, url);
    }

    public async Task DeleteImageAsync(int tenantId, string imageType, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Branding)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return;
        }

        var uploadDir = GetUploadDirectory(tenant.Slug);
        if (Directory.Exists(uploadDir))
        {
            // Delete all files matching the image type pattern
            var pattern = $"{imageType}.*";
            var files = Directory.GetFiles(uploadDir, pattern);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted branding file: {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete branding file: {File}", file);
                }
            }
        }

        // Clear the URL in the database
        if (tenant.Branding is not null)
        {
            switch (imageType.ToLowerInvariant())
            {
                case "logo":
                    tenant.Branding.LogoUrl = null;
                    break;
                case "favicon":
                    tenant.Branding.FaviconUrl = null;
                    break;
            }

            tenant.Branding.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            // Notify branding change if this is the current user's tenant
            if (_tenantContextAccessor.Current.TenantId == tenant.Id)
            {
                _tenantContextAccessor.SetTenant(tenant);
                _brandingProvider.NotifyBrandingChanged();
            }
        }
    }

    private async Task<BrandingUploadResult> SaveFileDirectlyAsync(
        string slug,
        string imageType,
        string extension,
        MemoryStream memoryStream,
        CancellationToken cancellationToken)
    {
        var uploadDir = GetUploadDirectory(slug);
        Directory.CreateDirectory(uploadDir);

        // Delete existing files of this type
        DeleteExistingFiles(uploadDir, imageType);

        var fileName = $"{imageType}{extension}";
        var filePath = Path.Combine(uploadDir, fileName);

        await File.WriteAllBytesAsync(filePath, memoryStream.ToArray(), cancellationToken);

        var relativeUrl = $"/uploads/tenants/{slug}/{fileName}";
        _logger.LogInformation("Saved branding file: {Url}", relativeUrl);

        return new BrandingUploadResult(true, relativeUrl, null, false, 0, 0);
    }

    private async Task<BrandingUploadResult> ProcessAndSaveImageAsync(
        string slug,
        string imageType,
        string extension,
        MemoryStream memoryStream,
        ImageDimensionRecommendation config,
        CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(memoryStream, cancellationToken);

        var originalWidth = image.Width;
        var originalHeight = image.Height;
        var wasResized = false;

        // Check if resize is needed
        if (image.Width > config.MaxWidth || image.Height > config.MaxHeight)
        {
            // Calculate aspect-ratio preserving dimensions
            var ratioX = (double)config.MaxWidth / image.Width;
            var ratioY = (double)config.MaxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));
            wasResized = true;

            _logger.LogInformation(
                "Resized {ImageType} from {OrigW}x{OrigH} to {NewW}x{NewH}",
                imageType, originalWidth, originalHeight, newWidth, newHeight);
        }

        var uploadDir = GetUploadDirectory(slug);
        Directory.CreateDirectory(uploadDir);

        // Delete existing files of this type
        DeleteExistingFiles(uploadDir, imageType);

        // Determine output format (prefer WebP for better compression, fallback to original)
        var outputExtension = extension;
        var fileName = $"{imageType}{outputExtension}";
        var filePath = Path.Combine(uploadDir, fileName);

        // Save with appropriate encoder
        await using var outputStream = File.Create(filePath);
        await SaveImageWithFormatAsync(image, outputStream, extension, cancellationToken);

        var relativeUrl = $"/uploads/tenants/{slug}/{fileName}";
        _logger.LogInformation("Saved processed branding file: {Url} (resized: {Resized})", relativeUrl, wasResized);

        return new BrandingUploadResult(true, relativeUrl, null, wasResized, image.Width, image.Height);
    }

    private static async Task SaveImageWithFormatAsync(
        Image image,
        Stream outputStream,
        string extension,
        CancellationToken cancellationToken)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".png":
                await image.SaveAsPngAsync(outputStream, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }, cancellationToken);
                break;
            case ".jpg":
            case ".jpeg":
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 }, cancellationToken);
                break;
            case ".webp":
                await image.SaveAsWebpAsync(outputStream, new WebpEncoder { Quality = 85 }, cancellationToken);
                break;
            default:
                await image.SaveAsPngAsync(outputStream, cancellationToken);
                break;
        }
    }

    private string GetUploadDirectory(string slug)
    {
        return Path.Combine(_environment.WebRootPath, "uploads", "tenants", slug);
    }

    private void DeleteExistingFiles(string directory, string imageType)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var pattern = $"{imageType}.*";
        var existingFiles = Directory.GetFiles(directory, pattern);
        foreach (var file in existingFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete existing file: {File}", file);
            }
        }
    }
}
