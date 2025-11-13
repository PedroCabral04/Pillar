using Microsoft.AspNetCore.Mvc;
using erp.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.Models.Identity;
using erp.Models.Audit;

namespace erp.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAllDepartments()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .ToListAsync();
            
        var dtos = departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Code = d.Code,
            IsActive = d.IsActive,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment?.Name,
            ManagerId = d.ManagerId,
            ManagerName = d.Manager?.FullName ?? d.Manager?.UserName,
            CostCenter = d.CostCenter,
            EmployeeCount = d.Employees.Count
        }).ToList();
        
        return Ok(dtos);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AuditRead("Department", DataSensitivity.Medium, Description = "Visualização de informações do departamento e funcionários")]
    public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);
            
        if (department == null)
            return NotFound($"Departamento com ID {id} não encontrado.");
            
        var dto = new DepartmentDto
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
        
        return Ok(dto);
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] CreateDepartmentDto createDto)
    {
        var department = new Department
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Code = createDto.Code,
            ParentDepartmentId = createDto.ParentDepartmentId,
            ManagerId = createDto.ManagerId,
            CostCenter = createDto.CostCenter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        
        var dto = new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            Code = department.Code,
            IsActive = department.IsActive,
            ParentDepartmentId = department.ParentDepartmentId,
            ManagerId = department.ManagerId,
            CostCenter = department.CostCenter,
            EmployeeCount = 0
        };
        
        return CreatedAtAction(nameof(GetDepartmentById), new { id = dto.Id }, dto);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDto)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound($"Departamento com ID {id} não encontrado.");
            
        department.Name = updateDto.Name;
        department.Description = updateDto.Description;
        department.Code = updateDto.Code;
        department.IsActive = updateDto.IsActive;
        department.ParentDepartmentId = updateDto.ParentDepartmentId;
        department.ManagerId = updateDto.ManagerId;
        department.CostCenter = updateDto.CostCenter;
        department.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .Include(d => d.SubDepartments)
            .FirstOrDefaultAsync(d => d.Id == id);
            
        if (department == null)
            return NotFound($"Departamento com ID {id} não encontrado.");
            
        if (department.Employees.Any())
            return BadRequest("Não é possível excluir um departamento com funcionários. Reatribua os funcionários primeiro.");
            
        if (department.SubDepartments.Any())
            return BadRequest("Não é possível excluir um departamento com subdepartamentos.");
        
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
