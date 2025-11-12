using erp.Models;

namespace erp.DTOs.Assets;

// ============= Category DTOs =============

public class AssetCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssetCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class UpdateAssetCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
}

// ============= Asset DTOs =============

public class AssetDto
{
    public int Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public int? SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public AssetStatus Status { get; set; }
    public string? Location { get; set; }
    public AssetCondition Condition { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int? ExpectedLifespanMonths { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Current assignment info (if assigned)
    public int? CurrentAssignmentId { get; set; }
    public string? CurrentAssignedToUserName { get; set; }
    public DateTime? CurrentAssignedDate { get; set; }
}

public class CreateAssetDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public int? SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public AssetStatus Status { get; set; }
    public string? Location { get; set; }
    public AssetCondition Condition { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int? ExpectedLifespanMonths { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateAssetDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public int? SupplierId { get; set; }
    public string? InvoiceNumber { get; set; }
    public AssetStatus Status { get; set; }
    public string? Location { get; set; }
    public AssetCondition Condition { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int? ExpectedLifespanMonths { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}

// ============= Assignment DTOs =============

public class AssetAssignmentDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public AssetCondition ConditionOnAssignment { get; set; }
    public AssetCondition? ConditionOnReturn { get; set; }
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public int AssignedByUserId { get; set; }
    public string AssignedByUserName { get; set; } = string.Empty;
    public int? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssetAssignmentDto
{
    public int AssetId { get; set; }
    public int AssignedToUserId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public AssetCondition ConditionOnAssignment { get; set; } = AssetCondition.Good;
    public string? AssignmentNotes { get; set; }
}

public class ReturnAssetDto
{
    public DateTime ReturnedDate { get; set; } = DateTime.UtcNow;
    public AssetCondition ConditionOnReturn { get; set; }
    public string? ReturnNotes { get; set; }
}

// ============= Maintenance DTOs =============

public class AssetMaintenanceDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public MaintenanceType Type { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ServiceDetails { get; set; }
    public string? ServiceProvider { get; set; }
    public decimal? Cost { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public int? CompletedByUserId { get; set; }
    public string? CompletedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssetMaintenanceDto
{
    public int AssetId { get; set; }
    public MaintenanceType Type { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ServiceProvider { get; set; }
    public decimal? Cost { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAssetMaintenanceDto
{
    public MaintenanceType Type { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ServiceDetails { get; set; }
    public string? ServiceProvider { get; set; }
    public decimal? Cost { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
}

// ============= Statistics DTOs =============

public class AssetStatisticsDto
{
    public int TotalAssets { get; set; }
    public int AvailableAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int InMaintenanceAssets { get; set; }
    public int RetiredAssets { get; set; }
    public decimal TotalAssetValue { get; set; }
    public int ScheduledMaintenances { get; set; }
    public int OverdueMaintenances { get; set; }
    public Dictionary<string, int> AssetsByCategory { get; set; } = new();
    public Dictionary<string, int> AssetsByStatus { get; set; } = new();
    public Dictionary<string, int> AssetsByCondition { get; set; } = new();
}
