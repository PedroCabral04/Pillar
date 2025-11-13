using System;

namespace erp.Models.Audit;

/// <summary>
/// Atributo para marcar métodos que devem ter suas leituras auditadas.
/// Útil para compliance com LGPD/GDPR ao rastrear acesso a dados sensíveis.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class AuditReadAttribute : Attribute
{
    /// <summary>
    /// Nome da entidade sendo lida (ex: "Customer", "ApplicationUser")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nível de sensibilidade dos dados (baixo, médio, alto)
    /// </summary>
    public DataSensitivity Sensitivity { get; set; } = DataSensitivity.Medium;
    
    /// <summary>
    /// Descrição da operação de leitura (ex: "Visualização de CPF", "Consulta de dados bancários")
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Se verdadeiro, captura os parâmetros da requisição
    /// </summary>
    public bool IncludeParameters { get; set; } = true;
    
    public AuditReadAttribute()
    {
    }
    
    public AuditReadAttribute(string entityName)
    {
        EntityName = entityName;
    }
    
    public AuditReadAttribute(string entityName, DataSensitivity sensitivity)
    {
        EntityName = entityName;
        Sensitivity = sensitivity;
    }
}

/// <summary>
/// Níveis de sensibilidade de dados conforme LGPD/GDPR
/// </summary>
public enum DataSensitivity
{
    /// <summary>
    /// Dados não sensíveis (ex: nome público, produtos)
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Dados sensíveis padrão (ex: email, telefone)
    /// </summary>
    Medium = 1,
    
    /// <summary>
    /// Dados altamente sensíveis (ex: CPF, RG, dados bancários, médicos)
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Dados críticos (ex: senhas, tokens, dados financeiros completos)
    /// </summary>
    Critical = 3
}
