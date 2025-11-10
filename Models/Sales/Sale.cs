using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using erp.Models.Identity;
using erp.Models.Audit;

namespace erp.Models.Sales;

/// <summary>
/// Represents a sale transaction
/// </summary>
public class Sale : IAuditable
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string SaleNumber { get; set; } = string.Empty;
    
    public int? CustomerId { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }
    
    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }
    
    [Precision(18, 2)]
    public decimal NetAmount { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pendente"; // Pendente, Finalizada, Cancelada
    
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Customer? Customer { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
    
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
