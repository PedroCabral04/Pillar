using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.ServiceOrders;

/// <summary>
/// Anexo de imagem/documento vinculado a uma ordem de serviço (não impresso)
/// </summary>
public class ServiceOrderAttachment : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int ServiceOrderId { get; set; }

    /// <summary>
    /// Nome do arquivo salvo no servidor
    /// </summary>
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Nome original do arquivo no upload
    /// </summary>
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Caminho relativo do arquivo no storage
    /// </summary>
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Tipo MIME do arquivo
    /// </summary>
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Descrição opcional do anexo
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Usuário que fez o upload
    /// </summary>
    public int UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ServiceOrder ServiceOrder { get; set; } = null!;
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;
}
