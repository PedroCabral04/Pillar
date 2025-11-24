namespace erp.DTOs.Tenancy;

public class TenantConfiguration
{
    public bool EnableHrModule { get; set; } = false;
    public bool EnableChatbot { get; set; } = false;
    public bool EnableCrm { get; set; } = true;
    public bool EnableInventory { get; set; } = true;
    public bool EnableFinancial { get; set; } = true;
    
    // Add other feature flags here
}
