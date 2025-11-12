using erp.DTOs.Assets;
using erp.Models;
using Riok.Mapperly.Abstractions;

namespace erp.Mappings;

[Mapper]
public partial class AssetMapper
{
    // ============= Category Mappings =============
    
    public partial AssetCategoryDto CategoryToDto(AssetCategory category);
    
    public partial AssetCategory CreateDtoToCategory(CreateAssetCategoryDto dto);
    
    public partial void UpdateCategoryFromDto(UpdateAssetCategoryDto dto, AssetCategory category);
    
    // ============= Asset Mappings =============
    
    [MapProperty(nameof(Asset.Category.Name), nameof(AssetDto.CategoryName))]
    public partial AssetDto AssetToDto(Asset asset);
    
    public partial Asset CreateDtoToAsset(CreateAssetDto dto);
    
    public partial void UpdateAssetFromDto(UpdateAssetDto dto, Asset asset);
    
    // ============= Assignment Mappings =============
    
    [MapProperty(nameof(AssetAssignment.Asset.Name), nameof(AssetAssignmentDto.AssetName))]
    [MapProperty(nameof(AssetAssignment.Asset.AssetCode), nameof(AssetAssignmentDto.AssetCode))]
    [MapProperty(nameof(AssetAssignment.AssignedToUser.FullName), nameof(AssetAssignmentDto.AssignedToUserName))]
    [MapProperty(nameof(AssetAssignment.AssignedByUser.FullName), nameof(AssetAssignmentDto.AssignedByUserName))]
    [MapProperty(nameof(AssetAssignment.ReturnedByUser.FullName), nameof(AssetAssignmentDto.ReturnedByUserName))]
    public partial AssetAssignmentDto AssignmentToDto(AssetAssignment assignment);
    
    public partial AssetAssignment CreateDtoToAssignment(CreateAssetAssignmentDto dto);
    
    // ============= Maintenance Mappings =============
    
    [MapProperty(nameof(AssetMaintenance.Asset.Name), nameof(AssetMaintenanceDto.AssetName))]
    [MapProperty(nameof(AssetMaintenance.Asset.AssetCode), nameof(AssetMaintenanceDto.AssetCode))]
    [MapProperty(nameof(AssetMaintenance.CreatedByUser.FullName), nameof(AssetMaintenanceDto.CreatedByUserName))]
    [MapProperty(nameof(AssetMaintenance.CompletedByUser.FullName), nameof(AssetMaintenanceDto.CompletedByUserName))]
    public partial AssetMaintenanceDto MaintenanceToDto(AssetMaintenance maintenance);
    
    public partial AssetMaintenance CreateDtoToMaintenance(CreateAssetMaintenanceDto dto);
    
    public partial void UpdateMaintenanceFromDto(UpdateAssetMaintenanceDto dto, AssetMaintenance maintenance);
}
