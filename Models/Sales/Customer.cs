using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Financial;
using erp.Models.Identity;

namespace erp.Models.Sales;

/// <summary>
/// Represents a customer in the sales system
/// </summary>
public class Customer : IAuditable
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(14)]
    public string Document { get; set; } = string.Empty; // CPF or CNPJ
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? TradeName { get; set; } // Nome Fantasia
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? Mobile { get; set; }
    
    // Registro
    [MaxLength(20)]
    public string? StateRegistration { get; set; } // IE
    
    [MaxLength(20)]
    public string? MunicipalRegistration { get; set; } // IM
    
    // Endereço
    [MaxLength(10)]
    public string? ZipCode { get; set; }
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(10)]
    public string? Number { get; set; }
    
    [MaxLength(100)]
    public string? Complement { get; set; }
    
    [MaxLength(100)]
    public string? Neighborhood { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(2)]
    public string? State { get; set; }
    
    [MaxLength(100)]
    public string Country { get; set; } = "Brasil";
    
    [MaxLength(200)]
    public string? Website { get; set; }
    
    // Financeiro
    public decimal CreditLimit { get; set; } = 0;
    public int PaymentTermDays { get; set; } = 30;
    
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Dinheiro";
    
    // Tipo de Cliente
    public CustomerType Type { get; set; } = CustomerType.Individual;
    
    // Observações
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public int? CreatedByUserId { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<AccountReceivable> AccountsReceivable { get; set; } = new List<AccountReceivable>();
}

public enum CustomerType
{
    Individual = 0, // Pessoa Física
    Business = 1    // Pessoa Jurídica
}
