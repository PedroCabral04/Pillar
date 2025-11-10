using erp.Models.Financial;

namespace erp.DTOs.Financial;

/// <summary>
/// DTO for creating a financial category
/// </summary>
public class CreateFinancialCategoryDto
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public CategoryType Type { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a financial category
/// </summary>
public class UpdateFinancialCategoryDto : CreateFinancialCategoryDto
{
}

/// <summary>
/// DTO for financial category response
/// </summary>
public class FinancialCategoryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public CategoryType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Properties for tree view
    public HashSet<FinancialCategoryDto>? SubCategories { get; set; }
    public bool Expanded { get; set; }
}

/// <summary>
/// DTO for creating a cost center
/// </summary>
public class CreateCostCenterDto
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public int? ManagerUserId { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a cost center
/// </summary>
public class UpdateCostCenterDto : CreateCostCenterDto
{
}

/// <summary>
/// DTO for cost center response
/// </summary>
public class CostCenterDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public int? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public decimal? MonthlyBudget { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
