using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace erp.Models.Audit;

/// <summary>
/// Entidade que armazena logs de auditoria de todas as operações no sistema
/// </summary>
public class AuditLog
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Nome da entidade auditada (ex: "Product", "Customer", "Sale")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID da entidade auditada
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de ação realizada
    /// </summary>
    [Required]
    [MaxLength(20)]
    public AuditAction Action { get; set; }
    
    /// <summary>
    /// ID do usuário que realizou a ação
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Nome do usuário (armazenado para histórico mesmo se usuário for deletado)
    /// </summary>
    [MaxLength(200)]
    public string? UserName { get; set; }
    
    /// <summary>
    /// Dados antigos da entidade em formato JSON (null para Create)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? OldValues { get; set; }
    
    /// <summary>
    /// Dados novos da entidade em formato JSON (null para Delete)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; }
    
    /// <summary>
    /// Lista das propriedades que foram alteradas
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ChangedProperties { get; set; }
    
    /// <summary>
    /// Timestamp da operação (UTC)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Endereço IP de onde a operação foi realizada
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User Agent do navegador/cliente
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Informações adicionais sobre o contexto da operação
    /// </summary>
    [MaxLength(1000)]
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Tipos de ações auditáveis
/// </summary>
public enum AuditAction
{
    Create,
    Update,
    Delete,
    Read  // Auditoria de leitura (LGPD/GDPR compliance)
}
