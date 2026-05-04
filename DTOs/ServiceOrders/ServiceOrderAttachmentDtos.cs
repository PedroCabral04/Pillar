namespace erp.DTOs.ServiceOrders;

/// <summary>
/// DTO para representação de anexo de ordem de serviço
/// </summary>
public class ServiceOrderAttachmentDto
{
    public int Id { get; set; }
    public int ServiceOrderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public int UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para upload de anexo via multipart/form-data
/// </summary>
public class UploadServiceOrderAttachmentDto
{
    public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;
    public string? Description { get; set; }
}
