using System.Collections.Generic;

namespace erp.DTOs.Preferences
{
    public class UserPreferences
    {
        public UiPreferences Ui { get; set; } = new();
        public LocalePreferences Locale { get; set; } = new();
        public TablePreferences Tables { get; set; } = new();
        public DashboardPreferences Dashboard { get; set; } = new();
        public NotificationPreferences Notifications { get; set; } = new();
        public SecurityPreferences Security { get; set; } = new();
        public ExportPreferences Export { get; set; } = new();
    }

    public class UiPreferences
    {
        public bool DarkMode { get; set; } = false;
        public string Density { get; set; } = "comfortable"; // comfortable|compact
        public bool HighContrast { get; set; } = false;
        public bool ReduceMotion { get; set; } = false;
        public string FontSize { get; set; } = "base"; // small|base|large
        public string NavigationType { get; set; } = "sidebar"; // sidebar|topbar
        public string PrimaryColor { get; set; } = "#2563EB"; // Custom primary color
        public bool SidebarPersistent { get; set; } = true; // Sidebar fixed vs. hover
        public bool KeyboardShortcutsEnabled { get; set; } = true; // Enable/disable keyboard shortcuts
        // Sidebar UX state
        public List<string> PinnedRoutes { get; set; } = new();
        public Dictionary<string, bool> GroupExpanded { get; set; } = new();
    }

    public class LocalePreferences
    {
        public string Language { get; set; } = "pt-BR";
        public string TimeZone { get; set; } = "America/Sao_Paulo";
        public string DateFormat { get; set; } = "dd/MM/yyyy";
        public string NumberFormat { get; set; } = "1.234,56";
        public string Currency { get; set; } = "BRL";
    }

    public class TablePreferences
    {
        public int PageSize { get; set; } = 25;
        public Dictionary<string, string>? DefaultSortPerModule { get; set; } // ex.: { "Clientes": "Nome:asc" }
        public Dictionary<string, string[]>? VisibleColumnsPerModule { get; set; }
    }

    public class DashboardPreferences
    {
        public List<string> Widgets { get; set; } = new();
        public string? DefaultStartPage { get; set; } = "Dashboard";
    }

    public class NotificationPreferences
    {
        public bool InApp { get; set; } = true;
        public bool Email { get; set; } = false;
        public bool Sounds { get; set; } = false;
        public int Volume { get; set; } = 70; // 0-100
        public string Digest { get; set; } = "immediate"; // immediate|daily|weekly
        public string SoundFile { get; set; } = "notification.mp3"; // Sound file to play
    }

    public class SecurityPreferences
    {
        public bool TwoFactor { get; set; } = false;
        public int AutoLogoutMinutes { get; set; } = 30;
    }

    public class ExportPreferences
    {
        public string DefaultFormat { get; set; } = "XLSX"; // CSV|XLSX|PDF
        public bool PrintHeader { get; set; } = true;
    }
}
