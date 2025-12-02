namespace erp.Models.Audit;

/// <summary>
/// Atributo que marca propriedades que devem ser excluídas da auditoria.
/// Use em campos sensíveis como senhas, tokens, dados pessoais protegidos, etc.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AuditExcludeAttribute : Attribute
{
    /// <summary>
    /// Motivo opcional para exclusão (para documentação)
    /// </summary>
    public string? Reason { get; set; }
    
    public AuditExcludeAttribute() { }
    
    public AuditExcludeAttribute(string reason)
    {
        Reason = reason;
    }
}
