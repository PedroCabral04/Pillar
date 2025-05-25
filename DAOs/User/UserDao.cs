using erp.Data;
using erp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using erp.DTOs.User; // Adicionar este using

namespace erp.DAOs;

public class UserDao(ApplicationDbContext context) : IUserDao
{
    private readonly ApplicationDbContext _context = context;

    // Busca um usuário pelo ID. Retorna null se não encontrar.
    // Para GetByIdAsync, ainda pode ser útil retornar a entidade completa
    // ou um DTO mais detalhado, dependendo do uso.
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
    }

    // Modificado para retornar IEnumerable<UserDto> e selecionar apenas os campos necessários.
    public async Task<IEnumerable<UserDto>> GetAllAsyncProjected() // Renomeado para clareza ou poderia substituir o original
    {
        return await _context.Users
            .Include(u => u.Role) // O Include ainda é necessário para acessar u.Role.Name
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id, // <<< DESCOMENTE ESTA LINHA
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                RoleName = u.Role != null ? u.Role.Name : null // Garante que Role não é null
                // IsActive e RoleId do UserDto não estão sendo preenchidos aqui.
                // Se forem necessários para a listagem, adicione-os à projeção.
            })
            .ToListAsync();
    }

    // Mantenha a assinatura original de GetAllAsync se outras partes do sistema esperam a entidade User completa.
    // Caso contrário, você pode adaptar a interface IUserDao e o restante do sistema.
    // Por agora, estou adicionando um novo método GetAllAsyncProjected.
    // Se você quiser substituir o GetAllAsync original, você precisará atualizar IUserDao e UserService.
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
        // Se você só precisar de alguns campos aqui também, pode projetar.
        return await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
    }
}