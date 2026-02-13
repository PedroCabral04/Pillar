namespace erp.Services.Assets;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string? subfolder = null);
    Task<Stream> GetFileAsync(string filePath);
    Task<byte[]> GetFileBytesAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    string GetFileUrl(string filePath);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly IConfiguration _configuration;
    
    public LocalFileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _basePath = configuration.GetValue<string>("FileStorage:BasePath") 
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assets");
        
        // Garante que o diretório base existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
    
    /// <summary>
    /// Salva um arquivo no sistema de arquivos local
    /// </summary>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string? subfolder = null)
    {
        // Gera um nome de arquivo único para evitar conflitos
        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        
        // Determina o caminho completo
        var folder = subfolder != null 
            ? Path.Combine(_basePath, subfolder) 
            : _basePath;
        
        // Cria a pasta se não existir
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        
        var fullPath = Path.Combine(folder, uniqueFileName);
        
        // Salva o arquivo
        using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }
        
        // Retorna o caminho relativo
        return subfolder != null 
            ? Path.Combine(subfolder, uniqueFileName) 
            : uniqueFileName;
    }
    
    /// <summary>
    /// Obtém um arquivo do sistema de arquivos
    /// </summary>
    public Task<Stream> GetFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Arquivo não encontrado", filePath);
        }

        // Return FileStream directly - caller must dispose using 'await using'
        var fileStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920, // 80KB buffer for better performance
            options: FileOptions.SequentialScan | FileOptions.Asynchronous
        );

        return Task.FromResult<Stream>(fileStream);
    }

    /// <summary>
    /// Obtém um arquivo como bytes (para operações na memória)
    /// </summary>
    public async Task<byte[]> GetFileBytesAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Arquivo não encontrado", filePath);
        }

        return await File.ReadAllBytesAsync(fullPath);
    }
    
    /// <summary>
    /// Exclui um arquivo do sistema de arquivos
    /// </summary>
    public Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Verifica se um arquivo existe
    /// </summary>
    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = Path.Combine(_basePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }
    
    /// <summary>
    /// Obtém a URL pública do arquivo
    /// </summary>
    public string GetFileUrl(string filePath)
    {
        // Retorna o caminho relativo que pode ser usado em URLs
        return $"/uploads/assets/{filePath.Replace("\\", "/")}";
    }
    
    /// <summary>
    /// Sanitiza o nome do arquivo removendo caracteres inválidos
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());
        
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
