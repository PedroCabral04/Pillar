using erp.DAOs.Administration;
using erp.DTOs.User;
using erp.Models.Identity;
using Microsoft.Extensions.Logging;

namespace erp.Services.Administration;

public class PositionService : IPositionService
{
    private readonly IPositionDao _positionDao;
    private readonly ILogger<PositionService> _logger;

    public PositionService(
        IPositionDao positionDao,
        ILogger<PositionService> logger)
    {
        _positionDao = positionDao;
        _logger = logger;
    }

    public async Task<List<PositionDto>> GetAllAsync(bool activeOnly = true)
    {
        var positions = await _positionDao.GetAllAsync(activeOnly);
        return positions.Select(MapToDto).ToList();
    }

    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        var position = await _positionDao.GetByIdWithRelationsAsync(id);
        return position == null ? null : MapToDtoWithDetails(position);
    }

    public async Task<PositionDto> CreateAsync(CreatePositionDto dto)
    {
        // Validate code uniqueness if provided
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            if (await _positionDao.CodeExistsAsync(dto.Code))
                throw new InvalidOperationException($"Já existe um cargo com o código '{dto.Code}'");
        }

        var position = new Position
        {
            Title = dto.Title,
            Description = dto.Description,
            Code = dto.Code,
            Level = dto.Level,
            MinSalary = dto.MinSalary,
            MaxSalary = dto.MaxSalary,
            DefaultDepartmentId = dto.DefaultDepartmentId,
            Requirements = dto.Requirements,
            Responsibilities = dto.Responsibilities,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _positionDao.CreateAsync(position);
        return MapToDto(created);
    }

    public async Task UpdateAsync(int id, UpdatePositionDto dto)
    {
        var position = await _positionDao.GetByIdAsync(id);
        if (position == null)
            throw new KeyNotFoundException($"Cargo com ID {id} não encontrado");

        // Validate code uniqueness if changed
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != position.Code)
        {
            if (await _positionDao.CodeExistsAsync(dto.Code, id))
                throw new InvalidOperationException($"Já existe outro cargo com o código '{dto.Code}'");
        }

        position.Title = dto.Title;
        position.Description = dto.Description;
        position.Code = dto.Code;
        position.Level = dto.Level;
        position.MinSalary = dto.MinSalary;
        position.MaxSalary = dto.MaxSalary;
        position.DefaultDepartmentId = dto.DefaultDepartmentId;
        position.Requirements = dto.Requirements;
        position.Responsibilities = dto.Responsibilities;
        position.IsActive = dto.IsActive;
        position.UpdatedAt = DateTime.UtcNow;

        await _positionDao.UpdateAsync(position);
    }

    public async Task DeleteAsync(int id)
    {
        var position = await _positionDao.GetByIdAsync(id);
        if (position == null)
            throw new KeyNotFoundException($"Cargo com ID {id} não encontrado");

        if (await _positionDao.HasEmployeesAsync(id))
            throw new InvalidOperationException("Não é possível excluir um cargo com funcionários. Reatribua os funcionários primeiro.");

        await _positionDao.DeleteAsync(id);
    }

    private static PositionDto MapToDto(Position position)
    {
        return new PositionDto
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            Code = position.Code,
            Level = position.Level,
            MinSalary = position.MinSalary,
            MaxSalary = position.MaxSalary,
            DefaultDepartmentId = position.DefaultDepartmentId,
            DefaultDepartmentName = position.DefaultDepartment?.Name,
            Requirements = position.Requirements,
            Responsibilities = position.Responsibilities,
            IsActive = position.IsActive,
            EmployeeCount = 0
        };
    }

    private static PositionDto MapToDtoWithDetails(Position position)
    {
        return new PositionDto
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            Code = position.Code,
            Level = position.Level,
            MinSalary = position.MinSalary,
            MaxSalary = position.MaxSalary,
            DefaultDepartmentId = position.DefaultDepartmentId,
            DefaultDepartmentName = position.DefaultDepartment?.Name,
            Requirements = position.Requirements,
            Responsibilities = position.Responsibilities,
            IsActive = position.IsActive,
            EmployeeCount = position.Employees.Count
        };
    }
}
