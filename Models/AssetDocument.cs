using erp.Models.Identity;

namespace erp.Models;

/// <summary>
/// Documento anexado a um ativo (nota fiscal, garantia, manual, etc.)
/// </summary>
public class AssetDocument : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    public int AssetId { get; set; }
    
    /// <summary>
    /// Tipo de documento: PurchaseOrder, Warranty, Manual, Invoice, Contract, Certificate, Photo, Other
    /// </summary>
    public AssetDocumentType Type { get; set; }
    
    /// <summary>
    /// Nome do arquivo
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome original do arquivo
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Caminho do arquivo no servidor ou URL
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo MIME do arquivo
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Descrição do documento
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Número do documento (ex: número da NF, número do contrato)
    /// </summary>
    public string? DocumentNumber { get; set; }
    
    /// <summary>
    /// Data do documento
    /// </summary>
    public DateTime? DocumentDate { get; set; }
    
    /// <summary>
    /// Data de validade (para garantias, certificados)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// Usuário que fez o upload
    /// </summary>
    public int UploadedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;
}

public enum AssetDocumentType
{
    PurchaseOrder,    // Ordem de compra
    Warranty,         // Garantia
    Manual,           // Manual
    Invoice,          // Nota fiscal
    Contract,         // Contrato
    Certificate,      // Certificado
    Photo,            // Foto
    Other             // Outro
}
