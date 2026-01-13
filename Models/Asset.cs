namespace erp.Models;

/// <summary>
/// Ativo da empresa (equipamento, móvel, veículo, etc.)
/// </summary>
public class Asset : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    /// <summary>
    /// Código único do ativo (ex: COMP-001, VEI-042)
    /// </summary>
    public string AssetCode { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int CategoryId { get; set; }
    
    /// <summary>
    /// Número de série ou identificação única do fabricante
    /// </summary>
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Fabricante/Marca
    /// </summary>
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Modelo
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Data de aquisição
    /// </summary>
    public DateTime? PurchaseDate { get; set; }
    
    /// <summary>
    /// Valor de aquisição
    /// </summary>
    public decimal? PurchaseValue { get; set; }
    
    /// <summary>
    /// Fornecedor que vendeu o ativo
    /// </summary>
    public int? SupplierId { get; set; }
    
    /// <summary>
    /// Nota fiscal de compra
    /// </summary>
    public string? InvoiceNumber { get; set; }
    
    /// <summary>
    /// Status: Available, InUse, Maintenance, Retired, Lost, Sold
    /// </summary>
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    
    /// <summary>
    /// Localização física do ativo
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Condição: Excellent, Good, Fair, Poor, Damaged
    /// </summary>
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    
    /// <summary>
    /// Data de garantia até
    /// </summary>
    public DateTime? WarrantyExpiryDate { get; set; }
    
    /// <summary>
    /// Vida útil esperada em meses
    /// </summary>
    public int? ExpectedLifespanMonths { get; set; }
    
    /// <summary>
    /// Observações gerais
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// URL da imagem do ativo
    /// </summary>
    public string? ImageUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual AssetCategory Category { get; set; } = null!;
    
    public virtual ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
    
    public virtual ICollection<AssetMaintenance> MaintenanceRecords { get; set; } = new List<AssetMaintenance>();
    
    public virtual ICollection<AssetDocument> Documents { get; set; } = new List<AssetDocument>();
    
    public virtual ICollection<AssetTransfer> Transfers { get; set; } = new List<AssetTransfer>();
}

public enum AssetStatus
{
    Available,    // Disponível para uso
    InUse,        // Em uso
    Maintenance,  // Em manutenção
    Retired,      // Aposentado/Desativado
    Lost,         // Perdido
    Sold          // Vendido
}

public enum AssetCondition
{
    Excellent,    // Excelente
    Good,         // Bom
    Fair,         // Regular
    Poor,         // Ruim
    Damaged       // Danificado
}
