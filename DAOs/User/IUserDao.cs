using erp.DTOs.User;

namespace erp.DAOs;

public interface IUserDao
{
    Task<Models.User?> GetByIdAsync(int id);
    Task<IEnumerable<Models.User>> GetAllAsync();
    Task<IEnumerable<UserDto>> GetAllAsyncProjected();
    
    Task<Models.User> CreateAsync(Models.User user);
    Task UpdateAsync(Models.User user);
    Task DeleteAsync(int id);
    Task<Models.User?> GetByEmailAsync(string email);
}
