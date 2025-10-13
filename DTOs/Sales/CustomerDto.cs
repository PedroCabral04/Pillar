using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Sales;

public class CustomerDto
{
    public int Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? ZipCode { get; set; }
    public string? Address { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Documento é obrigatório")]
    [StringLength(14, ErrorMessage = "Documento deve ter no máximo 14 caracteres")]
    public string Document { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(20)]
    public string? Mobile { get; set; }
    
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
}

public class UpdateCustomerDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(20)]
    public string? Mobile { get; set; }
    
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
    
    public bool IsActive { get; set; } = true;
}
