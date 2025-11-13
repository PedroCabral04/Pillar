# Pillar ERP - AI Coding Assistant Instructions

## Project Overview

**Pillar** is a modular ERP system built with Blazor Server (.NET 9), MudBlazor UI, and PostgreSQL. The system manages users, inventory, sales, HR, and includes a personal Kanban board and AI chatbot assistant.

**Core Tech Stack:** .NET 9, Blazor Server (interactive), ASP.NET Core Identity, Entity Framework Core, PostgreSQL (Npgsql), MudBlazor, Semantic Kernel (AI chatbot)

## Architecture Patterns

### 1. Dual Identity System (Critical!)
The project uses **two separate user systems**:
- **ASP.NET Core Identity** (`ApplicationUser`/`ApplicationRole` with int keys) - for authentication/authorization, uses default `AspNetUsers` tables
- **Legacy User tables** (`User`/`Role`/`UserRole`) - custom tables for application-specific user data

When working with user management:
- Controllers use `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>` for Identity operations
- DAOs/Services may still reference legacy `User` model for backward compatibility
- See `Controllers/UserController.cs` and `Data/ApplicationDbContext.cs` for the dual-context pattern

### 2. Hybrid API + Blazor Architecture
- **Blazor Server pages** (`Components/Pages/**`) for UI with server-side rendering
- **API Controllers** (`Controllers/**`) provide REST endpoints for both Blazor components (via `ApiService`) and external integrations
- API routes follow `/api/{resource}` convention (e.g., `/api/users`, `/api/products`)
- Use `IApiService` (DI-injected) in Razor components for HTTP calls to backend APIs

### 3. Layer Separation
```
┌─ Components (UI)
│  └─ Calls → Services/ApiService → HTTP
│
├─ Controllers (API Layer)
│  └─ Uses → UserManager/RoleManager (Identity) or Domain Services
│
├─ Services (Business Logic)
│  └─ Uses → DAOs + ApplicationDbContext
│
└─ DAOs (Data Access)
   └─ Uses → ApplicationDbContext (EF Core)
```

**Key principle:** Controllers should use `UserManager<ApplicationUser>` directly for Identity operations, not custom services.

### 4. Mapperly for DTOs
- Use **Riok.Mapperly** (source generators) for entity ↔ DTO mapping
- Mappers are in `Mappings/` (e.g., `UserMapper.cs`, `ProductMapper.cs`)
- Decorate with `[Mapper]` and define `partial` methods
- Ignore sensitive fields like `PasswordHash` using `[MapperIgnoreTarget]`

### 5. DbContext Configuration Pattern
- `ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser, ApplicationRole, int>`
- Custom entities configured in `OnModelCreating` via dedicated methods:
  - `ConfigureInventoryModels()` - Products, Stock, Warehouses
  - `ConfigureSalesModels()` - Customers, Sales, SaleItems
  - `ConfigureHRModels()` - Departments, Positions
- Always use proper indexes, cascading rules, and MaxLength constraints

## Development Workflows

### Database & Migrations
```powershell
# Create migration (always check migration names follow pattern: YYYYMMDDHHmmss_Description)
dotnet ef migrations add YourMigrationName

# Apply migrations (auto-runs on startup via db.Database.Migrate())
dotnet ef database update

# Check last migration
ls Migrations | sort -Descending | select -First 1
```

**Important:** Connection string fallback in `ApplicationDbContext.OnConfiguring` is `Host=localhost;Database=erp;Username=postgres;Password=123`. Override with environment variable `ConnectionStrings__DefaultConnection` or `appsettings.json`.

### Building & Running
```powershell
# Restore packages
dotnet restore

# Build (check for compile errors)
dotnet build

# Run development server
dotnet run

# Access points:
# - UI: https://localhost:7051
# - Swagger: https://localhost:7051/swagger (dev only)
```

**Default dev credentials:** `admin@erp.local` / `Admin@123!` (seeded in `Program.cs` on first run)

### Tests
- Test project: `Tests/erp.Tests.csproj`
- Run tests: `dotnet test`

## Project-Specific Conventions

### Security & Authentication
1. **Cookie Hardening:** All cookies use `HttpOnly`, `SameSite=Lax`, `Secure=Always` (except dev mode allows `SameAsRequest`)
2. **Antiforgery:** Global via `app.UseAntiforgery()` - automatically applied to forms
3. **API Key Mode (Optional):** Middleware `Security/ApiKeyMiddleware.cs` checks `X-Api-Key` header on `/api/*` routes when `Security:ApiKey` is configured
4. **Authorization:** Use `[Authorize]` on controllers/pages. Role-based via `[Authorize(Roles = "Administrador")]`
5. **API Auth:** Controllers return JSON 401/403 instead of redirecting when path starts with `/api` (see cookie events in `Program.cs`)

### Password Management
- **BCrypt.Net** with work factor 12
- Password validation: min 8 chars, uppercase, lowercase, digit, special char
- Auto-generated temp passwords on user creation (see `UserService.GenerateRandomPassword`)
- Reset via `UserManager<ApplicationUser>.GeneratePasswordResetTokenAsync`

### AI Chatbot Integration
- Uses **Semantic Kernel** with plugin architecture
- Supports OpenAI (GPT) or Google AI (Gemini) via `AI:Provider` config
- Plugins in `Services/Chatbot/ChatbotPlugins/` define `[KernelFunction]` methods
- Falls back to template responses if no API key configured
- Configuration in `appsettings.json`:
  ```json
  {
    "AI": { "Provider": "google" },
    "GoogleAI": { "ApiKey": "...", "Model": "gemini-1.5-flash" }
  }
  ```

### Dashboard Widget System
- Provider pattern: `IDashboardWidgetProvider` interface
- Providers register widgets via `IDashboardRegistry`
- Each provider in `Services/Dashboard/Providers/{Domain}/`
- Widgets query via `/api/dashboard/query/{providerKey}/{widgetKey}`

### Notifications
- Two implementations:
  - `INotificationService` - simple in-app notifications
  - `IAdvancedNotificationService` - advanced with persistence and filtering

### Inventory Management
- Multi-warehouse support via `Warehouse` entity
- Stock movements tracked in `StockMovement` with types (In, Out, Adjustment, Transfer, Return)
- Stock counts (physical inventory) with approval workflow
- Products use SKU (unique), Barcode, and support NCM/CEST codes (Brazilian tax)

### Sales Module
- Customer management with Brazilian document validation (CPF/CNPJ)
- Sales have status workflow: Draft → Confirmed → Shipped → Completed → Cancelled
- SaleItems link to Products with quantity/price snapshot
- Payment methods tracked per sale

### HR Management (Recent Addition - Migration 20251103134934)
- Department hierarchy with manager assignment
- Positions with salary ranges
- Employee data on `ApplicationUser` (CPF, RG, hire date, contract type)
- Bank account and emergency contact fields

## Common Pitfalls

### ❌ Don't
- Mix `User` and `ApplicationUser` entities - use `ApplicationUser` for all Identity operations
- Call `SaveChangesAsync()` in DAOs when service needs transaction control - let service coordinate
- Forget `AsNoTracking()` on read-only queries (causes tracking overhead)
- Use plain HTTP in production (cookies require HTTPS)
- Hard-code connection strings (use environment variables for CI/CD)

### ✅ Do
- Use `UserManager<ApplicationUser>` and `RoleManager<ApplicationRole>` in controllers
- Include related entities with `.Include()` before projecting to DTOs
- Add `[ResponseCache(NoStore = true)]` on user/role endpoints to prevent stale data
- Sanitize phone numbers before saving (see `UserService.sanitizePhoneNumber`)
- Use proper cascade delete rules (Restrict for critical FK, Cascade for owned entities)

## Key Files Reference

| Purpose | File(s) |
|---------|---------|
| Startup & DI | `Program.cs` |
| DB Context | `Data/ApplicationDbContext.cs` |
| Identity Models | `Models/Identity/ApplicationUser.cs`, `ApplicationRole.cs` |
| Auth API | `Controllers/AuthController.cs` |
| User CRUD | `Controllers/UserController.cs`, `Services/User/UserService.cs` |
| Security Middleware | `Security/ApiKeyMiddleware.cs` |
| API Client | `Services/ApiService.cs` |
| Chatbot | `Services/Chatbot/ChatbotService.cs` |
| Dashboard | `Services/Dashboard/DashboardService.cs` |
| Mappings | `Mappings/*Mapper.cs` |
| UI Layout | `Components/Layout/NavMenu.razor` |
| Auth Docs | `DTOs/Auth/README.md` |

## Environment Configuration

### Required for Development
```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=erp;Username=postgres;Password=yourpassword"
```

### Optional API Key Protection
```bash
$env:Security__ApiKey = "your-strong-random-key"
```

### Chatbot Configuration
```bash
# Option 1: Google AI (Free tier available)
$env:AI__Provider = "google"
$env:GoogleAI__ApiKey = "your-google-ai-key"

# Option 2: OpenAI
$env:AI__Provider = "openai"
$env:OpenAI__ApiKey = "your-openai-key"
```

## Additional Notes

- **Portuguese Language:** UI and messages are in Brazilian Portuguese
- **Time Zones:** All timestamps use UTC (`DateTime.UtcNow`)
- **Logging:** Standard ASP.NET Core logging to console
- **Static Files:** Served from `wwwroot/`
- **Blazor Mode:** Interactive Server (not WASM or Auto)
- **Package Restore:** Required on first clone (`dotnet restore`)
- **Dev Environment:** Requires .NET 9 SDK and PostgreSQL 14+
