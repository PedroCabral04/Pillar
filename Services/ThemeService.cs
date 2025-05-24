using Blazored.LocalStorage; 
using System;
using System.Threading.Tasks;

namespace erp.Services;

public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    private const string ThemePreferenceKey = "themePreference";
    
    public bool IsDarkMode { get; set; } 
    public event Action? OnThemeChanged;

    public ThemeService(ILocalStorageService localStorageService)
    {
        _localStorage = localStorageService;
    }

    public async Task InitializeThemeAsync()
    {
        // Blazored.LocalStorage lida com o JS interop internamente.
        // É geralmente seguro chamar a partir de OnInitializedAsync se o componente
        // for interativo, mas OnAfterRenderAsync ainda é a prática mais segura para carga inicial.
        string? storedPreferenceValue = null;
        try
        {
            // Verificar se a preferência existe antes de tentar obtê-la
            if (await _localStorage.ContainKeyAsync(ThemePreferenceKey))
            {
                storedPreferenceValue = await _localStorage.GetItemAsync<string>(ThemePreferenceKey);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Isso pode acontecer se ainda for chamado muito cedo, mesmo com Blazored.
            // Um log aqui é importante.
            Console.WriteLine($"Error accessing local storage during InitializeThemeAsync: {ex.Message}");
            // Manter o valor padrão (false para IsDarkMode)
        }


        bool loadedDarkModeState = storedPreferenceValue == "dark";

        if (IsDarkMode != loadedDarkModeState)
        {
            IsDarkMode = loadedDarkModeState;
            OnThemeChanged?.Invoke();
        }
    }

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            try
            {
                await _localStorage.SetItemAsync(ThemePreferenceKey, IsDarkMode ? "dark" : "light");
            }
            catch (InvalidOperationException ex)
            {
                // Isso pode acontecer se ainda for chamado muito cedo, mesmo com Blazored.
                // Um log aqui é importante.
                Console.WriteLine($"Error accessing local storage during SetDarkModeAsync: {ex.Message}");
            }
            OnThemeChanged?.Invoke();
        }
    }
    
}