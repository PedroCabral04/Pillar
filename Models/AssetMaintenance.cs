using erp.Models.Identity;

namespace erp.Models;

/// <summary>
/// Registro de manutenção de ativos
/// </summary>
public class AssetMaintenance : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    public int AssetId { get; set; }
    
    /// <summary>
    /// Tipo de manutenção: Preventive, Corrective, Emergency, Inspection
    /// </summary>
    public MaintenanceType Type { get; set; }
    
    /// <summary>
    /// Status: Scheduled, InProgress, Completed, Cancelled
    /// </summary>
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
    
    /// <summary>
    /// Data agendada para manutenção
    /// </summary>
    public DateTime ScheduledDate { get; set; }
    
    /// <summary>
    /// Data de início da manutenção
    /// </summary>
    public DateTime? StartedDate { get; set; }
    
    /// <summary>
    /// Data de conclusão da manutenção
    /// </summary>
    public DateTime? CompletedDate { get; set; }
    
    /// <summary>
    /// Descrição do serviço/problema
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Detalhes do serviço realizado
    /// </summary>
    public string? ServiceDetails { get; set; }
    
    /// <summary>
    /// Fornecedor/Técnico responsável
    /// </summary>
    public string? ServiceProvider { get; set; }
    
    /// <summary>
    /// Custo da manutenção
    /// </summary>
    public decimal? Cost { get; set; }
    
    /// <summary>
    /// Nota fiscal do serviço
    /// </summary>
    public string? InvoiceNumber { get; set; }
    
    /// <summary>
    /// Próxima manutenção preventiva (se aplicável)
    /// </summary>
    public DateTime? NextMaintenanceDate { get; set; }
    
    /// <summary>
    /// Observações gerais
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Usuário que criou o registro
    /// </summary>
    public int CreatedByUserId { get; set; }
    
    /// <summary>
    /// Usuário que aprovou/completou a manutenção
    /// </summary>
    public int? CompletedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Asset Asset { get; set; } = null!;
    
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
    
    public virtual ApplicationUser? CompletedByUser { get; set; }
}

public enum MaintenanceType
{
    Preventive,   // Preventiva
    Corrective,   // Corretiva
    Emergency,    // Emergencial
    Inspection    // Inspeção
}

public enum MaintenanceStatus
{
    Scheduled,    // Agendada
    InProgress,   // Em andamento
    Completed,    // Concluída
    Cancelled     // Cancelada
}
