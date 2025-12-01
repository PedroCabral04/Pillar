# Pillar ERP - AI Coding Agent Instructions

## Architecture Overview

**Pillar** is a modular Brazilian ERP system built with **Blazor Server**, **.NET 9**, **MudBlazor**, and **PostgreSQL**. It follows a layered architecture:

```
Components/Pages  →  Services  →  DAOs  →  ApplicationDbContext
     ↓                  ↓
Controllers (API)  →  DTOs/Mapperly
```

### Key Architectural Patterns

- **Multi-Tenancy**: Tenant resolved via subdomain (`{tenant}.pillar.local`), `X-Tenant` header, or user claims. See `Services/Tenancy/TenantResolutionMiddleware.cs`
- **Audit Trail**: All entity changes are automatically logged in `AuditLogs` via `ApplicationDbContext.SaveChangesAsync()` override
- **Mapperly for DTOs**: Use `[Mapper]` source generators in `Mappings/*.cs` instead of AutoMapper
- **ASP.NET Core Identity**: Custom `ApplicationUser`/`ApplicationRole` with int keys (not GUIDs)

## Project Structure

| Folder | Purpose |
|--------|---------|
| `Components/Pages/` | Blazor pages (`.razor`) - UI routes |
| `Components/Shared/` | Reusable Blazor components |
| `Controllers/` | API endpoints (`/api/*`) - JSON responses |
| `Services/` | Business logic with interface + implementation |
| `DAOs/` | Data access layer (Entity Framework queries) |
| `DTOs/` | Data transfer objects for API/service contracts |
| `Mappings/` | Mapperly mappers (compile-time DTO mapping) |
| `Models/` | EF Core entities |
| `Security/` | Custom middleware/filters (`ApiKeyMiddleware`, `AuditReadActionFilter`) |
| `Tests/` | xUnit tests with FluentAssertions, Moq, WebApplicationFactory |

## Development Commands

```bash
# Restore and run
dotnet restore
dotnet run

# Database migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Run tests
dotnet test Tests/erp.Tests.csproj

# Build for Docker
docker build -t pillar-erp .
```

**Dev URLs**: UI at `https://localhost:5001`, Swagger at `/swagger`, default admin: `admin@erp.local / Admin@123!`

## Coding Conventions

### Services Pattern
Always define an interface and implementation:
```csharp
// Services/Financial/IAccountReceivableService.cs
public interface IAccountReceivableService { ... }

// Services/Financial/AccountReceivableService.cs
public class AccountReceivableService : IAccountReceivableService { ... }
```
Register in `Program.cs` with `builder.Services.AddScoped<IXService, XService>();`

### DTOs & Mapperly
DTOs live in `DTOs/{Module}/` with naming: `{Entity}Dto`, `Create{Entity}Dto`, `Update{Entity}Dto`
```csharp
// Mappings/FinancialMapper.cs
[Mapper]
public partial class FinancialMapper
{
    public partial SupplierDto SupplierToDto(Supplier supplier);
}
```

### Controllers
- Route prefix: `[Route("api/{resource}")]`
- Return `ActionResult<T>` with proper status codes
- Use `[AuditRead]` for sensitive data endpoints
- Apply tenant scoping via `ITenantContextAccessor`

### Blazor Components
- Use `@inject` for DI, `@code { }` block for logic
- MudBlazor components: `MudTable`, `MudDialog`, `MudForm`, etc.
- Call APIs via `IApiService` (handles cookies/loading states)

### EF Core Models
- Implement `IAuditable` or `IMustHaveTenant` for automatic tracking
- Configure in `ApplicationDbContext.OnModelCreating()` via `Configure{Module}Models()`
- Fluent API for indexes, relationships, JSON columns (`HasColumnType("jsonb")`)

## Key Files to Understand

- `Program.cs` - DI registration, middleware pipeline, Identity config
- `Data/ApplicationDbContext.cs` - DbSets, audit logging, tenant filtering
- `Services/Tenancy/TenantResolutionMiddleware.cs` - Multi-tenant request handling
- `Components/Layout/MainLayout.razor` - App shell with navigation, theming
- `Services/ApiService.cs` - HTTP client wrapper with cookie forwarding

## Testing Patterns

Use `TestWebApplicationFactory` for integration tests with SQLite in-memory:
```csharp
public class MyControllerTests
{
    private readonly TestWebApplicationFactory _factory = new();
    
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var client = _factory.CreateClient();
        // ...
    }
}
```

Mock with `Moq`, assert with `FluentAssertions`. Tests are in `Tests/Controllers/` and `Tests/Services/`.

## Brazilian Business Rules

- CPF/CNPJ validation in `Services/Financial/Validation/BrazilianDocumentValidator.cs`
- Payroll with INSS/IRRF calculation in `Services/Payroll/PayrollCalculationService.cs`
- Address lookup via ViaCEP in `Services/Financial/Validation/ViaCepService.cs`

## Module Quick Reference

| Module | Key Entities | Service |
|--------|-------------|---------|
| Financial | `AccountPayable`, `AccountReceivable`, `Supplier` | `IAccountPayableService`, `IAccountReceivableService` |
| Inventory | `Product`, `StockMovement`, `Warehouse` | `IInventoryService`, `IStockMovementService` |
| Sales | `Sale`, `Customer`, `SaleItem` | `ISalesService`, `ICustomerService` |
| HR/Payroll | `PayrollPeriod`, `PayrollEntry`, `Department` | `IPayrollService`, `ITimeTrackingService` |
| Assets | `Asset`, `AssetAssignment`, `AssetMaintenance` | `IAssetService` |

## Chatbot & AI Integration

The system includes an AI-powered chatbot built with **Microsoft Semantic Kernel**. Located in `Services/Chatbot/`:

### Configuration (appsettings.json)
```json
{
  "AI": { "Provider": "openai" },  // openai, google, lmstudio, custom
  "OpenAI": { "ApiKey": "...", "Model": "gpt-4o-mini" },
  "GoogleAI": { "ApiKey": "...", "Model": "gemini-1.5-flash" },
  "LMStudio": { "Endpoint": "http://localhost:1234/v1", "Model": "local-model" },
  "CustomAI": { "Endpoint": "http://localhost:11434/v1", "Model": "llama3" }
}
```

### Semantic Kernel Plugins
Plugins expose ERP functionality to the AI. Located in `Services/Chatbot/ChatbotPlugins/`:

| Plugin | Purpose |
|--------|---------|
| `ProductsPlugin` | List/search/create products, check stock |
| `SalesPlugin` | Query sales data, create orders |
| `FinancialPlugin` | Accounts payable/receivable summaries |
| `HRPlugin` | Employee lookup, department queries |
| `SystemPlugin` | Help text, system info |

**Creating a new plugin:**
```csharp
public class MyPlugin
{
    private readonly IMyService _service;
    public MyPlugin(IMyService service) => _service = service;

    [KernelFunction, Description("What this function does")]
    public async Task<string> MyAction(
        [Description("Parameter description")] string param)
    {
        // Call service, return human-readable string
    }
}
```
Register in `ChatbotService.cs` constructor:
```csharp
builder.Plugins.AddFromObject(
    ActivatorUtilities.CreateInstance<MyPlugin>(serviceProvider), 
    "MyPlugin");
```

### UI Component
`Components/Shared/ChatbotWidget.razor` - Floating chat widget with:
- Conversation history (last 10 messages for context)
- Suggested action chips
- Typing indicator animation
- Dark mode support

## MudBlazor UI Conventions

### Dialog Pattern
All dialogs in `Components/Shared/Dialogs/` follow this structure:

```razor
@using MudBlazor

<MudDialog>
    <DialogContent>
        <MudForm @ref="_form" Model="@_model">
            <MudStack Spacing="3">
                <!-- Form fields with Variant="Variant.Outlined" -->
                <MudTextField @bind-Value="_model.Name"
                              Label="Nome"
                              Variant="Variant.Outlined"
                              Required="true"/>
            </MudStack>
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancelar</MudButton>
        <MudButton Color="MudBlazor.Color.Primary" 
                   Variant="Variant.Filled" 
                   OnClick="Submit" 
                   Disabled="_processing">
            @if (_processing) { <MudProgressCircular Size="Size.Small" Indeterminate="true"/> }
            else { <text>Salvar</text> }
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    // Parameters, form ref, model, etc.
    void Cancel() => MudDialog.Cancel();
    void Submit() => MudDialog.Close(DialogResult.Ok(true));
}
```

**Opening dialogs:**
```csharp
var parameters = new DialogParameters { { "EntityId", id }, { "IsEdit", true } };
var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
var dialog = await DialogService.ShowAsync<MyDialog>("Título", parameters, options);
var result = await dialog.Result;
if (result is { Canceled: false }) { /* refresh data */ }
```

### Table Pattern
Use `MudTable` with server-side pagination:

```razor
<MudTable T="MyDto" ServerData="ServerReload" @ref="_table"
          Hover="true" Dense="true" Striped="true">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Título</MudText>
        <MudSpacer/>
        <MudTextField @bind-Value="_searchString" 
                      Placeholder="Buscar..."
                      Adornment="Adornment.Start" 
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      OnDebounceIntervalElapsed="@(_ => _table?.ReloadServerData())"/>
    </ToolBarContent>
    <HeaderContent>
        <MudTh><MudTableSortLabel SortLabel="name" T="MyDto">Nome</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new int[] { 10, 25, 50, 100 }"/>
    </PagerContent>
</MudTable>
```

### Form Field Standards
- **Variant**: Always use `Variant.Outlined` for consistency
- **Labels**: Portuguese labels (e.g., "Nome", "Descrição", "Data de Nascimento")
- **Required fields**: Use `Required="true"` attribute
- **Validation**: Use `MudForm` with `For="@(() => _model.Property)"` for field-level validation

### Color Usage
- **Primary actions**: `Color.Primary` with `Variant.Filled`
- **Secondary actions**: `Color.Default` or no color specified
- **Destructive actions**: `Color.Error`
- **Success feedback**: `Color.Success`

### Snackbar Notifications
```csharp
@inject ISnackbar Snackbar

// Success
Snackbar.Add("Operação realizada com sucesso!", Severity.Success);

// Error
Snackbar.Add($"Erro: {ex.Message}", Severity.Error);

// Warning
Snackbar.Add("Atenção: verificar dados", Severity.Warning);
```

### Confirm Dialog Pattern
Use the shared `ConfirmDialog` for destructive actions:
```csharp
var parameters = new DialogParameters
{
    { "ContentText", "Tem certeza que deseja excluir?" },
    { "ButtonText", "Excluir" },
    { "Color", MudBlazor.Color.Error }
};
var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirmar", parameters);
var result = await dialog.Result;
if (result is { Canceled: false }) { /* proceed with delete */ }
```

## Deployment Notes

- Dockerfile exposes port 8080, uses `DB_BOOTSTRAP=true` for first-run schema creation
- Health endpoints: `/health` (liveness), `/ready` (DB connection check)
- Data Protection keys persisted via `DATAPROTECTION__KEYS_DIRECTORY` env var
- See `COOLIFY_DEPLOYMENT.md` for container orchestration details
