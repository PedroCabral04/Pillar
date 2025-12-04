using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using erp.DTOs.Preferences;

namespace erp.Services
{
    public class PreferenceService
    {
        private readonly IApiService _apiService;
        private readonly ILocalStorageService _localStorage;
        public UserPreferences CurrentPreferences { get; private set; } = new();
        public event Action? OnPreferenceChanged;

        public PreferenceService(IApiService apiService, ILocalStorageService localStorage)
        {
            _apiService = apiService;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            var localPreferences = await _localStorage.GetItemAsync<UserPreferences>("userPreferences");
            if (localPreferences is not null)
            {
                CurrentPreferences = localPreferences;
            }

            try
            {
                var serverPreferences = await _apiService.GetAsync<UserPreferences>("api/preferences/me");
                if (serverPreferences != null)
                {
                    CurrentPreferences = serverPreferences;
                    await _localStorage.SetItemAsync("userPreferences", CurrentPreferences);
                }
            }
            catch
            {
                // API might be unavailable or user not logged in, proceed with local preferences
            }
            OnPreferenceChanged?.Invoke();
        }

        public async Task SaveAsync()
        {
            await _localStorage.SetItemAsync("userPreferences", CurrentPreferences);
            
            const int maxRetries = 3;
            Exception? lastException = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var response = await _apiService.PutAsync("api/preferences/me", CurrentPreferences);
                    if (response.IsSuccessStatusCode)
                    {
                        OnPreferenceChanged?.Invoke();
                        return;
                    }
                    
                    var error = await response.Content.ReadAsStringAsync();
                    
                    // If it's a concurrency error, retry
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                        error.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
                    {
                        if (attempt < maxRetries - 1)
                        {
                            await Task.Delay(50 * (attempt + 1));
                            continue;
                        }
                    }
                    
                    throw new HttpRequestException($"Server returned {response.StatusCode}: {error}");
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    // Only retry on concurrency-related errors
                    if (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
                    {
                        await Task.Delay(50 * (attempt + 1));
                        continue;
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving preferences: {ex.Message}");
                    throw;
                }
            }
            
            if (lastException != null)
                throw lastException;
                
            OnPreferenceChanged?.Invoke();
        }
    }
}
