using erp.Models.Identity;

namespace erp.Models;

/// <summary>
/// Atribuição de ativo a um funcionário
/// </summary>
public class AssetAssignment
{
    public int Id { get; set; }
    
    public int AssetId { get; set; }
    
    /// <summary>
    /// Funcionário responsável pelo ativo
    /// </summary>
    public int AssignedToUserId { get; set; }
    
    /// <summary>
    /// Data de atribuição
    /// </summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data de devolução (null se ainda estiver com o funcionário)
    /// </summary>
    public DateTime? ReturnedDate { get; set; }
    
    /// <summary>
    /// Condição na entrega
    /// </summary>
    public AssetCondition ConditionOnAssignment { get; set; } = AssetCondition.Good;
    
    /// <summary>
    /// Condição na devolução
    /// </summary>
    public AssetCondition? ConditionOnReturn { get; set; }
    
    /// <summary>
    /// Observações da atribuição
    /// </summary>
    public string? AssignmentNotes { get; set; }
    
    /// <summary>
    /// Observações da devolução
    /// </summary>
    public string? ReturnNotes { get; set; }
    
    /// <summary>
    /// Usuário que registrou a atribuição
    /// </summary>
    public int AssignedByUserId { get; set; }
    
    /// <summary>
    /// Usuário que registrou a devolução
    /// </summary>
    public int? ReturnedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    
    public virtual ApplicationUser AssignedToUser { get; set; } = null!;
    
    public virtual ApplicationUser AssignedByUser { get; set; } = null!;
    
    public virtual ApplicationUser? ReturnedByUser { get; set; }
}
