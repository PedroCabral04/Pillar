using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Data;
using Microsoft.EntityFrameworkCore;

using erp.Models.Identity;

namespace erp.Services.Chatbot.ChatbotPlugins;

public class HRPlugin
{
    private readonly ApplicationDbContext _context;

    public HRPlugin(ApplicationDbContext context)
    {
        _context = context;
    }

    [KernelFunction, Description("Busca informações de contato e cargo de um funcionário pelo nome")]
    public async Task<string> GetEmployeeDetails(
        [Description("Nome do funcionário")] string name)
    {
        var employee = await _context.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => (u.UserName != null && u.UserName.Contains(name)) || 
                        (u.Email != null && u.Email.Contains(name)) || 
                        (u.FullName != null && u.FullName.Contains(name)))
            .Select(u => new { u.FullName, u.Email, u.PhoneNumber, u.Position, u.Department })
            .FirstOrDefaultAsync();

        if (employee == null)
            return $"🔍 Não encontrei nenhum funcionário com o nome **'{name}'**.";

        return $"""
            👤 **Ficha do Funcionário**
            
            | Campo | Informação |
            |-------|------------|
            | **Nome** | {employee.FullName ?? "—"} |
            | **Email** | {employee.Email ?? "—"} |
            | **Telefone** | {employee.PhoneNumber ?? "—"} |
            | **Cargo** | {employee.Position?.Title ?? "—"} |
            | **Departamento** | {employee.Department?.Name ?? "—"} |
            """;
    }

    [KernelFunction, Description("Lista os membros de um departamento")]
    public async Task<string> ListDepartmentMembers(
        [Description("Nome do departamento")] string departmentName)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Name.Contains(departmentName));

        if (department == null)
            return $"🔍 Não encontrei o departamento **'{departmentName}'**.";

        if (!department.Employees.Any())
            return $"🏢 O departamento **{department.Name}** não possui funcionários alocados.";

        var employeeList = department.Employees.Take(15).Select(emp =>
            $"| {emp.FullName ?? emp.UserName} | {emp.Email} |"
        );
        
        var remaining = department.Employees.Count - 15;
        var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} funcionários.*" : "";

        return $"""
            🏢 **Equipe {department.Name}** ({department.Employees.Count})
            
            | Nome | Email |
            |------|-------|
            {string.Join("\n", employeeList)}{moreText}
            """;
    }
}
