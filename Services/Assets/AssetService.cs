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
    
    // Document Management
    Task<AssetDocumentDto?> GetDocumentByIdAsync(int id);
    Task<List<AssetDocumentDto>> GetDocumentsByAssetIdAsync(int assetId);
    Task<List<AssetDocumentDto>> GetDocumentsByTypeAsync(int assetId, AssetDocumentType type);
    Task<AssetDocumentDto> CreateDocumentAsync(CreateAssetDocumentDto dto, Stream fileStream, int uploadedByUserId);
    Task<AssetDocumentDto> UpdateDocumentAsync(int id, UpdateAssetDocumentDto dto);
    Task DeleteDocumentAsync(int id);
    Task<Stream> DownloadDocumentAsync(int id);
    
    // Transfer Management
    Task<AssetTransferDto?> GetTransferByIdAsync(int id);
    Task<List<AssetTransferDto>> GetTransferHistoryForAssetAsync(int assetId);
    Task<List<AssetTransferDto>> GetPendingTransfersAsync();
    Task<List<AssetTransferDto>> GetTransfersByStatusAsync(TransferStatus status);
    Task<AssetTransferDto> CreateTransferAsync(CreateAssetTransferDto dto, int requestedByUserId);
    Task<AssetTransferDto> ApproveTransferAsync(int id, int approvedByUserId);
    Task<AssetTransferDto> RejectTransferAsync(int id, int rejectedByUserId, string reason);
    Task<AssetTransferDto> CompleteTransferAsync(int id, int completedByUserId);
    Task<AssetTransferDto> CancelTransferAsync(int id, int cancelledByUserId, string reason);
    
    // QR Code Generation
    Task<byte[]> GenerateAssetQRCodeAsync(int assetId);
    Task<string> GenerateAssetQRCodeBase64Async(int assetId);
}

public class AssetService : IAssetService
{
    private readonly IAssetDao _assetDao;
    private readonly AssetMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly IQRCodeService _qrCodeService;

    public AssetService(
        IAssetDao assetDao, 
        AssetMapper mapper,
        IFileStorageService fileStorage,
        IQRCodeService qrCodeService)
    {
        _assetDao = assetDao;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _qrCodeService = qrCodeService;
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
    
    // ============= Document Management =============
    
    public async Task<AssetDocumentDto?> GetDocumentByIdAsync(int id)
    {
        var document = await _assetDao.GetDocumentByIdAsync(id);
        return document != null ? _mapper.DocumentToDto(document) : null;
    }
    
    public async Task<List<AssetDocumentDto>> GetDocumentsByAssetIdAsync(int assetId)
    {
        var documents = await _assetDao.GetDocumentsByAssetIdAsync(assetId);
        return documents.Select(d => _mapper.DocumentToDto(d)).ToList();
    }
    
    public async Task<List<AssetDocumentDto>> GetDocumentsByTypeAsync(int assetId, AssetDocumentType type)
    {
        var documents = await _assetDao.GetDocumentsByTypeAsync(assetId, type);
        return documents.Select(d => _mapper.DocumentToDto(d)).ToList();
    }
    
    public async Task<AssetDocumentDto> CreateDocumentAsync(CreateAssetDocumentDto dto, Stream fileStream, int uploadedByUserId)
    {
        // Verifica se o ativo existe
        var asset = await _assetDao.GetAssetByIdAsync(dto.AssetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Determina o nome do arquivo baseado no tipo
        var fileName = $"{dto.Type}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var subfolder = $"asset_{dto.AssetId}";
        var filePath = await _fileStorage.SaveFileAsync(fileStream, fileName, subfolder);
        
        // Obtém o tamanho do arquivo
        fileStream.Position = 0;
        var fileSize = fileStream.Length;
        
        // Cria o documento
        var document = _mapper.CreateDtoToDocument(dto);
        document.FileName = fileName;
        document.OriginalFileName = fileName;
        document.FilePath = filePath;
        document.FileSize = fileSize;
        document.ContentType = "application/pdf"; // Default, pode ser melhorado
        document.UploadedByUserId = uploadedByUserId;
        document.CreatedAt = DateTime.UtcNow;
        
        var created = await _assetDao.CreateDocumentAsync(document);
        return _mapper.DocumentToDto(created);
    }
    
    public async Task<AssetDocumentDto> UpdateDocumentAsync(int id, UpdateAssetDocumentDto dto)
    {
        var document = await _assetDao.GetDocumentByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException("Documento não encontrado");
        }
        
        _mapper.UpdateDocumentFromDto(dto, document);
        
        var updated = await _assetDao.UpdateDocumentAsync(document);
        return _mapper.DocumentToDto(updated);
    }
    
    public async Task DeleteDocumentAsync(int id)
    {
        var document = await _assetDao.GetDocumentByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException("Documento não encontrado");
        }
        
        // Exclui o arquivo físico
        await _fileStorage.DeleteFileAsync(document.FilePath);
        
        // Exclui o registro do documento
        await _assetDao.DeleteDocumentAsync(id);
    }
    
    public async Task<Stream> DownloadDocumentAsync(int id)
    {
        var document = await _assetDao.GetDocumentByIdAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException("Documento não encontrado");
        }
        
        return await _fileStorage.GetFileAsync(document.FilePath);
    }
    
    // ============= Transfer Management =============
    
    public async Task<AssetTransferDto?> GetTransferByIdAsync(int id)
    {
        var transfer = await _assetDao.GetTransferByIdAsync(id);
        return transfer != null ? _mapper.TransferToDto(transfer) : null;
    }
    
    public async Task<List<AssetTransferDto>> GetTransferHistoryForAssetAsync(int assetId)
    {
        var transfers = await _assetDao.GetTransferHistoryForAssetAsync(assetId);
        return transfers.Select(t => _mapper.TransferToDto(t)).ToList();
    }
    
    public async Task<List<AssetTransferDto>> GetPendingTransfersAsync()
    {
        var transfers = await _assetDao.GetPendingTransfersAsync();
        return transfers.Select(t => _mapper.TransferToDto(t)).ToList();
    }
    
    public async Task<List<AssetTransferDto>> GetTransfersByStatusAsync(TransferStatus status)
    {
        var transfers = await _assetDao.GetTransfersByStatusAsync(status);
        return transfers.Select(t => _mapper.TransferToDto(t)).ToList();
    }
    
    public async Task<AssetTransferDto> CreateTransferAsync(CreateAssetTransferDto dto, int requestedByUserId)
    {
        // Verifica se o ativo existe
        var asset = await _assetDao.GetAssetByIdAsync(dto.AssetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Verifica se já existe uma transferência pendente para este ativo
        var pendingTransfers = await _assetDao.GetPendingTransfersAsync();
        if (pendingTransfers.Any(t => t.AssetId == dto.AssetId))
        {
            throw new InvalidOperationException("Já existe uma transferência pendente para este ativo");
        }
        
        var transfer = _mapper.CreateDtoToTransfer(dto);
        transfer.RequestedByUserId = requestedByUserId;
        transfer.Status = TransferStatus.Pending;
        transfer.FromLocation = asset.Location ?? "";
        transfer.CreatedAt = DateTime.UtcNow;
        
        var created = await _assetDao.CreateTransferAsync(transfer);
        return _mapper.TransferToDto(created);
    }
    
    public async Task<AssetTransferDto> ApproveTransferAsync(int id, int approvedByUserId)
    {
        var transfer = await _assetDao.GetTransferByIdAsync(id);
        if (transfer == null)
        {
            throw new InvalidOperationException("Transferência não encontrada");
        }
        
        if (transfer.Status != TransferStatus.Pending)
        {
            throw new InvalidOperationException("Apenas transferências pendentes podem ser aprovadas");
        }
        
        transfer.Status = TransferStatus.InTransit;
        transfer.ApprovedByUserId = approvedByUserId;
        transfer.ApprovedDate = DateTime.UtcNow;
        
        var updated = await _assetDao.UpdateTransferAsync(transfer);
        return _mapper.TransferToDto(updated);
    }
    
    public async Task<AssetTransferDto> RejectTransferAsync(int id, int rejectedByUserId, string reason)
    {
        var transfer = await _assetDao.GetTransferByIdAsync(id);
        if (transfer == null)
        {
            throw new InvalidOperationException("Transferência não encontrada");
        }
        
        if (transfer.Status != TransferStatus.Pending)
        {
            throw new InvalidOperationException("Apenas transferências pendentes podem ser rejeitadas");
        }
        
        transfer.Status = TransferStatus.Cancelled;
        transfer.ApprovedByUserId = rejectedByUserId;
        transfer.ApprovedDate = DateTime.UtcNow;
        transfer.Notes = $"Rejeitada: {reason}";
        
        var updated = await _assetDao.UpdateTransferAsync(transfer);
        return _mapper.TransferToDto(updated);
    }
    
    public async Task<AssetTransferDto> CompleteTransferAsync(int id, int completedByUserId)
    {
        var transfer = await _assetDao.GetTransferByIdAsync(id);
        if (transfer == null)
        {
            throw new InvalidOperationException("Transferência não encontrada");
        }
        
        if (transfer.Status != TransferStatus.InTransit)
        {
            throw new InvalidOperationException("Apenas transferências em trânsito podem ser concluídas");
        }
        
        transfer.Status = TransferStatus.Completed;
        transfer.CompletedByUserId = completedByUserId;
        transfer.CompletedDate = DateTime.UtcNow;
        
        // Atualiza a localização do ativo
        var asset = await _assetDao.GetAssetByIdAsync(transfer.AssetId);
        if (asset != null)
        {
            asset.Location = transfer.ToLocation;
            await _assetDao.UpdateAssetAsync(asset);
        }
        
        var updated = await _assetDao.UpdateTransferAsync(transfer);
        return _mapper.TransferToDto(updated);
    }
    
    public async Task<AssetTransferDto> CancelTransferAsync(int id, int cancelledByUserId, string reason)
    {
        var transfer = await _assetDao.GetTransferByIdAsync(id);
        if (transfer == null)
        {
            throw new InvalidOperationException("Transferência não encontrada");
        }
        
        if (transfer.Status == TransferStatus.Completed)
        {
            throw new InvalidOperationException("Não é possível cancelar uma transferência concluída");
        }
        
        if (transfer.Status == TransferStatus.Cancelled)
        {
            throw new InvalidOperationException("Esta transferência já está cancelada");
        }
        
        transfer.Status = TransferStatus.Cancelled;
        transfer.Notes = $"Cancelada: {reason}";
        
        var updated = await _assetDao.UpdateTransferAsync(transfer);
        return _mapper.TransferToDto(updated);
    }
    
    // ============= QR Code Generation =============
    
    public async Task<byte[]> GenerateAssetQRCodeAsync(int assetId)
    {
        var asset = await _assetDao.GetAssetByIdAsync(assetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Gera um texto com informações do ativo
        var qrContent = $"ASSET:{asset.AssetCode}|{asset.Name}|{asset.Id}";
        
        return _qrCodeService.GenerateQRCode(qrContent);
    }
    
    public async Task<string> GenerateAssetQRCodeBase64Async(int assetId)
    {
        var asset = await _assetDao.GetAssetByIdAsync(assetId);
        if (asset == null)
        {
            throw new InvalidOperationException("Ativo não encontrado");
        }
        
        // Gera um texto com informações do ativo
        var qrContent = $"ASSET:{asset.AssetCode}|{asset.Name}|{asset.Id}";
        
        return _qrCodeService.GenerateQRCodeBase64(qrContent);
    }
}
