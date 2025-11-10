using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;

namespace erp.Services.Financial;

public interface ICostCenterService
{
    Task<List<CostCenterDto>> GetAllAsync(bool activeOnly = true);
    Task<CostCenterDto?> GetByIdAsync(int id);
    
    Task<CostCenterDto> CreateAsync(CreateCostCenterDto createDto, int userId);
    Task<CostCenterDto> UpdateAsync(int id, UpdateCostCenterDto updateDto, int userId);
    Task DeleteAsync(int id);
    Task<CostCenterDto> ToggleActiveAsync(int id, int userId);
}

public class CostCenterService : ICostCenterService
{
    private readonly ICostCenterDao _dao;
    private readonly FinancialMapper _mapper;

    public CostCenterService(ICostCenterDao dao, FinancialMapper mapper)
    {
        _dao = dao;
        _mapper = mapper;
    }

    public async Task<List<CostCenterDto>> GetAllAsync(bool activeOnly = true)
    {
        var entities = await _dao.GetAllAsync(activeOnly);
        return entities.Select(x => _mapper.ToDto(x)).ToList();
    }

    public async Task<CostCenterDto?> GetByIdAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        return entity != null ? _mapper.ToDto(entity) : null;
    }

    public async Task<CostCenterDto> CreateAsync(CreateCostCenterDto createDto, int userId)
    {
        // Validate code uniqueness
        var codeExists = await _dao.CodeExistsAsync(createDto.Code);
        if (codeExists)
            throw new InvalidOperationException($"Já existe um centro de custo com o código '{createDto.Code}'");

        var entity = _mapper.ToEntity(createDto);
        entity.IsActive = true;
        entity.CreatedAt = DateTime.UtcNow;

        var created = await _dao.CreateAsync(entity);
        return _mapper.ToDto(created);
    }

    public async Task<CostCenterDto> UpdateAsync(int id, UpdateCostCenterDto updateDto, int userId)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Centro de custo com ID {id} não encontrado");

        // Validate code uniqueness if changed
        if (updateDto.Code != entity.Code)
        {
            var codeExists = await _dao.CodeExistsAsync(updateDto.Code, id);
            if (codeExists)
                throw new InvalidOperationException($"Já existe um centro de custo com o código '{updateDto.Code}'");
        }

        _mapper.UpdateEntity(updateDto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return _mapper.ToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Centro de custo com ID {id} não encontrado");

        await _dao.DeleteAsync(id);
    }

    public async Task<CostCenterDto> ToggleActiveAsync(int id, int userId)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Centro de custo com ID {id} não encontrado");

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return _mapper.ToDto(updated);
    }
}
