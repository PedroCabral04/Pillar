using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp.Models.Tenancy;
using erp.Models.Audit;

namespace erp.Models.Identity;

public class ApplicationUser : IdentityUser<int>, erp.Models.Audit.IAuditable
{
    public bool IsActive { get; set; } = true;
    
    [AuditExclude("Dados de preferências do usuário")]
    public string? PreferencesJson { get; set; }
    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public ICollection<TenantMembership> TenantMemberships { get; set; } = new List<TenantMembership>();
    
    // Informações Pessoais
    [MaxLength(200)]
    public string? FullName { get; set; }

    // Backwards compatibility: older code/tests may reference 'Name'
    // Map transparently to FullName without creating a new column
    [NotMapped]
    [Obsolete("Use FullName instead of Name.")]
    public string? Name { get => FullName; set => FullName = value; }
    
    [MaxLength(14)]
    [AuditExclude("Dado pessoal sensível - CPF")]
    public string? Cpf { get; set; }
    
    [MaxLength(20)]
    [AuditExclude("Dado pessoal sensível - RG")]
    public string? Rg { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    [MaxLength(20)]
    public string? Gender { get; set; } // Masculino, Feminino, Outro, Prefiro não informar
    
    [MaxLength(50)]
    public string? MaritalStatus { get; set; } // Solteiro, Casado, Divorciado, Viúvo, União Estável
    
    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }
    
    // Endereço
    [MaxLength(9)]
    public string? PostalCode { get; set; }
    
    [MaxLength(200)]
    public string? Street { get; set; }
    
    [MaxLength(20)]
    public string? Number { get; set; }
    
    [MaxLength(100)]
    public string? Complement { get; set; }
    
    [MaxLength(100)]
    public string? Neighborhood { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(2)]
    public string? State { get; set; }
    
    [MaxLength(50)]
    public string? Country { get; set; }
    
    // Informações Profissionais
    public int? DepartmentId { get; set; }
    
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }
    
    public int? PositionId { get; set; }
    
    [ForeignKey(nameof(PositionId))]
    public Position? Position { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    [AuditExclude("Dado sensível - Salário")]
    public decimal? Salary { get; set; }
    
    public DateTime? HireDate { get; set; }
    
    public DateTime? TerminationDate { get; set; }
    
    [MaxLength(50)]
    public string? ContractType { get; set; } // CLT, PJ, Estágio, Temporário, etc.
    
    [MaxLength(50)]
    public string? EmploymentStatus { get; set; } // Ativo, Férias, Afastado, Demitido, etc.
    
    // Informações Bancárias
    [MaxLength(10)]
    [AuditExclude("Dado bancário sensível")]
    public string? BankCode { get; set; }
    
    [MaxLength(100)]
    [AuditExclude("Dado bancário sensível")]
    public string? BankName { get; set; }
    
    [MaxLength(10)]
    [AuditExclude("Dado bancário sensível")]
    public string? BankAgency { get; set; }
    
    [MaxLength(20)]
    [AuditExclude("Dado bancário sensível")]
    public string? BankAccount { get; set; }
    
    [MaxLength(10)]
    [AuditExclude("Dado bancário sensível")]
    public string? BankAccountType { get; set; } // Corrente, Poupança
    
    [MaxLength(14)]
    [AuditExclude("Dado bancário sensível - CPF titular")]
    public string? BankAccountCpf { get; set; }
    
    // Informações de Emergência
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }
    
    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; set; }
    
    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }
    
    // Documentos e Certificações
    [MaxLength(20)]
    [AuditExclude("Documento trabalhista sensível")]
    public string? WorkCard { get; set; } // Carteira de Trabalho
    
    [MaxLength(20)]
    [AuditExclude("Documento trabalhista sensível")]
    public string? PisNumber { get; set; }
    
    [MaxLength(200)]
    public string? EducationLevel { get; set; }

    /// <summary>
    /// Número de dependentes para fins de cálculo de IRRF na folha de pagamento.
    /// Cada dependente permite uma dedução de R$ 189,59 no cálculo do IRRF.
    /// </summary>
    public int DependentCount { get; set; } = 0;

    public string? Certifications { get; set; } // JSON array de certificações
    
    // Observações e Metadados
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public int? CreatedById { get; set; }
    
    public int? UpdatedById { get; set; }
}
