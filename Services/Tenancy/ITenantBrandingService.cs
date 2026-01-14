namespace erp.Services.Tenancy;

/// <summary>
/// Service for handling tenant branding image uploads (logo, favicon).
/// </summary>
public interface ITenantBrandingService
{
    /// <summary>
    /// Uploads and processes a branding image (logo or favicon) for a tenant.
    /// Images exceeding maximum dimensions will be auto-resized.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="imageType">Type of image: "logo" or "favicon"</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileStream">The image file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The relative URL path to the uploaded image</returns>
    Task<BrandingUploadResult> UploadImageAsync(
        int tenantId,
        string imageType,
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a branding image for a tenant.
    /// </summary>
    Task DeleteImageAsync(int tenantId, string imageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the recommended dimensions for a branding image type.
    /// </summary>
    ImageDimensionRecommendation GetDimensionRecommendation(string imageType);
}

/// <summary>
/// Result of a branding image upload operation.
/// </summary>
public record BrandingUploadResult(
    bool Success,
    string? Url,
    string? ErrorMessage,
    bool WasResized,
    int FinalWidth,
    int FinalHeight
);

/// <summary>
/// Dimension recommendations for branding images.
/// </summary>
public record ImageDimensionRecommendation(
    int RecommendedWidth,
    int RecommendedHeight,
    int MaxWidth,
    int MaxHeight,
    long MaxFileSizeBytes,
    string[] AllowedExtensions,
    string Description
);
