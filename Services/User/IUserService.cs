using erp.DAOs;
using erp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace erp.Services
{
    public interface IUserService
    {
        Task<User> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateAsync(User user, string password);
        Task UpdateAsync(User user, string password = null);
        Task DeleteAsync(int id);
        Task<User> AuthenticateAsync(string email, string password);
        
        // Métodos para gerenciamento de senha
        Task ResetPasswordAsync(int userId, string newPassword);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task UnlockUserAsync(int userId);
        Task<bool> IsPasswordValidAsync(string password); // Verifica se a senha atende aos requisitos
    }
}
