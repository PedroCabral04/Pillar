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
            return $"Não encontrei nenhum funcionário com o nome '{name}'.";

        return $@"👤 **Ficha do Funcionário**
**Nome:** {employee.FullName}
**Email:** {employee.Email}
**Telefone:** {employee.PhoneNumber ?? "N/A"}
**Cargo:** {employee.Position?.Title ?? "N/A"}
**Departamento:** {employee.Department?.Name ?? "N/A"}";
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
            return $"Não encontrei o departamento '{departmentName}'.";

        if (!department.Employees.Any())
            return $"O departamento {department.Name} não possui funcionários alocados.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🏢 **Equipe {department.Name}:**");
        
        foreach (var emp in department.Employees)
        {
            sb.AppendLine($"- {emp.FullName ?? emp.UserName} ({emp.Email})");
        }

        return sb.ToString();
    }
}
