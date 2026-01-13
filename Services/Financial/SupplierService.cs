using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;
using erp.Services.Financial.Validation;

namespace erp.Services.Financial;

public interface ISupplierService
{
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<List<SupplierDto>> GetAllAsync(bool activeOnly = true);
    Task<(List<SupplierSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null, bool? activeOnly = null,
        string? sortBy = null, bool sortDescending = false);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto, int currentUserId);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto);
    Task DeleteAsync(int id);
    Task<ReceitaWsResponse?> GetCompanyDataAsync(string cnpj);
    Task<ViaCepResponse?> GetAddressAsync(string cep);
}

public class SupplierService : ISupplierService
{
    private readonly ISupplierDao _supplierDao;
    private readonly FinancialMapper _mapper;
    private readonly IReceitaWsService _receitaWsService;
    private readonly IViaCepService _viaCepService;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(
        ISupplierDao supplierDao,
        FinancialMapper mapper,
        IReceitaWsService receitaWsService,
        IViaCepService viaCepService,
        ILogger<SupplierService> logger)
    {
        _supplierDao = supplierDao;
        _mapper = mapper;
        _receitaWsService = receitaWsService;
        _viaCepService = viaCepService;
        _logger = logger;
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        var supplier = await _supplierDao.GetByIdWithRelationsAsync(id);
        return supplier == null ? null : _mapper.ToDtoWithRelations(supplier);
    }

    public async Task<List<SupplierDto>> GetAllAsync(bool activeOnly = true)
    {
        var suppliers = await _supplierDao.GetAllAsync(activeOnly);
        return suppliers.Select(s => _mapper.ToDto(s)).ToList();
    }

    public async Task<(List<SupplierSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null, bool? activeOnly = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var (items, totalCount) = await _supplierDao.GetPagedAsync(
            page, pageSize, search, activeOnly, sortBy, sortDescending);

        var dtos = items.Select(s => _mapper.ToSummaryDtoWithRelations(s)).ToList();
        return (dtos, totalCount);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto, int currentUserId)
    {
        // Validate CNPJ/CPF
        var cleanedTaxId = BrazilianDocumentValidator.RemoveFormatting(dto.TaxId);
        if (!BrazilianDocumentValidator.IsValidDocument(cleanedTaxId))
            throw new ArgumentException("CNPJ/CPF inválido");

        // Check for duplicates
        if (await _supplierDao.TaxIdExistsAsync(cleanedTaxId))
            throw new InvalidOperationException("Já existe um fornecedor com este CNPJ/CPF");

        var supplier = _mapper.ToEntity(dto);
        supplier.TaxId = cleanedTaxId;
        supplier.CreatedByUserId = currentUserId;
        supplier.CreatedAt = DateTime.UtcNow;

        var created = await _supplierDao.CreateAsync(supplier);
        return _mapper.ToDto(created);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierDto dto)
    {
        var existing = await _supplierDao.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Fornecedor {id} não encontrado");

        // Validate CNPJ/CPF
        var cleanedTaxId = BrazilianDocumentValidator.RemoveFormatting(dto.TaxId);
        if (!BrazilianDocumentValidator.IsValidDocument(cleanedTaxId))
            throw new ArgumentException("CNPJ/CPF inválido");

        // Check for duplicates (excluding current)
        if (await _supplierDao.TaxIdExistsAsync(cleanedTaxId, id))
            throw new InvalidOperationException("Já existe outro fornecedor com este CNPJ/CPF");

        _mapper.UpdateEntity(dto, existing);
        existing.TaxId = cleanedTaxId;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _supplierDao.UpdateAsync(existing);
        return _mapper.ToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _supplierDao.GetByIdAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Fornecedor {id} não encontrado");

        // Check if supplier has associated accounts
        // In a real scenario, you might want to soft-delete instead
        await _supplierDao.DeleteAsync(id);
    }

    public async Task<ReceitaWsResponse?> GetCompanyDataAsync(string cnpj)
    {
        return await _receitaWsService.GetCompanyByCnpjAsync(cnpj);
    }

    public async Task<ViaCepResponse?> GetAddressAsync(string cep)
    {
        return await _viaCepService.GetAddressByCepAsync(cep);
    }
}
