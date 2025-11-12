using erp.DAOs.Assets;
using erp.DTOs.Assets;
using erp.Mappings;
using erp.Models;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Assets;

public interface IAssetService
{
    // Asset Management
    Task<AssetDto?> GetAssetByIdAsync(int id);
    Task<AssetDto?> GetAssetByCodeAsync(string assetCode);
    Task<List<AssetDto>> GetAllAssetsAsync();
    Task<List<AssetDto>> GetAssetsByCategoryAsync(int categoryId);
    Task<List<AssetDto>> GetAssetsByStatusAsync(AssetStatus status);
    Task<AssetDto> CreateAssetAsync(CreateAssetDto dto);
    Task<AssetDto> UpdateAssetAsync(int id, UpdateAssetDto dto);
    Task DeleteAssetAsync(int id);
    
    // Asset Assignment
    Task<AssetAssignmentDto?> GetAssignmentByIdAsync(int id);
    Task<AssetAssignmentDto?> GetCurrentAssignmentForAssetAsync(int assetId);
    Task<List<AssetAssignmentDto>> GetAssignmentHistoryForAssetAsync(int assetId);
    Task<List<AssetAssignmentDto>> GetAssignmentsForUserAsync(int userId, bool includeReturned = false);
    Task<AssetAssignmentDto> AssignAssetAsync(CreateAssetAssignmentDto dto, int assignedByUserId);
    Task<AssetAssignmentDto> ReturnAssetAsync(int assignmentId, ReturnAssetDto dto, int returnedByUserId);
    
    // Asset Maintenance
    Task<AssetMaintenanceDto?> GetMaintenanceByIdAsync(int id);
    Task<List<AssetMaintenanceDto>> GetMaintenanceHistoryForAssetAsync(int assetId);
    Task<List<AssetMaintenanceDto>> GetScheduledMaintenancesAsync();
    Task<List<AssetMaintenanceDto>> GetOverdueMaintenancesAsync();
    Task<AssetMaintenanceDto> CreateMaintenanceAsync(CreateAssetMaintenanceDto dto, int createdByUserId);
    Task<AssetMaintenanceDto> UpdateMaintenanceAsync(int id, UpdateAssetMaintenanceDto dto);
    Task<AssetMaintenanceDto> CompleteMaintenanceAsync(int id, int completedByUserId);
    Task DeleteMaintenanceAsync(int id);
    
    // Category Management
    Task<AssetCategoryDto?> GetCategoryByIdAsync(int id);
    Task<List<AssetCategoryDto>> GetAllCategoriesAsync();
    Task<AssetCategoryDto> CreateCategoryAsync(CreateAssetCategoryDto dto);
    Task<AssetCategoryDto> UpdateCategoryAsync(int id, UpdateAssetCategoryDto dto);
    Task DeleteCategoryAsync(int id);
    
    // Statistics
    Task<AssetStatisticsDto> GetAssetStatisticsAsync();
}

public class AssetService : IAssetService
{
    private readonly IAssetDao _assetDao;
    private readonly AssetMapper _mapper;

    public AssetService(IAssetDao assetDao, AssetMapper mapper)
    {
        _assetDao = assetDao;
        _mapper = mapper;
    }

    // ============= Asset Management =============
    
    public async Task<AssetDto?> GetAssetByIdAsync(int id)
    {
        var asset = await _assetDao.GetAssetByIdAsync(id);
        if (asset == null) return null;
        
        var dto = _mapper.AssetToDto(asset);
        
        // Add current assignment info
        var currentAssignment = asset.Assignments.FirstOrDefault(a => a.ReturnedDate == null);
        if (currentAssignment != null)
        {
            dto.CurrentAssignmentId = currentAssignment.Id;
            dto.CurrentAssignedToUserName = currentAssignment.AssignedToUser.FullName;
            dto.CurrentAssignedDate = currentAssignment.AssignedDate;
        }
        
        return dto;
    }

    public async Task<AssetDto?> GetAssetByCodeAsync(string assetCode)
    {
        var asset = await _assetDao.GetAssetByCodeAsync(assetCode);
        return asset != null ? _mapper.AssetToDto(asset) : null;
    }

    public async Task<List<AssetDto>> GetAllAssetsAsync()
    {
        var assets = await _assetDao.GetAllAssetsAsync();
        return assets.Select(a =>
        {
            var dto = _mapper.AssetToDto(a);
            var currentAssignment = a.Assignments.FirstOrDefault(aa => aa.ReturnedDate == null);
            if (currentAssignment != null)
            {
                dto.CurrentAssignmentId = currentAssignment.Id;
                dto.CurrentAssignedToUserName = currentAssignment.AssignedToUser.FullName;
                dto.CurrentAssignedDate = currentAssignment.AssignedDate;
            }
            return dto;
        }).ToList();
    }

    public async Task<List<AssetDto>> GetAssetsByCategoryAsync(int categoryId)
    {
        var assets = await _assetDao.GetAssetsByCategoryAsync(categoryId);
        return assets.Select(a => _mapper.AssetToDto(a)).ToList();
    }

    public async Task<List<AssetDto>> GetAssetsByStatusAsync(AssetStatus status)
    {
        var assets = await _assetDao.GetAssetsByStatusAsync(status);
        return assets.Select(a => _mapper.AssetToDto(a)).ToList();
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto)
    {
        // Check if asset code already exists
        var existing = await _assetDao.GetAssetByCodeAsync(dto.AssetCode);
        if (existing != null)
        {
            throw new InvalidOperationException($"Já existe um ativo com o código {dto.AssetCode}");
        }
        
        var asset = _mapper.CreateDtoToAsset(dto);
        asset.CreatedAt = DateTime.UtcNow;
        
        var created = await _assetDao.CreateAssetAsync(asset);
        return _mapper.AssetToDto(created);
    }

    public async Task<AssetDto> UpdateAssetAsync(int id, UpdateAssetDto dto)
    {
        var asset = await _assetDao.GetAssetByIdAsync(id);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Check if code is being changed and if new code already exists
        if (asset.AssetCode != dto.AssetCode)
        {
            var existing = await _assetDao.GetAssetByCodeAsync(dto.AssetCode);
            if (existing != null && existing.Id != id)
            {
                throw new InvalidOperationException($"Já existe um ativo com o código {dto.AssetCode}");
            }
        }
        
        _mapper.UpdateAssetFromDto(dto, asset);
        
        var updated = await _assetDao.UpdateAssetAsync(asset);
        return _mapper.AssetToDto(updated);
    }

    public async Task DeleteAssetAsync(int id)
    {
        var asset = await _assetDao.GetAssetByIdAsync(id);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Check if asset has active assignments
        var currentAssignment = await _assetDao.GetCurrentAssignmentForAssetAsync(id);
        if (currentAssignment != null)
        {
            throw new InvalidOperationException("Não é possível excluir um ativo que está atualmente atribuído a um funcionário");
        }
        
        await _assetDao.DeleteAssetAsync(id);
    }

    // ============= Asset Assignment =============
    
    public async Task<AssetAssignmentDto?> GetAssignmentByIdAsync(int id)
    {
        var assignment = await _assetDao.GetAssignmentByIdAsync(id);
        return assignment != null ? _mapper.AssignmentToDto(assignment) : null;
    }

    public async Task<AssetAssignmentDto?> GetCurrentAssignmentForAssetAsync(int assetId)
    {
        var assignment = await _assetDao.GetCurrentAssignmentForAssetAsync(assetId);
        return assignment != null ? _mapper.AssignmentToDto(assignment) : null;
    }

    public async Task<List<AssetAssignmentDto>> GetAssignmentHistoryForAssetAsync(int assetId)
    {
        var assignments = await _assetDao.GetAssignmentHistoryForAssetAsync(assetId);
        return assignments.Select(a => _mapper.AssignmentToDto(a)).ToList();
    }

    public async Task<List<AssetAssignmentDto>> GetAssignmentsForUserAsync(int userId, bool includeReturned = false)
    {
        var assignments = await _assetDao.GetAssignmentsForUserAsync(userId, includeReturned);
        return assignments.Select(a => _mapper.AssignmentToDto(a)).ToList();
    }

    public async Task<AssetAssignmentDto> AssignAssetAsync(CreateAssetAssignmentDto dto, int assignedByUserId)
    {
        // Check if asset exists
        var asset = await _assetDao.GetAssetByIdAsync(dto.AssetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Check if asset is already assigned
        var currentAssignment = await _assetDao.GetCurrentAssignmentForAssetAsync(dto.AssetId);
        if (currentAssignment != null)
        {
            throw new InvalidOperationException("Este ativo já está atribuído. Registre a devolução antes de atribuir novamente.");
        }
        
        var assignment = _mapper.CreateDtoToAssignment(dto);
        assignment.AssignedByUserId = assignedByUserId;
        assignment.CreatedAt = DateTime.UtcNow;
        
        // Update asset status
        asset.Status = AssetStatus.InUse;
        await _assetDao.UpdateAssetAsync(asset);
        
        var created = await _assetDao.CreateAssignmentAsync(assignment);
        return _mapper.AssignmentToDto(created);
    }

    public async Task<AssetAssignmentDto> ReturnAssetAsync(int assignmentId, ReturnAssetDto dto, int returnedByUserId)
    {
        var assignment = await _assetDao.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException("Atribuição não encontrada");
        }
        
        if (assignment.ReturnedDate != null)
        {
            throw new InvalidOperationException("Este ativo já foi devolvido");
        }
        
        assignment.ReturnedDate = dto.ReturnedDate;
        assignment.ConditionOnReturn = dto.ConditionOnReturn;
        assignment.ReturnNotes = dto.ReturnNotes;
        assignment.ReturnedByUserId = returnedByUserId;
        
        // Update asset status and condition
        var asset = await _assetDao.GetAssetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Available;
            asset.Condition = dto.ConditionOnReturn;
            await _assetDao.UpdateAssetAsync(asset);
        }
        
        var updated = await _assetDao.UpdateAssignmentAsync(assignment);
        return _mapper.AssignmentToDto(updated);
    }

    // ============= Asset Maintenance =============
    
    public async Task<AssetMaintenanceDto?> GetMaintenanceByIdAsync(int id)
    {
        var maintenance = await _assetDao.GetMaintenanceByIdAsync(id);
        return maintenance != null ? _mapper.MaintenanceToDto(maintenance) : null;
    }

    public async Task<List<AssetMaintenanceDto>> GetMaintenanceHistoryForAssetAsync(int assetId)
    {
        var maintenances = await _assetDao.GetMaintenanceHistoryForAssetAsync(assetId);
        return maintenances.Select(m => _mapper.MaintenanceToDto(m)).ToList();
    }

    public async Task<List<AssetMaintenanceDto>> GetScheduledMaintenancesAsync()
    {
        var maintenances = await _assetDao.GetScheduledMaintenancesAsync();
        return maintenances.Select(m => _mapper.MaintenanceToDto(m)).ToList();
    }

    public async Task<List<AssetMaintenanceDto>> GetOverdueMaintenancesAsync()
    {
        var maintenances = await _assetDao.GetOverdueMaintenancesAsync();
        return maintenances.Select(m => _mapper.MaintenanceToDto(m)).ToList();
    }

    public async Task<AssetMaintenanceDto> CreateMaintenanceAsync(CreateAssetMaintenanceDto dto, int createdByUserId)
    {
        var asset = await _assetDao.GetAssetByIdAsync(dto.AssetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        var maintenance = _mapper.CreateDtoToMaintenance(dto);
        maintenance.CreatedByUserId = createdByUserId;
        maintenance.Status = MaintenanceStatus.Scheduled;
        maintenance.CreatedAt = DateTime.UtcNow;
        
        // If scheduled for today or past, set asset status to Maintenance
        if (maintenance.ScheduledDate.Date <= DateTime.UtcNow.Date)
        {
            asset.Status = AssetStatus.Maintenance;
            await _assetDao.UpdateAssetAsync(asset);
        }
        
        var created = await _assetDao.CreateMaintenanceAsync(maintenance);
        return _mapper.MaintenanceToDto(created);
    }

    public async Task<AssetMaintenanceDto> UpdateMaintenanceAsync(int id, UpdateAssetMaintenanceDto dto)
    {
        var maintenance = await _assetDao.GetMaintenanceByIdAsync(id);
        if (maintenance == null)
        {
            throw new InvalidOperationException("Manutenção não encontrada");
        }
        
        _mapper.UpdateMaintenanceFromDto(dto, maintenance);
        
        var updated = await _assetDao.UpdateMaintenanceAsync(maintenance);
        return _mapper.MaintenanceToDto(updated);
    }

    public async Task<AssetMaintenanceDto> CompleteMaintenanceAsync(int id, int completedByUserId)
    {
        var maintenance = await _assetDao.GetMaintenanceByIdAsync(id);
        if (maintenance == null)
        {
            throw new InvalidOperationException("Manutenção não encontrada");
        }
        
        if (maintenance.Status == MaintenanceStatus.Completed)
        {
            throw new InvalidOperationException("Esta manutenção já foi concluída");
        }
        
        maintenance.Status = MaintenanceStatus.Completed;
        maintenance.CompletedDate = DateTime.UtcNow;
        maintenance.CompletedByUserId = completedByUserId;
        
        // Update asset status back to Available if no other active maintenances
        var asset = await _assetDao.GetAssetByIdAsync(maintenance.AssetId);
        if (asset != null && asset.Status == AssetStatus.Maintenance)
        {
            var otherActiveMaintenance = (await _assetDao.GetMaintenanceHistoryForAssetAsync(maintenance.AssetId))
                .Any(m => m.Id != id && (m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress));
            
            if (!otherActiveMaintenance)
            {
                asset.Status = AssetStatus.Available;
                await _assetDao.UpdateAssetAsync(asset);
            }
        }
        
        var updated = await _assetDao.UpdateMaintenanceAsync(maintenance);
        return _mapper.MaintenanceToDto(updated);
    }

    public async Task DeleteMaintenanceAsync(int id)
    {
        var maintenance = await _assetDao.GetMaintenanceByIdAsync(id);
        if (maintenance == null)
        {
            throw new InvalidOperationException("Manutenção não encontrada");
        }
        
        if (maintenance.Status == MaintenanceStatus.InProgress)
        {
            throw new InvalidOperationException("Não é possível excluir uma manutenção em andamento");
        }
        
        await _assetDao.DeleteMaintenanceAsync(id);
    }

    // ============= Category Management =============
    
    public async Task<AssetCategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _assetDao.GetCategoryByIdAsync(id);
        return category != null ? _mapper.CategoryToDto(category) : null;
    }

    public async Task<List<AssetCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _assetDao.GetAllCategoriesAsync();
        return categories.Select(c => _mapper.CategoryToDto(c)).ToList();
    }

    public async Task<AssetCategoryDto> CreateCategoryAsync(CreateAssetCategoryDto dto)
    {
        var category = _mapper.CreateDtoToCategory(dto);
        category.CreatedAt = DateTime.UtcNow;
        
        var created = await _assetDao.CreateCategoryAsync(category);
        return _mapper.CategoryToDto(created);
    }

    public async Task<AssetCategoryDto> UpdateCategoryAsync(int id, UpdateAssetCategoryDto dto)
    {
        var category = await _assetDao.GetCategoryByIdAsync(id);
        if (category == null)
        {
            throw new InvalidOperationException("Categoria não encontrada");
        }
        
        _mapper.UpdateCategoryFromDto(dto, category);
        
        var updated = await _assetDao.UpdateCategoryAsync(category);
        return _mapper.CategoryToDto(updated);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _assetDao.GetCategoryByIdAsync(id);
        if (category == null)
        {
            throw new InvalidOperationException("Categoria não encontrada");
        }
        
        // Check if category has assets
        var assets = await _assetDao.GetAssetsByCategoryAsync(id);
        if (assets.Any())
        {
            throw new InvalidOperationException("Não é possível excluir uma categoria que possui ativos associados");
        }
        
        await _assetDao.DeleteCategoryAsync(id);
    }

    // ============= Statistics =============
    
    public async Task<AssetStatisticsDto> GetAssetStatisticsAsync()
    {
        var allAssets = await _assetDao.GetAllAssetsAsync();
        var scheduledMaintenances = await _assetDao.GetScheduledMaintenancesAsync();
        var overdueMaintenances = await _assetDao.GetOverdueMaintenancesAsync();
        
        return new AssetStatisticsDto
        {
            TotalAssets = allAssets.Count,
            AvailableAssets = allAssets.Count(a => a.Status == AssetStatus.Available),
            AssignedAssets = allAssets.Count(a => a.Status == AssetStatus.InUse),
            InMaintenanceAssets = allAssets.Count(a => a.Status == AssetStatus.Maintenance),
            RetiredAssets = allAssets.Count(a => a.Status == AssetStatus.Retired),
            TotalAssetValue = allAssets.Where(a => a.PurchaseValue.HasValue).Sum(a => a.PurchaseValue!.Value),
            ScheduledMaintenances = scheduledMaintenances.Count,
            OverdueMaintenances = overdueMaintenances.Count,
            AssetsByCategory = allAssets
                .GroupBy(a => a.Category.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssetsByStatus = allAssets
                .GroupBy(a => a.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            AssetsByCondition = allAssets
                .GroupBy(a => a.Condition.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}
