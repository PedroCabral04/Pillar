using System.ComponentModel.DataAnnotations;
using erp.Models.Sales;

namespace erp.DTOs.Sales;

public class CustomerDto
{
    public int Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }
    public string? ZipCode { get; set; }
    public string? Address { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "Brasil";
    public string? Website { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermDays { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Documento é obrigatório")]
    [StringLength(14, ErrorMessage = "Documento deve ter no máximo 14 caracteres")]
    public string Document { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? TradeName { get; set; }
    
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(20)]
    public string? Mobile { get; set; }
    
    [StringLength(20)]
    public string? StateRegistration { get; set; }
    
    [StringLength(20)]
    public string? MunicipalRegistration { get; set; }
    
    [StringLength(10)]
    public string? ZipCode { get; set; }
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [StringLength(10)]
    public string? Number { get; set; }
    
    [StringLength(100)]
    public string? Complement { get; set; }
    
    [StringLength(100)]
    public string? Neighborhood { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(2)]
    public string? State { get; set; }
    
    [StringLength(100)]
    public string Country { get; set; } = "Brasil";
    
    [StringLength(200)]
    public string? Website { get; set; }
    
    public decimal CreditLimit { get; set; } = 0;
    public int PaymentTermDays { get; set; } = 30;
    
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Dinheiro";
    
    public CustomerType Type { get; set; } = CustomerType.Individual;
    public string? Notes { get; set; }
}

public class UpdateCustomerDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? TradeName { get; set; }
    
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(20)]
    public string? Mobile { get; set; }
    
    [StringLength(20)]
    public string? StateRegistration { get; set; }
    
    [StringLength(20)]
    public string? MunicipalRegistration { get; set; }
    
    [StringLength(10)]
    public string? ZipCode { get; set; }
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [StringLength(10)]
    public string? Number { get; set; }
    
    [StringLength(100)]
    public string? Complement { get; set; }
    
    [StringLength(100)]
    public string? Neighborhood { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(2)]
    public string? State { get; set; }
    
    [StringLength(100)]
    public string Country { get; set; } = "Brasil";
    
    [StringLength(200)]
    public string? Website { get; set; }
    
    public decimal CreditLimit { get; set; } = 0;
    public int PaymentTermDays { get; set; } = 30;
    
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Dinheiro";
    
    public CustomerType Type { get; set; } = CustomerType.Individual;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
