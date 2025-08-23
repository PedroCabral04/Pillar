using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using erp.DTOs.Preferences;

namespace erp.Services
{
    public class PreferenceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        public UserPreferences CurrentPreferences { get; private set; } = new();

        public PreferenceService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
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
                var serverPreferences = await _httpClient.GetFromJsonAsync<UserPreferences>("api/preferences/me");
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
        }

        public async Task SaveAsync()
        {
            await _localStorage.SetItemAsync("userPreferences", CurrentPreferences);
            try
            {
                await _httpClient.PutAsJsonAsync("api/preferences/me", CurrentPreferences);
            }
            catch
            {
                // Handle case where user is offline
            }
        }
    }
}
