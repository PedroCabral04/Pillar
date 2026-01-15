using erp.Data;
using erp.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Administration;

public class DepartmentDao : IDepartmentDao
{
    private readonly ApplicationDbContext _context;

    public DepartmentDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Department?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Department>> GetAllAsync(bool activeOnly = true)
    {
        var query = _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.Manager)
            .AsNoTracking();

        if (activeOnly)
            query = query.Where(d => d.IsActive);

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Department?> GetByCodeAsync(string code)
    {
        return await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _context.Departments.Where(d => d.Code == code);

        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<Department> CreateAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return department;
    }

    public async Task<Department> UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
        return department;
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasEmployeesAsync(int id)
    {
        return await _context.Departments
            .Where(d => d.Id == id)
            .AnyAsync(d => d.Employees.Any());
    }

    public async Task<bool> HasSubDepartmentsAsync(int id)
    {
        return await _context.Departments
            .Where(d => d.Id == id)
            .AnyAsync(d => d.SubDepartments.Any());
    }
}
