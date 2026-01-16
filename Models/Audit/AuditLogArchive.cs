using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace erp.Models.Audit;

/// <summary>
/// Archived audit logs for long-term storage.
/// Records are moved here from AuditLogs after the retention period.
/// </summary>
public class AuditLogArchive
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// ID of the original AuditLog entry
    /// </summary>
    public long OriginalAuditLogId { get; set; }

    /// <summary>
    /// ID do tenant ao qual este log pertence (multi-tenancy)
    /// </summary>
    public int TenantId { get; set; }

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
    /// Descrição legível da entidade (ex: nome do produto, nome do cliente)
    /// </summary>
    [MaxLength(500)]
    public string? EntityDescription { get; set; }

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
    /// Lista das propriedades que foram alteradas com valores legíveis
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// Referências legíveis para FKs
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? References { get; set; }

    /// <summary>
    /// Timestamp da operação original (UTC)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Timestamp do arquivamento (UTC)
    /// </summary>
    [Required]
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Endereço IP de onde a operação foi realizada
    /// </summary>
    [MaxLength(45)]
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
