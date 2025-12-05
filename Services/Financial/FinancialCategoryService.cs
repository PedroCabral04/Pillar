using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;

namespace erp.Services.Financial;

public interface IFinancialCategoryService
{
    Task<List<FinancialCategoryDto>> GetAllAsync(bool activeOnly = true);
    Task<FinancialCategoryDto?> GetByIdAsync(int id);
    Task<List<FinancialCategoryDto>> GetByTypeAsync(CategoryType type, bool activeOnly = true);
    Task<List<FinancialCategoryDto>> GetRootCategoriesAsync(CategoryType? type = null);
    Task<List<FinancialCategoryDto>> GetSubCategoriesAsync(int parentId);
    
    Task<FinancialCategoryDto> CreateAsync(CreateFinancialCategoryDto createDto, int userId);
    Task<FinancialCategoryDto> UpdateAsync(int id, UpdateFinancialCategoryDto updateDto, int userId);
    Task DeleteAsync(int id);
}

public class FinancialCategoryService : IFinancialCategoryService
{
    private readonly IFinancialCategoryDao _dao;
    private readonly FinancialMapper _mapper;

    public FinancialCategoryService(IFinancialCategoryDao dao, FinancialMapper mapper)
    {
        _dao = dao;
        _mapper = mapper;
    }

    public async Task<List<FinancialCategoryDto>> GetAllAsync(bool activeOnly = true)
    {
        var entities = await _dao.GetAllAsync(activeOnly);
        return entities.Select(x => _mapper.ToDtoWithRelations(x)).ToList();
    }

    public async Task<FinancialCategoryDto?> GetByIdAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        return entity != null ? _mapper.ToDto(entity) : null;
    }

    public async Task<List<FinancialCategoryDto>> GetByTypeAsync(CategoryType type, bool activeOnly = true)
    {
        var entities = await _dao.GetByTypeAsync(type, activeOnly);
        return entities.Select(x => _mapper.ToDtoWithRelations(x)).ToList();
    }

    public async Task<List<FinancialCategoryDto>> GetRootCategoriesAsync(CategoryType? type = null)
    {
        var entities = await _dao.GetRootCategoriesAsync(type);
        return entities.Select(x => _mapper.ToDto(x)).ToList();
    }

    public async Task<List<FinancialCategoryDto>> GetSubCategoriesAsync(int parentId)
    {
        var entities = await _dao.GetSubCategoriesAsync(parentId);
        return entities.Select(x => _mapper.ToDto(x)).ToList();
    }

    public async Task<FinancialCategoryDto> CreateAsync(CreateFinancialCategoryDto createDto, int userId)
    {
        // Validate code uniqueness
        var codeExists = await _dao.CodeExistsAsync(createDto.Code);
        if (codeExists)
            throw new InvalidOperationException($"Já existe uma categoria com o código '{createDto.Code}'");

        // Validate parent exists if specified
        if (createDto.ParentCategoryId.HasValue)
        {
            var parent = await _dao.GetByIdAsync(createDto.ParentCategoryId.Value);
            if (parent == null)
                throw new KeyNotFoundException($"Categoria pai com ID {createDto.ParentCategoryId.Value} não encontrada");
            
            // Ensure parent and child are same type
            if (parent.Type != createDto.Type)
                throw new InvalidOperationException("A categoria pai deve ser do mesmo tipo (Receita/Despesa)");
        }

        var entity = _mapper.ToEntity(createDto);
        entity.CreatedAt = DateTime.UtcNow;

        var created = await _dao.CreateAsync(entity);
        return _mapper.ToDto(created);
    }

    public async Task<FinancialCategoryDto> UpdateAsync(int id, UpdateFinancialCategoryDto updateDto, int userId)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Categoria com ID {id} não encontrada");

        // Validate code uniqueness if changed
        if (updateDto.Code != entity.Code)
        {
            var codeExists = await _dao.CodeExistsAsync(updateDto.Code, id);
            if (codeExists)
                throw new InvalidOperationException($"Já existe uma categoria com o código '{updateDto.Code}'");
        }

        // Validate parent change
        if (updateDto.ParentCategoryId.HasValue && updateDto.ParentCategoryId != entity.ParentCategoryId)
        {
            // Prevent circular reference
            if (updateDto.ParentCategoryId.Value == id)
                throw new InvalidOperationException("Uma categoria não pode ser pai de si mesma");

            var parent = await _dao.GetByIdAsync(updateDto.ParentCategoryId.Value);
            if (parent == null)
                throw new KeyNotFoundException($"Categoria pai com ID {updateDto.ParentCategoryId.Value} não encontrada");
            
            if (parent.Type != entity.Type)
                throw new InvalidOperationException("A categoria pai deve ser do mesmo tipo (Receita/Despesa)");
            
            // Check if new parent is a descendant (would create cycle)
            if (await IsDescendant(id, updateDto.ParentCategoryId.Value))
                throw new InvalidOperationException("Não é possível criar uma hierarquia circular");
        }

        _mapper.UpdateEntity(updateDto, entity);

        var updated = await _dao.UpdateAsync(entity);
        return _mapper.ToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Categoria com ID {id} não encontrada");

        // Check if has subcategories
        var subCategories = await _dao.GetSubCategoriesAsync(id);
        if (subCategories.Any())
            throw new InvalidOperationException("Não é possível excluir uma categoria que possui subcategorias");

        await _dao.DeleteAsync(id);
    }

    private async Task<bool> IsDescendant(int ancestorId, int potentialDescendantId)
    {
        var current = await _dao.GetByIdAsync(potentialDescendantId);
        
        while (current?.ParentCategoryId.HasValue == true)
        {
            if (current.ParentCategoryId.Value == ancestorId)
                return true;
            
            current = await _dao.GetByIdAsync(current.ParentCategoryId.Value);
        }
        
        return false;
    }
}
