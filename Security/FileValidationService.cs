namespace erp.Security;

/// <summary>
/// Serviço para validação de segurança de arquivos uploadados
/// Implementa whitelist de tipos, validação de magic bytes e proteção contra path traversal
/// </summary>
public interface IFileValidationService
{
    Task<FileValidationResult> ValidateFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    bool IsAllowedExtension(string fileName);
    string[] GetAllowedExtensions();
}

public record FileValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    string? DetectedMimeType = null
);

/// <summary>
/// Define tipos de arquivo permitidos com seus magic bytes (assinaturas de arquivo)
/// </summary>
public record FileTypeDefinition(
    string Extension,
    string MimeType,
    byte[][] MagicSignatures
);

public class FileValidationService : IFileValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileValidationService> _logger;
    private readonly long _maxFileSizeBytes;

    // Tipos de arquivo permitidos com suas assinaturas magic bytes
    private static readonly FileTypeDefinition[] AllowedFileTypes = new[]
    {
        // Imagens
        new FileTypeDefinition(".jpg", "image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }),
        new FileTypeDefinition(".jpeg", "image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }),
        new FileTypeDefinition(".png", "image/png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } }),
        new FileTypeDefinition(".gif", "image/gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } }),
        new FileTypeDefinition(".webp", "image/webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }), // RIFF
        new FileTypeDefinition(".bmp", "image/bmp", new[] { new byte[] { 0x42, 0x4D } }),
        new FileTypeDefinition(".svg", "image/svg+xml", Array.Empty<byte[]>()), // SVG é texto, requer validação diferente

        // Documentos PDF
        new FileTypeDefinition(".pdf", "application/pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }),

        // Documentos Office (antigo formato)
        new FileTypeDefinition(".doc", "application/msword", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }),
        new FileTypeDefinition(".xls", "application/vnd.ms-excel", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }),
        new FileTypeDefinition(".ppt", "application/vnd.ms-powerpoint", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }),

        // Documentos Office (novo formato OOXML)
        new FileTypeDefinition(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }), // ZIP
        new FileTypeDefinition(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }),
        new FileTypeDefinition(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }),

        // Arquivos de texto
        new FileTypeDefinition(".txt", "text/plain", Array.Empty<byte[]>()),
        new FileTypeDefinition(".csv", "text/csv", Array.Empty<byte[]>()),
        new FileTypeDefinition(".json", "application/json", Array.Empty<byte[]>()),
        new FileTypeDefinition(".xml", "application/xml", Array.Empty<byte[]>()),
    };

    public FileValidationService(IConfiguration configuration, ILogger<FileValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeMB", 10) * 1024 * 1024;
    }

    public string[] GetAllowedExtensions()
    {
        return AllowedFileTypes.Select(x => x.Extension).ToArray();
    }

    public bool IsAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedFileTypes.Any(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return new FileValidationResult(false, "Arquivo não fornecido ou vazio.");
        }

        // 1. Validação de tamanho
        if (file.Length > _maxFileSizeBytes)
        {
            var maxSizeMB = _maxFileSizeBytes / (1024.0 * 1024.0);
            return new FileValidationResult(false,
                $"Arquivo muito grande. Tamanho máximo permitido: {maxSizeMB:F0}MB");
        }

        // 2. Validação de extensão (whitelist)
        var fileName = SanitizeFileName(file.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedFileTypes.Any(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return new FileValidationResult(false,
                $"Tipo de arquivo não permitido. Extensões aceitas: {string.Join(", ", GetAllowedExtensions())}");
        }

        // 3. Validação de magic bytes (assinatura de arquivo)
        try
        {
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[16]; // Lê primeiros 16 bytes para identificar o tipo

            var bytesRead = await stream.ReadAsync(headerBytes, 0, 16, cancellationToken);
            stream.Position = 0; // Reset stream para leitura posterior

            if (bytesRead > 0)
            {
                var fileType = AllowedFileTypes.FirstOrDefault(x =>
                    x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));

                if (fileType != null && fileType.MagicSignatures.Any())
                {
                    var isValidMagicBytes = fileType.MagicSignatures.Any(signature =>
                        headerBytes.Take(signature.Length).SequenceEqual(signature));

                    if (!isValidMagicBytes)
                    {
                        _logger.LogWarning(
                            "Arquivo com extensão {Extension} possui magic bytes inválidos. FileName: {FileName}",
                            extension, fileName);

                        return new FileValidationResult(false,
                            $"O conteúdo do arquivo não corresponde à extensão {extension}. O arquivo pode estar corrompido ou malicioso.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar magic bytes do arquivo {FileName}", file.FileName);
            return new FileValidationResult(false, "Falha na validação de segurança do arquivo.");
        }

        // 4. Validação adicional para SVG (arquivos de texto que podem conter scripts maliciosos)
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var svgValidation = await ValidateSvgContentAsync(file, cancellationToken);
            if (!svgValidation.IsValid)
            {
                return svgValidation;
            }
        }

        return new FileValidationResult(true, DetectedMimeType: AllowedFileTypes
            .First(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)).MimeType);
    }

    /// <summary>
    /// Validação específica para SVG pois podem conter scripts JavaScript maliciosos
    /// </summary>
    private async Task<FileValidationResult> ValidateSvgContentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync(cancellationToken);

            // Verifica por tags de script ou handlers de eventos perigosos
            var dangerousPatterns = new[]
            {
                "<script",
                "javascript:",
                "onload=",
                "onerror=",
                "onclick=",
                "onmouseover=",
                "eval(",
                "data:text/html"
            };

            var lowerContent = content.ToLowerInvariant();
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerContent.Contains(pattern.ToLowerInvariant()))
                {
                    return new FileValidationResult(false,
                        $"O arquivo SVG contém elementos potencialmente perigosos: {pattern}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar conteúdo SVG");
            return new FileValidationResult(false, "Erro ao validar conteúdo do arquivo SVG.");
        }

        return new FileValidationResult(true);
    }

    /// <summary>
    /// Sanitiza o nome do arquivo removendo caracteres perigosos e prevenindo path traversal
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        // Remove path traversal attempts
        fileName = fileName.Replace("..", "").Replace("\\", "").Replace("/", "");

        // Remove caracteres inválidos
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c) && c < 128) // Remove caracteres não-ASCII
            .ToArray());

        // Remove espaços e pontos extras no início/fim
        sanitized = sanitized.Trim('.', ' ');

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
