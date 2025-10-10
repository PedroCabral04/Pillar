# Pillar ERP - AI Coding Agent Instructions

## Project Overview
Pillar is a modular ERP system built with **Blazor Server**, **.NET 9**, **MudBlazor**, and **ASP.NET Core Identity**. It features dashboard analytics, user administration, personal Kanban boards, and user preferences with robust cookie/CSRF security.

## Architecture & Key Patterns

### Dual Identity System
The project uses **two parallel identity systems** (legacy decision):
- **ASP.NET Core Identity** (`ApplicationUser`/`ApplicationRole` with `int` keys) - Active auth system using tables `AspNetUsers`, `AspNetRoles`, etc.
- **Legacy app tables** (`User`, `Role`, `UserRole`) - Still present in DbContext but being phased out

**Critical**: All new auth code must use `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>`, NOT the legacy `User` model. See `Controllers/UserController.cs` for correct patterns.

### Service Layer Architecture
- **ApiService Pattern**: Blazor components call backend APIs via `IApiService`, which automatically forwards authentication cookies from server-side HttpContext
  - Example: `Components/Pages/Admin/Users.razor` → `ApiService` → `Controllers/UserController.cs`
  - The ApiService wraps HttpClient and ensures cookie forwarding for authenticated API calls
  
- **Provider Pattern for Dashboard**: Modular widget system using `IDashboardWidgetProvider` interface
  - Each module (Sales, Finance) registers a provider in `Program.cs`
  - Registry (`IDashboardRegistry`) centralizes discovery and routing
  - See `Services/Dashboard/Providers/Sales/SalesDashboardProvider.cs` for implementation example

### Object Mapping
Uses **Riok.Mapperly** (source generator) for DTO ↔ Entity mapping:
- Mappers are partial classes with `[Mapper]` attribute
- Register as scoped services: `builder.Services.AddScoped<UserMapper, UserMapper>()`
- See `Mappings/UserMapper.cs` - note the `[MapperIgnoreTarget]` attributes for security-sensitive fields

### Data Access Pattern
- **No separate repository layer** - Services directly use `UserManager`/`RoleManager` for Identity entities
- **Legacy DAO pattern** (`DAOs/User/UserDao.cs`) exists but avoid for new code
- **EF Core direct access** for Kanban entities via `ApplicationDbContext`

## Development Workflow

### Running the Application
```powershell
# First time setup
dotnet restore
dotnet tool install --global dotnet-ef

# Database migrations (if needed)
dotnet ef migrations add MigrationName
dotnet ef database update

# Run the app
dotnet run
```

**URLs**:
- App: `https://localhost:7051` or `http://localhost:5121`
- Swagger (dev only): `https://localhost:7051/swagger`

**Default credentials** (dev seed): `admin@erp.local` / `Admin@123!`

### Database Configuration
PostgreSQL 14+ required. Configure via:
- `appsettings.json`: `"ConnectionStrings:DefaultConnection"`
- Environment variable: `ConnectionStrings__DefaultConnection` (double underscore)

Auto-migration runs on startup via `db.Database.Migrate()` in `Program.cs`.

## Security & Authentication

### Cookie Hardening (Already Applied)
- Auth cookie: `HttpOnly=true`, `SameSite=Lax`, `Secure=Always` (prod)
- CSRF token: `app.UseAntiforgery()` in pipeline
- Global cookie policy enforced in `Program.cs`

### Optional API Key Mode
For `/api/*` routes, enable via environment variable:
```powershell
$env:Security__ApiKey = "your-strong-key"
```
Clients must send `X-Api-Key` header. See `Security/ApiKeyMiddleware.cs`.

### Authorization Patterns
- Pages: Use `@attribute [Authorize]` at top of `.razor` files
- Controllers: Use `[Authorize]` on class or methods
- Roles: Check via `UserManager.IsInRoleAsync()` or claims-based policies

## UI Component Patterns

### MudBlazor Conventions
- **Server-side tables**: Use `<MudTable T="..." ServerData="ServerReload">` for paginated data
  - Example: `Components/Pages/Admin/Users.razor` with search/filter/pagination
  - Always implement `ServerReload` method returning `Task<TableData<T>>`
  
- **Dialogs**: Use `IDialogService.ShowAsync<TDialog>(...)` with dedicated dialog components in `Components/Shared/Dialogs/`
  - Dialog components inherit from `MudDialog` base
  - Return data via `MudDialog.Close(DialogResult.Ok(data))`
  - See `Components/Shared/Dialogs/UserCreateDialog.razor` for form validation patterns

- **Icons**: Use semantic icons from `Icons.Material.Filled.*` / `Icons.Material.Outlined.*`
  - Roles: `AdminPanelSettings` (Admin), `ManageAccounts` (Manager), `StoreMallDirectory` (Vendor)

### Form Validation
- Use `<MudForm @ref="_form" Model="@_model" Validation="..." />`
- Real-time validation: `Immediate="true"` on inputs
- Server-side validation in DTOs with `[Required]`, `[EmailAddress]`, etc.

## Project-Specific Conventions

### File Organization
- **Components/Pages/**: Routable Blazor pages (use `@page "/route"`)
- **Components/Shared/**: Reusable components, dialogs, charts
- **Components/Layout/**: Shell layouts (`MainLayout`, `AuthLayout`, `NavMenu`)
- **DTOs/**: Request/response models grouped by feature (Auth, Dashboard, User, etc.)
- **Controllers/**: ASP.NET Core API controllers (suffix: `Controller.cs`)
- **Services/**: Business logic and external integrations
  - Scoped services for user-specific state (PreferenceService, ThemeService)
  - Dashboard services use subdirectory pattern: `Services/Dashboard/Providers/`

### Naming Conventions
- **Controllers**: Plural names (`UsersController`, `RolesController`)
- **API routes**: `/api/{resource}` (lowercase, plural)
- **Razor components**: PascalCase, match filename (`UserCreateDialog.razor`)
- **Private fields**: `_camelCase` with underscore prefix

### State Management
- **User preferences**: Stored in `UserPreferences` JSON column on `ApplicationUser`, synced to `Blazored.LocalStorage`
  - Managed by `PreferenceService` with event notifications (`OnPreferenceChanged`)
- **Theme**: Managed by `ThemeService`, persisted in preferences
- **Notifications**: Use `ISnackbar` (MudBlazor) for user feedback, `INotificationService` for app-level notifications

## Common Tasks

### Adding a New API Endpoint
1. Add method to appropriate controller in `Controllers/`
2. Use `[HttpGet]`, `[HttpPost]`, etc. with route template
3. Add `[Authorize]` if authentication required
4. Return `ActionResult<T>` for type safety
5. Update Blazor component to call via `IApiService`

### Creating a New Dashboard Widget
1. Implement `IDashboardWidgetProvider` in `Services/Dashboard/Providers/{Module}/`
2. Return `DashboardWidgetDefinition` with `ProviderKey` and `WidgetKey`
3. Implement `QueryAsync` to return `ChartDataResponse`
4. Register provider in `Program.cs`: `builder.Services.AddScoped<IDashboardWidgetProvider, YourProvider>()`

### Adding a New Migration
```powershell
dotnet ef migrations add YourMigrationName
dotnet ef database update
```
**Note**: Migrations folder is in `.gitignore` - this is intentional for dev flexibility.

## Integration Points

### External Dependencies
- **PostgreSQL**: Primary database (Npgsql provider)
- **MudBlazor**: Complete UI component library
- **ApexCharts**: Dashboard charting (`Blazor-ApexCharts` package)
- **Blazored.LocalStorage**: Client-side storage for preferences

### Browser JavaScript
- Minimal JS in `wwwroot/js/auth.js` for login flow enhancements
- Use `IJSRuntime` for interop when needed, but prefer C# solutions

## Troubleshooting Notes

- **"Cookie not found" errors**: Check `CookieSecurePolicy` matches your dev environment (HTTP vs HTTPS)
- **CSRF validation fails**: Ensure `app.UseAntiforgery()` is AFTER `app.UseAuthentication()` in `Program.cs`
- **Identity tables missing**: Run `dotnet ef database update` to apply migrations
- **Dual User models confusion**: Always use `ApplicationUser` (Identity), not `User` (legacy)

## Key Files Reference
- **App entry**: `Program.cs` - Complete middleware pipeline and DI registration
- **Auth config**: `DTOs/Auth/README.md` - Security guidelines and patterns
- **DB context**: `Data/ApplicationDbContext.cs` - EF Core configuration
- **API patterns**: `Services/ApiService.cs` - Cookie-forwarding HTTP client
- **Main nav**: `Components/Layout/NavMenu.razor` - Site navigation structure
- **User admin**: `Components/Pages/Admin/Users.razor` - Server-side table example
