using erp.Models.Identity;

namespace erp.Models;

/// <summary>
/// Transferência de ativo entre localizações/departamentos
/// </summary>
public class AssetTransfer : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    public int AssetId { get; set; }
    
    /// <summary>
    /// Localização de origem
    /// </summary>
    public string FromLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// Departamento de origem
    /// </summary>
    public int? FromDepartmentId { get; set; }
    
    /// <summary>
    /// Localização de destino
    /// </summary>
    public string ToLocation { get; set; } = string.Empty;
    
    /// <summary>
    /// Departamento de destino
    /// </summary>
    public int? ToDepartmentId { get; set; }
    
    /// <summary>
    /// Data da transferência
    /// </summary>
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Motivo da transferência
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Condição do ativo na transferência
    /// </summary>
    public AssetCondition Condition { get; set; }
    
    /// <summary>
    /// Observações
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Status: Pending, InTransit, Completed, Cancelled
    /// </summary>
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    
    /// <summary>
    /// Usuário que solicitou a transferência
    /// </summary>
    public int RequestedByUserId { get; set; }
    
    /// <summary>
    /// Usuário que aprovou a transferência
    /// </summary>
    public int? ApprovedByUserId { get; set; }
    
    /// <summary>
    /// Data de aprovação
    /// </summary>
    public DateTime? ApprovedDate { get; set; }
    
    /// <summary>
    /// Usuário que completou a transferência
    /// </summary>
    public int? CompletedByUserId { get; set; }
    
    /// <summary>
    /// Data de conclusão
    /// </summary>
    public DateTime? CompletedDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    
    public virtual Department? FromDepartment { get; set; }
    
    public virtual Department? ToDepartment { get; set; }
    
    public virtual ApplicationUser RequestedByUser { get; set; } = null!;
    
    public virtual ApplicationUser? ApprovedByUser { get; set; }
    
    public virtual ApplicationUser? CompletedByUser { get; set; }
}

public enum TransferStatus
{
    Pending,      // Pendente
    InTransit,    // Em trânsito
    Completed,    // Concluída
    Cancelled     // Cancelada
}
