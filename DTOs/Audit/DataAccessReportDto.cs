using erp.Models.Audit;

namespace erp.DTOs.Audit;

/// <summary>
/// DTO para relatório de acesso a dados sensíveis (LGPD/GDPR compliance)
/// </summary>
public class DataAccessReportDto
{
    public long Id { get; set; }
    
    /// <summary>
    /// Nome da entidade acessada
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID da entidade acessada
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nível de sensibilidade dos dados
    /// </summary>
    public DataSensitivity Sensitivity { get; set; }
    
    /// <summary>
    /// Descrição do tipo de acesso
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// ID do usuário que acessou
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Nome do usuário
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Endereço IP de origem
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Data e hora do acesso
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Parâmetros da requisição (se disponível)
    /// </summary>
    public string? Parameters { get; set; }
}
