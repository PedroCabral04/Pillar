using System.ComponentModel.DataAnnotations;

namespace erp.Models.Sales;

/// <summary>
/// Represents a customer in the sales system
/// </summary>
public class Customer
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(14)]
    public string Document { get; set; } = string.Empty; // CPF or CNPJ
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(20)]
    public string? Mobile { get; set; }
    
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
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
