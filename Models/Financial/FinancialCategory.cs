using System.ComponentModel.DataAnnotations;
using erp.Models;

namespace erp.Models.Financial;

/// <summary>
/// Represents a financial category for revenue or expenses
/// </summary>
public class FinancialCategory : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty; // Ex: 1.1.01
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public CategoryType Type { get; set; }
    
    // Hierarquia
    public int? ParentCategoryId { get; set; }
    public virtual FinancialCategory? ParentCategory { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<FinancialCategory> SubCategories { get; set; } = new List<FinancialCategory>();
    public virtual ICollection<AccountReceivable> AccountsReceivable { get; set; } = new List<AccountReceivable>();
    public virtual ICollection<AccountPayable> AccountsPayable { get; set; } = new List<AccountPayable>();
}

public enum CategoryType
{
    Revenue = 0,  // Receita
    Expense = 1   // Despesa
}
