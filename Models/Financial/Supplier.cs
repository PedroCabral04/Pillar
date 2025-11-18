using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;

using erp.Models.Tenancy;

namespace erp.Models.Financial;

/// <summary>
/// Represents a supplier in the financial system
/// </summary>
public class Supplier : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // Razão Social
    
    [MaxLength(200)]
    public string? TradeName { get; set; } // Nome Fantasia
    
    [Required]
    [MaxLength(14)]
    public string TaxId { get; set; } = string.Empty; // CNPJ/CPF
    
    [MaxLength(20)]
    public string? StateRegistration { get; set; } // IE
    
    [MaxLength(20)]
    public string? MunicipalRegistration { get; set; } // IM
    
    // Endereço
    [MaxLength(10)]
    public string? ZipCode { get; set; }
    
    [MaxLength(200)]
    public string? Street { get; set; }
    
    [MaxLength(10)]
    public string? Number { get; set; }
    
    [MaxLength(100)]
    public string? Complement { get; set; }
    
    [MaxLength(100)]
    public string? District { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(2)]
    public string? State { get; set; }
    
    [MaxLength(100)]
    public string Country { get; set; } = "Brasil";
    
    // Contato
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? MobilePhone { get; set; }
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    // Categorização
    [MaxLength(100)]
    public string? Category { get; set; } // Matéria-prima, Serviços, etc.
    
    // Financeiro
    public decimal MinimumOrderValue { get; set; } = 0;
    public int DeliveryLeadTimeDays { get; set; } = 0;
    public int PaymentTermDays { get; set; } = 30;
    
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Boleto";
    
    public bool IsPreferred { get; set; } = false;
    
    // Observações
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public int CreatedByUserId { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ICollection<AccountPayable> AccountsPayable { get; set; } = new List<AccountPayable>();
}
