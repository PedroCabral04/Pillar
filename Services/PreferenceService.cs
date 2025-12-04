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
            Console.WriteLine("[PreferenceService] InitializeAsync started");
            
            var localPreferences = await _localStorage.GetItemAsync<UserPreferences>("userPreferences");
            if (localPreferences is not null)
            {
                CurrentPreferences = localPreferences;
                Console.WriteLine($"[PreferenceService] Loaded from localStorage - DefaultStartPage: '{localPreferences.Dashboard.DefaultStartPage}'");
            }
            else
            {
                Console.WriteLine("[PreferenceService] No preferences in localStorage");
            }

            try
            {
                var serverPreferences = await _apiService.GetAsync<UserPreferences>("api/preferences/me");
                if (serverPreferences != null)
                {
                    Console.WriteLine($"[PreferenceService] Loaded from server - DefaultStartPage: '{serverPreferences.Dashboard.DefaultStartPage}'");
                    CurrentPreferences = serverPreferences;
                    await _localStorage.SetItemAsync("userPreferences", CurrentPreferences);
                }
                else
                {
                    Console.WriteLine("[PreferenceService] Server returned null preferences");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PreferenceService] Error loading from server: {ex.Message}");
                // API might be unavailable or user not logged in, proceed with local preferences
            }
            
            Console.WriteLine($"[PreferenceService] Final DefaultStartPage: '{CurrentPreferences.Dashboard.DefaultStartPage}'");
            OnPreferenceChanged?.Invoke();
        }

        public async Task SaveAsync()
        {
            Console.WriteLine($"[PreferenceService] SaveAsync called - DefaultStartPage: '{CurrentPreferences.Dashboard.DefaultStartPage}'");
            await _localStorage.SetItemAsync("userPreferences", CurrentPreferences);
            
            const int maxRetries = 3;
            Exception? lastException = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"[PreferenceService] Attempting to save to server (attempt {attempt + 1})...");
                    var response = await _apiService.PutAsync("api/preferences/me", CurrentPreferences);
                    Console.WriteLine($"[PreferenceService] Server response: {response.StatusCode}");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("[PreferenceService] Preferences saved successfully!");
                        OnPreferenceChanged?.Invoke();
                        return;
                    }
                    
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PreferenceService] Server error: {error}");
                    
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
                    Console.WriteLine($"[PreferenceService] HttpRequestException: {ex.Message}");
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
                    Console.WriteLine($"[PreferenceService] Error saving preferences: {ex.Message}");
                    throw;
                }
            }
            
            if (lastException != null)
                throw lastException;
                
            OnPreferenceChanged?.Invoke();
        }
    }
}
