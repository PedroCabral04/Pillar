using erp.DAOs.Administration;
using erp.DTOs.User;
using erp.Models.Identity;
using Microsoft.Extensions.Logging;

namespace erp.Services.Administration;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentDao _departmentDao;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(
        IDepartmentDao departmentDao,
        ILogger<DepartmentService> logger)
    {
        _departmentDao = departmentDao;
        _logger = logger;
    }

    public async Task<List<DepartmentDto>> GetAllAsync(bool activeOnly = true)
    {
        var departments = await _departmentDao.GetAllAsync(activeOnly);
        return departments.Select(MapToDto).ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var department = await _departmentDao.GetByIdWithRelationsAsync(id);
        return department == null ? null : MapToDtoWithDetails(department);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        // Validate code uniqueness if provided
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            if (await _departmentDao.CodeExistsAsync(dto.Code))
                throw new InvalidOperationException($"Já existe um departamento com o código '{dto.Code}'");
        }

        var department = new Department
        {
            Name = dto.Name,
            Description = dto.Description,
            Code = dto.Code,
            ParentDepartmentId = dto.ParentDepartmentId,
            ManagerId = dto.ManagerId,
            CostCenter = dto.CostCenter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _departmentDao.CreateAsync(department);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, UpdateDepartmentDto dto)
    {
        var department = await _departmentDao.GetByIdAsync(id);
        if (department == null)
            throw new KeyNotFoundException($"Departamento com ID {id} não encontrado");

        // Validate code uniqueness if changed
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != department.Code)
        {
            if (await _departmentDao.CodeExistsAsync(dto.Code, id))
                throw new InvalidOperationException($"Já existe outro departamento com o código '{dto.Code}'");
        }

        department.Name = dto.Name;
        department.Description = dto.Description;
        department.Code = dto.Code;
        department.IsActive = dto.IsActive;
        department.ParentDepartmentId = dto.ParentDepartmentId;
        department.ManagerId = dto.ManagerId;
        department.CostCenter = dto.CostCenter;
        department.UpdatedAt = DateTime.UtcNow;

        await _departmentDao.UpdateAsync(department);
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _departmentDao.GetByIdAsync(id);
        if (department == null)
            throw new KeyNotFoundException($"Departamento com ID {id} não encontrado");

        if (await _departmentDao.HasEmployeesAsync(id))
            throw new InvalidOperationException("Não é possível excluir um departamento com funcionários. Reatribua os funcionários primeiro.");

        if (await _departmentDao.HasSubDepartmentsAsync(id))
            throw new InvalidOperationException("Não é possível excluir um departamento com subdepartamentos.");

        await _departmentDao.DeleteAsync(id);
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            Code = department.Code,
            IsActive = department.IsActive,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            ManagerId = department.ManagerId,
            ManagerName = department.Manager?.FullName ?? department.Manager?.UserName,
            CostCenter = department.CostCenter,
            EmployeeCount = 0
        };
    }

    private static DepartmentDto MapToDtoWithDetails(Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            Code = department.Code,
            IsActive = department.IsActive,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            ManagerId = department.ManagerId,
            ManagerName = department.Manager?.FullName ?? department.Manager?.UserName,
            CostCenter = department.CostCenter,
            EmployeeCount = department.Employees.Count
        };
    }
}
