using erp.Data;
using erp.Models;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs;

public class UserDao(AppDbContext context) : IUserDao
{
    private readonly AppDbContext _context = context;

    // Busca um usuário pelo ID. Retorna null se não encontrar.
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
    }

    // Retorna todos os usuários como uma lista.
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.Include(u => u.Role).AsNoTracking().ToListAsync();
    }

    // Adiciona o novo usuário ao contexto.
    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        // Salva as mudanças no banco de dados.
        await _context.SaveChangesAsync();
        // Retorna o usuário criado
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        // Marca o usuário como modificado no contexto.
        _context.Users.Update(user);
        // Salva as mudanças no banco de dados.
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        // Encontra o usuário pelo ID.
        var userToDelete = await _context.Users.FindAsync(id);
        // Se o usuário existir...
        if (userToDelete != null)
        {
            // Remove o usuário do contexto.
            _context.Users.Remove(userToDelete);
            // Salva as mudanças no banco de dados.
            await _context.SaveChangesAsync();
        }
        // Se o usuário não for encontrado, uma exceçao e chamada.
        else
        {
            throw new Exception("Usuario nao existe no banco de dados");
        }
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        // Busca o primeiro usuário que corresponde ao email fornecido.
        // Retorna null se nenhum usuário com esse email for encontrado.
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}