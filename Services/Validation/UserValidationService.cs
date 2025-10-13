using erp.Services;

namespace erp.Services.Validation;

public interface IUserValidationService
{
    Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null);
    Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
}

public class UserValidationService : IUserValidationService
{
    private readonly IApiService _apiService;
    private readonly Dictionary<string, (bool result, DateTime timestamp)> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(5);

    public UserValidationService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var cacheKey = $"email:{email}:{excludeUserId}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.timestamp < _cacheExpiration)
            {
                return cached.result;
            }
            _cache.Remove(cacheKey);
        }

        try
        {
            var endpoint = $"api/users/validate/email?email={Uri.EscapeDataString(email)}";
            if (excludeUserId.HasValue)
            {
                endpoint += $"&excludeUserId={excludeUserId.Value}";
            }

            var response = await _apiService.GetAsync(endpoint);
            var isAvailable = response.IsSuccessStatusCode;

            // Cache the result
            _cache[cacheKey] = (isAvailable, DateTime.UtcNow);

            return isAvailable;
        }
        catch
        {
            // Em caso de erro, assumimos que está disponível para não bloquear o usuário
            return true;
        }
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var cacheKey = $"username:{username}:{excludeUserId}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.timestamp < _cacheExpiration)
            {
                return cached.result;
            }
            _cache.Remove(cacheKey);
        }

        try
        {
            var endpoint = $"api/users/validate/username?username={Uri.EscapeDataString(username)}";
            if (excludeUserId.HasValue)
            {
                endpoint += $"&excludeUserId={excludeUserId.Value}";
            }

            var response = await _apiService.GetAsync(endpoint);
            var isAvailable = response.IsSuccessStatusCode;

            // Cache the result
            _cache[cacheKey] = (isAvailable, DateTime.UtcNow);

            return isAvailable;
        }
        catch
        {
            // Em caso de erro, assumimos que está disponível para não bloquear o usuário
            return true;
        }
    }
}
