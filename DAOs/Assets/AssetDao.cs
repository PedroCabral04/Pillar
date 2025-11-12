using erp.Data;
using erp.Models;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Assets;

public interface IAssetDao
{
    // Asset CRUD
    Task<Asset?> GetAssetByIdAsync(int id);
    Task<Asset?> GetAssetByCodeAsync(string assetCode);
    Task<List<Asset>> GetAllAssetsAsync();
    Task<List<Asset>> GetAssetsByCategoryAsync(int categoryId);
    Task<List<Asset>> GetAssetsByStatusAsync(AssetStatus status);
    Task<Asset> CreateAssetAsync(Asset asset);
    Task<Asset> UpdateAssetAsync(Asset asset);
    Task DeleteAssetAsync(int id);
    
    // Asset Assignment
    Task<AssetAssignment?> GetAssignmentByIdAsync(int id);
    Task<AssetAssignment?> GetCurrentAssignmentForAssetAsync(int assetId);
    Task<List<AssetAssignment>> GetAssignmentHistoryForAssetAsync(int assetId);
    Task<List<AssetAssignment>> GetAssignmentsForUserAsync(int userId, bool includeReturned = false);
    Task<AssetAssignment> CreateAssignmentAsync(AssetAssignment assignment);
    Task<AssetAssignment> UpdateAssignmentAsync(AssetAssignment assignment);
    
    // Asset Maintenance
    Task<AssetMaintenance?> GetMaintenanceByIdAsync(int id);
    Task<List<AssetMaintenance>> GetMaintenanceHistoryForAssetAsync(int assetId);
    Task<List<AssetMaintenance>> GetScheduledMaintenancesAsync();
    Task<List<AssetMaintenance>> GetOverdueMaintenancesAsync();
    Task<AssetMaintenance> CreateMaintenanceAsync(AssetMaintenance maintenance);
    Task<AssetMaintenance> UpdateMaintenanceAsync(AssetMaintenance maintenance);
    Task DeleteMaintenanceAsync(int id);
    
    // Category
    Task<AssetCategory?> GetCategoryByIdAsync(int id);
    Task<List<AssetCategory>> GetAllCategoriesAsync();
    Task<AssetCategory> CreateCategoryAsync(AssetCategory category);
    Task<AssetCategory> UpdateCategoryAsync(AssetCategory category);
    Task DeleteCategoryAsync(int id);
}

public class AssetDao : IAssetDao
{
    private readonly ApplicationDbContext _context;

    public AssetDao(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============= Asset CRUD =============
    
    public async Task<Asset?> GetAssetByIdAsync(int id)
    {
        return await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Assignments.Where(aa => aa.ReturnedDate == null))
                .ThenInclude(aa => aa.AssignedToUser)
            .Include(a => a.MaintenanceRecords.OrderByDescending(m => m.ScheduledDate).Take(5))
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Asset?> GetAssetByCodeAsync(string assetCode)
    {
        return await _context.Assets
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.AssetCode == assetCode);
    }

    public async Task<List<Asset>> GetAllAssetsAsync()
    {
        return await _context.Assets
            .Include(a => a.Category)
            .Include(a => a.Assignments.Where(aa => aa.ReturnedDate == null))
                .ThenInclude(aa => aa.AssignedToUser)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetAssetsByCategoryAsync(int categoryId)
    {
        return await _context.Assets
            .Include(a => a.Category)
            .Where(a => a.CategoryId == categoryId && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetAssetsByStatusAsync(AssetStatus status)
    {
        return await _context.Assets
            .Include(a => a.Category)
            .Where(a => a.Status == status && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Asset> CreateAssetAsync(Asset asset)
    {
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<Asset> UpdateAssetAsync(Asset asset)
    {
        asset.UpdatedAt = DateTime.UtcNow;
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task DeleteAssetAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset != null)
        {
            asset.IsActive = false;
            asset.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // ============= Asset Assignment =============
    
    public async Task<AssetAssignment?> GetAssignmentByIdAsync(int id)
    {
        return await _context.AssetAssignments
            .Include(aa => aa.Asset)
            .Include(aa => aa.AssignedToUser)
            .Include(aa => aa.AssignedByUser)
            .Include(aa => aa.ReturnedByUser)
            .FirstOrDefaultAsync(aa => aa.Id == id);
    }

    public async Task<AssetAssignment?> GetCurrentAssignmentForAssetAsync(int assetId)
    {
        return await _context.AssetAssignments
            .Include(aa => aa.AssignedToUser)
            .Include(aa => aa.AssignedByUser)
            .Where(aa => aa.AssetId == assetId && aa.ReturnedDate == null)
            .OrderByDescending(aa => aa.AssignedDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AssetAssignment>> GetAssignmentHistoryForAssetAsync(int assetId)
    {
        return await _context.AssetAssignments
            .Include(aa => aa.AssignedToUser)
            .Include(aa => aa.AssignedByUser)
            .Include(aa => aa.ReturnedByUser)
            .Where(aa => aa.AssetId == assetId)
            .OrderByDescending(aa => aa.AssignedDate)
            .ToListAsync();
    }

    public async Task<List<AssetAssignment>> GetAssignmentsForUserAsync(int userId, bool includeReturned = false)
    {
        var query = _context.AssetAssignments
            .Include(aa => aa.Asset)
                .ThenInclude(a => a.Category)
            .Include(aa => aa.AssignedByUser)
            .Where(aa => aa.AssignedToUserId == userId);

        if (!includeReturned)
        {
            query = query.Where(aa => aa.ReturnedDate == null);
        }

        return await query
            .OrderByDescending(aa => aa.AssignedDate)
            .ToListAsync();
    }

    public async Task<AssetAssignment> CreateAssignmentAsync(AssetAssignment assignment)
    {
        _context.AssetAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<AssetAssignment> UpdateAssignmentAsync(AssetAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.AssetAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    // ============= Asset Maintenance =============
    
    public async Task<AssetMaintenance?> GetMaintenanceByIdAsync(int id)
    {
        return await _context.AssetMaintenances
            .Include(m => m.Asset)
            .Include(m => m.CreatedByUser)
            .Include(m => m.CompletedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<AssetMaintenance>> GetMaintenanceHistoryForAssetAsync(int assetId)
    {
        return await _context.AssetMaintenances
            .Include(m => m.CreatedByUser)
            .Include(m => m.CompletedByUser)
            .Where(m => m.AssetId == assetId)
            .OrderByDescending(m => m.ScheduledDate)
            .ToListAsync();
    }

    public async Task<List<AssetMaintenance>> GetScheduledMaintenancesAsync()
    {
        return await _context.AssetMaintenances
            .Include(m => m.Asset)
                .ThenInclude(a => a.Category)
            .Include(m => m.CreatedByUser)
            .Where(m => m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress)
            .OrderBy(m => m.ScheduledDate)
            .ToListAsync();
    }

    public async Task<List<AssetMaintenance>> GetOverdueMaintenancesAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.AssetMaintenances
            .Include(m => m.Asset)
                .ThenInclude(a => a.Category)
            .Include(m => m.CreatedByUser)
            .Where(m => (m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.InProgress)
                        && m.ScheduledDate.Date < today)
            .OrderBy(m => m.ScheduledDate)
            .ToListAsync();
    }

    public async Task<AssetMaintenance> CreateMaintenanceAsync(AssetMaintenance maintenance)
    {
        _context.AssetMaintenances.Add(maintenance);
        await _context.SaveChangesAsync();
        return maintenance;
    }

    public async Task<AssetMaintenance> UpdateMaintenanceAsync(AssetMaintenance maintenance)
    {
        maintenance.UpdatedAt = DateTime.UtcNow;
        _context.AssetMaintenances.Update(maintenance);
        await _context.SaveChangesAsync();
        return maintenance;
    }

    public async Task DeleteMaintenanceAsync(int id)
    {
        var maintenance = await _context.AssetMaintenances.FindAsync(id);
        if (maintenance != null)
        {
            _context.AssetMaintenances.Remove(maintenance);
            await _context.SaveChangesAsync();
        }
    }

    // ============= Category =============
    
    public async Task<AssetCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.AssetCategories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<AssetCategory>> GetAllCategoriesAsync()
    {
        return await _context.AssetCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<AssetCategory> CreateCategoryAsync(AssetCategory category)
    {
        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<AssetCategory> UpdateCategoryAsync(AssetCategory category)
    {
        _context.AssetCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.AssetCategories.FindAsync(id);
        if (category != null)
        {
            category.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
