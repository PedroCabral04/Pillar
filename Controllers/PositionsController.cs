using Microsoft.AspNetCore.Mvc;
using erp.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.Models.Identity;

namespace erp.Controllers;

[ApiController]
[Route("api/positions")]
[Authorize]
public class PositionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public PositionsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetAllPositions()
    {
        var positions = await _context.Positions
            .AsNoTracking()
            .Include(p => p.DefaultDepartment)
            .Include(p => p.Employees)
            .ToListAsync();
            
        var dtos = positions.Select(p => new PositionDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Code = p.Code,
            Level = p.Level,
            MinSalary = p.MinSalary,
            MaxSalary = p.MaxSalary,
            DefaultDepartmentId = p.DefaultDepartmentId,
            DefaultDepartmentName = p.DefaultDepartment?.Name,
            Requirements = p.Requirements,
            Responsibilities = p.Responsibilities,
            IsActive = p.IsActive,
            EmployeeCount = p.Employees.Count
        }).ToList();
        
        return Ok(dtos);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PositionDto>> GetPositionById(int id)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(p => p.DefaultDepartment)
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (position == null)
            return NotFound($"Cargo com ID {id} não encontrado.");
            
        var dto = new PositionDto
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
        
        return Ok(dto);
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PositionDto>> CreatePosition([FromBody] CreatePositionDto createDto)
    {
        var position = new Position
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Code = createDto.Code,
            Level = createDto.Level,
            MinSalary = createDto.MinSalary,
            MaxSalary = createDto.MaxSalary,
            DefaultDepartmentId = createDto.DefaultDepartmentId,
            Requirements = createDto.Requirements,
            Responsibilities = createDto.Responsibilities,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
        
        var dto = new PositionDto
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            Code = position.Code,
            Level = position.Level,
            MinSalary = position.MinSalary,
            MaxSalary = position.MaxSalary,
            DefaultDepartmentId = position.DefaultDepartmentId,
            Requirements = position.Requirements,
            Responsibilities = position.Responsibilities,
            IsActive = position.IsActive,
            EmployeeCount = 0
        };
        
        return CreatedAtAction(nameof(GetPositionById), new { id = dto.Id }, dto);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionDto updateDto)
    {
        var position = await _context.Positions.FindAsync(id);
        if (position == null)
            return NotFound($"Cargo com ID {id} não encontrado.");
            
        position.Title = updateDto.Title;
        position.Description = updateDto.Description;
        position.Code = updateDto.Code;
        position.Level = updateDto.Level;
        position.MinSalary = updateDto.MinSalary;
        position.MaxSalary = updateDto.MaxSalary;
        position.DefaultDepartmentId = updateDto.DefaultDepartmentId;
        position.Requirements = updateDto.Requirements;
        position.Responsibilities = updateDto.Responsibilities;
        position.IsActive = updateDto.IsActive;
        position.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var position = await _context.Positions
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (position == null)
            return NotFound($"Cargo com ID {id} não encontrado.");
            
        if (position.Employees.Any())
            return BadRequest("Não é possível excluir um cargo com funcionários. Reatribua os funcionários primeiro.");
        
        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
