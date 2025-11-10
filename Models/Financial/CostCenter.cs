using System.ComponentModel.DataAnnotations;
using erp.Models.Identity;

namespace erp.Models.Financial;

/// <summary>
/// Represents a cost center for financial tracking
/// </summary>
public class CostCenter
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int? ManagerUserId { get; set; }
    public virtual ApplicationUser? Manager { get; set; }
    
    public decimal? MonthlyBudget { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<AccountReceivable> AccountsReceivable { get; set; } = new List<AccountReceivable>();
    public virtual ICollection<AccountPayable> AccountsPayable { get; set; } = new List<AccountPayable>();
}
