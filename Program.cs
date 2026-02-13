using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using System;
using System.Globalization;
using MudBlazor.Services;
using erp.Components;
using erp.DAOs;
using erp.Data;
using erp.Extensions;
using erp.Mappings;
using erp.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using erp.Security;
using erp.Models.Identity;
using erp.Models.Tenancy;
using erp.Services.Dashboard;
using erp.Services.Dashboard.Providers.Sales;
using erp.Services.Dashboard.Providers.Finance;
using erp.Services.Dashboard.Providers.Inventory;
using erp.Services.Sales;
using erp.Services.Seeding;
using erp.Services.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Microsoft.Extensions.Options;
using ApexCharts;
using Microsoft.Extensions.Logging;

// Prefer using DotNetEnv to load .env into environment variables in dev.
// This keeps the bootstrap simple and delegates parsing to a tested library.
// If DotNetEnv is not available the call will be no-op at runtime only if
// you choose not to reference the package; we add the package in the project.
try
{
    // Loads .env from current directory by default and sets variables for the process
    DotNetEnv.Env.Load();
}
catch
{
    // If DotNetEnv isn't present for any reason, continue without failing.
}

var builder = WebApplication.CreateBuilder(args);

// --- Configure Serviços ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

// Configure Authorization with module-based policies
builder.Services.AddAuthorization(options =>
{
    // Module-based authorization policies
    options.AddPolicy(ModulePolicies.Dashboard, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Dashboard)));
    options.AddPolicy(ModulePolicies.Sales, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Sales)));
    options.AddPolicy(ModulePolicies.ServiceOrder, policy =>
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.ServiceOrder)));
    options.AddPolicy(ModulePolicies.Inventory, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Inventory)));
    options.AddPolicy(ModulePolicies.Financial, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Financial)));
    options.AddPolicy(ModulePolicies.HR, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.HR)));
    options.AddPolicy(ModulePolicies.Assets, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Assets)));
    options.AddPolicy(ModulePolicies.Kanban, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Kanban)));
    options.AddPolicy(ModulePolicies.Reports, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Reports)));
    options.AddPolicy(ModulePolicies.Admin, policy => 
        policy.Requirements.Add(new ModuleAccessRequirement(ModuleKeys.Admin)));
});

// Register authorization handler
builder.Services.AddScoped<IAuthorizationHandler, ModuleAccessHandler>();
builder.Services.AddMemoryCache();

// CORS Configuration - Security: Restrict cross-origin requests
var allowedOrigins = builder.Configuration["Security:CorsAllowedOrigins"];
if (!string.IsNullOrEmpty(allowedOrigins))
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = allowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (origins.Contains("*"))
            {
                // SECURITY: Block wildcard CORS in production
                if (builder.Environment.IsProduction())
                {
                    throw new InvalidOperationException(
                        "CORS wildcard (*) is not allowed in production. " +
                        "Please configure specific origins in Security:CorsAllowedOrigins.");
                }

                // WARNING: AllowAll origins is not secure for production
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.WithOrigins(origins)
                      .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN")
                      .AllowCredentials();
            }
        });
    });
}
else
{
    // Default policy for development - allow same origin only
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production: same origin only by default
                policy.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                      .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN");
            }
        });
    });
}

// Data Protection keys persistence (optional, via env DATAPROTECTION__KEYS_DIRECTORY)
var dataProtectionKeysDir = Environment.GetEnvironmentVariable("DATAPROTECTION__KEYS_DIRECTORY");
if (!string.IsNullOrWhiteSpace(dataProtectionKeysDir))
{
    Directory.CreateDirectory(dataProtectionKeysDir);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDir));
}

// Identity com chaves int
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.User.RequireUniqueEmail = true;
        
        // Configurações de Two-Factor Authentication (2FA)
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        options.SignIn.RequireConfirmedAccount = false; // Set to true if you want email confirmation
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders(); // Adiciona provedores de token padrão incluindo authenticator

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.Cookie.Name = "erp.auth";

        // SECURITY: Configuração de expiração de cookie (configurável via appsettings)
        var sessionHours = builder.Configuration.GetValue<int>("Security:SessionExpirationHours", 8);
        options.ExpireTimeSpan = TimeSpan.FromHours(sessionHours);
        options.SlidingExpiration = builder.Configuration.GetValue<bool>("Security:SlidingExpiration", true);

    // Harden cookie
    options.Cookie.HttpOnly = true; // Previne acesso via JavaScript (anti-XSS)

        // SECURITY: SameSite configuration
        // Lax é necessário para Blazor Server funcionar com navegadores modernos
        // Strict pode ser usado para endpoints de API sensíveis se necessário
        var sameSiteMode = builder.Configuration.GetValue<string>("Security:SameSiteMode", "Lax");
        options.Cookie.SameSite = Enum.Parse<SameSiteMode>(sameSiteMode);

        // SECURITY: SecurePolicy - Always em produção quando HTTPS está habilitado
        if (builder.Environment.IsProduction())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        else
        {
            // Desenvolvimento: SameAsRequest para suportar TLS termination
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }

    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true; // ensure cookie not blocked by consent if used

    // Configure API-specific events to return JSON instead of redirecting
    options.Events.OnRedirectToLogin = context =>
    {
        // If it's an API request, return 401 instead of redirecting
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Unauthorized\",\"message\":\"Authentication required\"}");
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        // If it's an API request, return 403 instead of redirecting
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Forbidden\",\"message\":\"Access denied\"}");
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    })
    // Adicionar esquemas de cookie para 2FA
    .AddCookie(IdentityConstants.TwoFactorUserIdScheme, options =>
    {
        options.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, options =>
    {
        options.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

// Registra o filtro de auditoria de leitura
builder.Services.AddScoped<erp.Security.AuditReadActionFilter>();

builder.Services.AddControllers(options =>
{
    // Adiciona o filtro globalmente para interceptar [AuditRead]
    options.Filters.AddService<erp.Security.AuditReadActionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Pillar ERP API",
        Description = "Sistema ERP modular desenvolvido com Blazor Server e ASP.NET Core",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Suporte Pillar ERP",
            Email = "suporte@pillar-erp.com"
        }
    });
    
    // Resolve naming conflicts by using fully qualified type names for schema IDs
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    
    // Configura Swagger para usar XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
    
    // Adiciona suporte para autenticação JWT/Cookie no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Cookie de autenticação do ASP.NET Core Identity",
        Name = "Cookie",
        In = Microsoft.OpenApi.Models.ParameterLocation.Cookie,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache(); // Required for rate limiting in ApiKeyMiddleware
builder.Services.Configure<DemoSeedOptions>(builder.Configuration.GetSection("DemoSeed"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped(sp => {
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var httpClientHandler = new HttpClientHandler();
    
    // In development, bypass SSL certificate validation for self-signed certs
    if (builder.Environment.IsDevelopment())
    {
        httpClientHandler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    
    return new HttpClient(httpClientHandler)
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

// Configuração global de cultura brasileira para formatação de datas e números
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Adiciona serviços de terceiros.
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
});
builder.Services.AddApexCharts();
builder.Services.AddBlazoredLocalStorage();

// Antiforgery hardening (used by UseAntiforgery)
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    // Use SameAsRequest even in production when behind a reverse proxy (Coolify/nginx)
    // that terminates TLS and forwards HTTP internally with X-Forwarded-Proto
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.Cookie.Name = "erp.csrf";
    // HeaderName can be customized if you post forms via JS: o.HeaderName = "X-CSRF-TOKEN";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? builder.Configuration["DbContextSettings:ConnectionString"];

// SECURITY: Valida que connection string está configurada em produção
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string not configured. Please set ConnectionStrings__DefaultConnection environment variable.");
}

// SECURITY: Validate admin password is configured in production
var adminPassword = builder.Configuration["Security:DefaultAdminPassword"];
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(adminPassword))
{
    throw new InvalidOperationException(
        "Default admin password not configured. Please set Security__DefaultAdminPassword environment variable.");
}

// Use extension methods to validate and configure database
builder.Services.ValidateConnectionString(connectionString, builder.Environment);

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsqlWithMigrations(connectionString, typeof(ApplicationDbContext).Assembly);
});

builder.Services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsqlWithMigrations(connectionString, typeof(ApplicationDbContext).Assembly);
}, ServiceLifetime.Scoped);

// Registra DAOs e Serviços
builder.Services.AddScoped<PreferenceService>();
builder.Services.AddScoped<CurrencyFormatService>();
builder.Services.AddScoped<ThemeService>();

// Register HttpClient for ApiService with proper configuration
builder.Services.AddHttpClient<IApiService, ApiService>((serviceProvider, client) =>
{
    // Get the current request's base URL for Blazor Server
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    
    if (httpContext != null)
    {
        var request = httpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        client.BaseAddress = new Uri(baseUrl);
    }
    else
    {
        // Fallback for cases where HttpContext is not available
        // This should work for most dev scenarios
        client.BaseAddress = new Uri("http://localhost:5121");
    }
    
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // In development, ignore SSL certificate errors
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    return handler;
});

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<erp.Services.Notifications.IAdvancedNotificationService, erp.Services.Notifications.AdvancedNotificationService>();
builder.Services.AddScoped<ITablePreferenceService, TablePreferenceService>();
// Dashboard services
builder.Services.AddScoped<IDashboardRegistry, DashboardRegistry>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDashboardWidgetProvider, SalesDashboardProvider>();
builder.Services.AddScoped<IDashboardWidgetProvider, FinanceDashboardProvider>();
builder.Services.AddScoped<IDashboardWidgetProvider, erp.Services.Dashboard.Providers.Inventory.InventoryDashboardProvider>();
builder.Services.AddScoped<IDashboardWidgetProvider, erp.Services.Dashboard.Providers.HR.HRDashboardProvider>();
builder.Services.AddScoped<erp.Services.DashboardCustomization.IDashboardLayoutService, erp.Services.DashboardCustomization.DashboardLayoutService>();
// Validation services
builder.Services.AddScoped<erp.Services.Validation.IUserValidationService, erp.Services.Validation.UserValidationService>();
// Onboarding services
builder.Services.AddScoped<erp.Services.Onboarding.IOnboardingService, erp.Services.Onboarding.OnboardingService>();
// Onboarding resume helper (per-circuit scoped)
builder.Services.AddScoped<erp.Services.Onboarding.IOnboardingResumeService, erp.Services.Onboarding.OnboardingResumeService>();

// Inventory services
builder.Services.AddScoped<erp.Services.Inventory.IInventoryService, erp.Services.Inventory.InventoryService>();
builder.Services.AddScoped<erp.Services.Inventory.IStockMovementService, erp.Services.Inventory.StockMovementService>();
builder.Services.AddScoped<erp.Services.Inventory.IStockCountService, erp.Services.Inventory.StockCountService>();

// Sales services
builder.Services.AddScoped<erp.Services.Sales.ICustomerService, erp.Services.Sales.CustomerService>();
builder.Services.AddScoped<erp.Services.Sales.ISalesService, erp.Services.Sales.SalesService>();

// Service Orders services
builder.Services.AddScoped<erp.Services.ServiceOrders.IServiceOrderService, erp.Services.ServiceOrders.ServiceOrderService>();

// Financial services
builder.Services.Configure<erp.Services.Financial.FinancialOptions>(
    builder.Configuration.GetSection(erp.Services.Financial.FinancialOptions.SectionName));
builder.Services.AddScoped<erp.Services.Financial.IAccountingService, erp.Services.Financial.AccountingService>();
builder.Services.AddScoped<erp.Services.Financial.ISupplierService, erp.Services.Financial.SupplierService>();
builder.Services.AddScoped<erp.Services.Financial.IFinancialCategoryService, erp.Services.Financial.FinancialCategoryService>();
builder.Services.AddScoped<erp.Services.Financial.ICostCenterService, erp.Services.Financial.CostCenterService>();
builder.Services.AddScoped<erp.Services.Financial.IAccountReceivableService, erp.Services.Financial.AccountReceivableService>();
builder.Services.AddScoped<erp.Services.Financial.IAccountPayableService, erp.Services.Financial.AccountPayableService>();
builder.Services.AddScoped<erp.Services.Financial.IFinancialDashboardService, erp.Services.Financial.FinancialDashboardService>();
builder.Services.AddScoped<erp.Services.Financial.ICommissionService, erp.Services.Financial.CommissionService>();
builder.Services.AddScoped<erp.Services.Financial.ISalesGoalService, erp.Services.Financial.SalesGoalService>();
builder.Services.AddScoped<erp.Services.Financial.IVendorPerformanceService, erp.Services.Financial.VendorPerformanceService>();

// Financial DAOs
builder.Services.AddScoped<erp.DAOs.Financial.ISupplierDao, erp.DAOs.Financial.SupplierDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IFinancialCategoryDao, erp.DAOs.Financial.FinancialCategoryDao>();
builder.Services.AddScoped<erp.DAOs.Financial.ICostCenterDao, erp.DAOs.Financial.CostCenterDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IAccountReceivableDao, erp.DAOs.Financial.AccountReceivableDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IAccountPayableDao, erp.DAOs.Financial.AccountPayableDao>();
builder.Services.AddScoped<erp.DAOs.Financial.ICommissionDao, erp.DAOs.Financial.CommissionDao>();
builder.Services.AddScoped<erp.DAOs.Financial.ISalesGoalDao, erp.DAOs.Financial.SalesGoalDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IVendorPerformanceDao, erp.DAOs.Financial.VendorPerformanceDao>();

// Financial validation services (BrazilianDocumentValidator is static, no DI needed)
builder.Services.AddHttpClient<erp.Services.Financial.Validation.IViaCepService, erp.Services.Financial.Validation.ViaCepService>();
builder.Services.AddHttpClient<erp.Services.Financial.Validation.IReceitaWsService, erp.Services.Financial.Validation.ReceitaWsService>();

// Asset Management services
builder.Services.AddScoped<erp.DAOs.Assets.IAssetDao, erp.DAOs.Assets.AssetDao>();
builder.Services.AddScoped<erp.Services.Assets.IAssetService, erp.Services.Assets.AssetService>();
builder.Services.AddScoped<erp.Services.Assets.IQRCodeService, erp.Services.Assets.QRCodeService>();
builder.Services.AddScoped<erp.Services.Assets.IFileStorageService, erp.Services.Assets.LocalFileStorageService>();
builder.Services.AddScoped<erp.Security.IFileValidationService, erp.Security.FileValidationService>();
builder.Services.AddScoped<erp.Mappings.AssetMapper>();

// Time tracking services
builder.Services.AddScoped<erp.Services.TimeTracking.ITimeTrackingService, erp.Services.TimeTracking.TimeTrackingService>();

// Payroll services
builder.Services.AddScoped<erp.Services.Payroll.IPayrollCalculationService, erp.Services.Payroll.PayrollCalculationService>();
builder.Services.AddScoped<erp.Services.Payroll.IPayrollSlipService, erp.Services.Payroll.PayrollSlipService>();
builder.Services.AddScoped<erp.Services.Payroll.IPayrollService, erp.Services.Payroll.PayrollService>();

// Audit services
builder.Services.AddScoped<erp.Services.Audit.IAuditService, erp.Services.Audit.AuditService>();
builder.Services.AddScoped<erp.Services.Audit.IAuditRetentionService, erp.Services.Audit.AuditRetentionService>();

// Authorization / Permission services
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Chatbot services
builder.Services.AddSingleton<erp.Services.Chatbot.IChatbotCacheService, erp.Services.Chatbot.ChatbotCacheService>();
builder.Services.AddScoped<erp.Services.Chatbot.IChatbotService, erp.Services.Chatbot.ChatbotService>();
builder.Services.AddScoped<erp.Services.Chatbot.IChatConversationService, erp.Services.Chatbot.ChatConversationService>();
builder.Services.AddScoped<erp.DAOs.Chatbot.IChatConversationDao, erp.DAOs.Chatbot.ChatConversationDao>();
builder.Services.AddScoped<erp.Services.Chatbot.IChatbotUserContext, erp.Services.Chatbot.ChatbotUserContext>();

// Browser Service (Mobile/Responsive)
builder.Services.AddScoped<erp.Services.Browser.IBrowserService, erp.Services.Browser.BrowserService>();

// Tenancy services
builder.Services.AddScoped<erp.Services.Tenancy.ITenantService, erp.Services.Tenancy.TenantService>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantBrandingService, erp.Services.Tenancy.TenantBrandingService>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantContextAccessor, erp.Services.Tenancy.TenantContextAccessor>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantResolver, erp.Services.Tenancy.DefaultTenantResolver>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantBrandingProvider, erp.Services.Tenancy.TenantBrandingProvider>();

// Administration services
builder.Services.AddScoped<erp.Services.Administration.IDepartmentService, erp.Services.Administration.DepartmentService>();
builder.Services.AddScoped<erp.DAOs.Administration.IDepartmentDao, erp.DAOs.Administration.DepartmentDao>();
builder.Services.AddScoped<erp.Services.Administration.IPositionService, erp.Services.Administration.PositionService>();
builder.Services.AddScoped<erp.DAOs.Administration.IPositionDao, erp.DAOs.Administration.PositionDao>();

// Kanban services
builder.Services.AddScoped<erp.Services.Kanban.IKanbanService, erp.Services.Kanban.KanbanService>();
builder.Services.AddScoped<erp.DAOs.Kanban.IKanbanDao, erp.DAOs.Kanban.KanbanDao>();

// Inventory DAOs
builder.Services.AddScoped<erp.DAOs.Inventory.IProductDao, erp.DAOs.Inventory.ProductDao>();
builder.Services.AddScoped<erp.DAOs.Inventory.IProductCategoryDao, erp.DAOs.Inventory.ProductCategoryDao>();
builder.Services.AddScoped<erp.DAOs.Inventory.IStockMovementDao, erp.DAOs.Inventory.StockMovementDao>();
builder.Services.AddScoped<erp.DAOs.Inventory.IWarehouseDao, erp.DAOs.Inventory.WarehouseDao>();

// Sales DAOs
builder.Services.AddScoped<erp.DAOs.Sales.ICustomerDao, erp.DAOs.Sales.CustomerDao>();
builder.Services.AddScoped<erp.DAOs.Sales.ISaleDao, erp.DAOs.Sales.SaleDao>();

// Payroll DAOs
builder.Services.AddScoped<erp.DAOs.Payroll.IPayrollPeriodDao, erp.DAOs.Payroll.PayrollPeriodDao>();
builder.Services.AddScoped<erp.DAOs.Payroll.IPayrollEntryDao, erp.DAOs.Payroll.PayrollEntryDao>();

// Service Orders DAOs
builder.Services.AddScoped<erp.DAOs.ServiceOrders.IServiceOrderDao, erp.DAOs.ServiceOrders.ServiceOrderDao>();

// Email services
builder.Services.Configure<erp.Services.Email.EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<erp.Services.Email.IEmailService, erp.Services.Email.EmailService>();

// Report services
builder.Services.AddScoped<erp.Services.Reports.ISalesReportService, erp.Services.Reports.SalesReportService>();
builder.Services.AddScoped<erp.Services.Reports.IFinancialReportService, erp.Services.Reports.FinancialReportService>();
builder.Services.AddScoped<erp.Services.Reports.IInventoryReportService, erp.Services.Reports.InventoryReportService>();
builder.Services.AddScoped<erp.Services.Reports.IHRReportService, erp.Services.Reports.HRReportService>();
builder.Services.AddScoped<erp.Services.Reports.IPdfExportService, erp.Services.Reports.PdfExportService>();
builder.Services.AddScoped<erp.Services.Reports.IExcelExportService, erp.Services.Reports.ExcelExportService>();

// ------- REGISTRO DO MAPPERLY -------
builder.Services.AddScoped<ProductMapper, ProductMapper>();
builder.Services.AddScoped<StockMovementMapper, StockMovementMapper>();
builder.Services.AddScoped<StockCountMapper, StockCountMapper>();
builder.Services.AddScoped<SalesMapper, SalesMapper>();
builder.Services.AddScoped<ServiceOrderMapper, ServiceOrderMapper>();
builder.Services.AddScoped<TimeTrackingMapper, TimeTrackingMapper>();
builder.Services.AddScoped<PayrollMapper, PayrollMapper>();
builder.Services.AddScoped<erp.Mappings.FinancialMapper, erp.Mappings.FinancialMapper>();
builder.Services.AddScoped<erp.Mappings.TenantMapper, erp.Mappings.TenantMapper>();

// --- Constrói a aplicação ---
var app = builder.Build();

// --- Configura o pipeline de requisições HTTP ---

// CRITICAL: UseForwardedHeaders MUST be first so X-Forwarded-Proto is processed
// before any middleware that checks Request.Scheme (like antiforgery, HTTPS redirection)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
// When running inside containers behind a proxy the proxy's IP may not be known
// to the runtime. Clear known networks/proxies so the forwarded headers are accepted.
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Global exception handling - must be early to catch exceptions from all middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Habilita a interface do Swagger em /swagger
    app.UseDeveloperExceptionPage();
}
else
{
    // Configura tratamento de erros para produção
    // app.UseExceptionHandler("/Error");
    // Habilita HSTS (Strict Transport Security)
    app.UseHsts();
}

app.UseHttpsRedirection(); // Redireciona HTTP para HTTPS

// Security Headers - Protege contra XSS, clickjacking, MIME sniffing
app.UseSecurityHeaders();

app.UseStaticFiles(); // Permite servir arquivos estáticos como CSS, JS, imagens

// CORS - Deve vir antes de Authentication/Authorization
app.UseCors();

// Enforce secure cookie behaviors globally
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    // Align Secure policy with antiforgery/cookies: prefer SameAsRequest so TLS termination works
    Secure = CookieSecurePolicy.SameAsRequest
});

app.UseAntiforgery(); // Adiciona proteção contra CSRF

// Adiciona middlewares de autenticação e autorização (se aplicável)
// A ordem é importante: UseAuthentication antes de UseAuthorization
app.UseAuthentication();
app.UseMiddleware<erp.Services.Tenancy.TenantResolutionMiddleware>();
app.UseAuthorization();

// API Key enforcement placeholder: only activates if Security:ApiKey is configured
app.UseMiddleware<ApiKeyMiddleware>();

// Mapeia os endpoints
app.MapControllers(); // Mapeia rotas para os Controllers de API

// Liveness probe (não depende do banco) -> use em Coolify se quiser expor mesmo sem DB
app.MapGet("/health", () => Results.Ok(new {
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).AllowAnonymous();

// Readiness probe (verifica banco) -> use em Coolify somente se o app só deve expor quando o DB estiver OK
app.MapGet("/ready", async (ApplicationDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new {
            status = "ready",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "Database connection failed"
        );
    }
}).AllowAnonymous();

app.MapRazorComponents<App>() // Mapeia os componentes Blazor
    .AddInteractiveServerRenderMode();
// Mapeie outros endpoints (Minimal APIs, etc.) aqui, se necessário

// Seed inicial de Identity (ambiente de dev)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Bootstrap flag: forces EnsureCreated on first deploys when migrations aren't available
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var bootstrapEnv = Environment.GetEnvironmentVariable("DB_BOOTSTRAP");
        var bootstrapCfg = config.GetValue<bool>("Database:Bootstrap");
        var dbBootstrap = bootstrapCfg || string.Equals(bootstrapEnv, "true", StringComparison.OrdinalIgnoreCase);

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Prefer Migrate when migrations exist; allow forcing EnsureCreated via DB_BOOTSTRAP
        var anyModelMigrations = db.Database.GetMigrations().Any();
        var bootstrapMode = Environment.GetEnvironmentVariable("DB_BOOTSTRAP")?.Trim().ToLowerInvariant();
        var bootstrapDropCreate = bootstrapMode == "dropcreate";
        var bootstrapEnabled = bootstrapDropCreate || dbBootstrap;

        if (bootstrapEnabled)
        {
            if (bootstrapDropCreate)
            {
                // SECURITY: Block dropcreate in production to prevent accidental data loss
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                if (env.IsProduction())
                {
                    throw new InvalidOperationException(
                        "DB_BOOTSTRAP=dropcreate is not allowed in production environment. " +
                        "This operation would delete all database data.");
                }

                logger.LogWarning("[DB] Bootstrap DROP+CREATE enabled -> EnsureDeleted() + EnsureCreated()");
                try { db.Database.EnsureDeleted(); } catch (Exception ex) { logger.LogWarning(ex, "[DB] EnsureDeleted warning"); }
                db.Database.EnsureCreated();
            }
            else
            {
                logger.LogInformation("[DB] Bootstrap mode enabled -> EnsureCreated()/CreateTables()");
                var created = db.Database.EnsureCreated();
                if (!created)
                {
                    try
                    {
                        var databaseCreator = (RelationalDatabaseCreator)db.Database.GetService<IRelationalDatabaseCreator>();
                        databaseCreator.CreateTables();
                        logger.LogInformation("[DB] CreateTables executed successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[DB] CreateTables fallback failed");
                    }
                }
            }
        }
        else if (anyModelMigrations)
        {
            logger.LogInformation("[DB] Applying migrations -> Migrate()");
            db.Database.Migrate();
        }
        else
        {
            logger.LogInformation("[DB] No migrations found -> EnsureCreated()");
            db.Database.EnsureCreated();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task<Tenant> EnsureTenant(string slug, string name)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tenant == null)
            {
                tenant = new Tenant 
                { 
                    Slug = slug, 
                    Name = name, 
                    Status = TenantStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();
                logger.LogInformation("[Seed] Created tenant: {Slug}", slug);
            }
            return tenant;
        }

        async Task EnsureRole(string name, string? description = null, string? icon = null)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole 
                { 
                    Name = name, 
                    NormalizedName = name.ToUpperInvariant(),
                    Description = description,
                    Icon = icon
                });
            }
        }

        await EnsureRole("Administrador", "Acesso total ao sistema", "AdminPanelSettings");
        await EnsureRole("Gerente", "Gerenciar equipes e relatórios", "SupervisorAccount");
        await EnsureRole("Vendedor", "Registro de vendas e atendimento", "PointOfSale");
        await EnsureRole("Estoque", "Usuário comum de estoque", "Inventory");
        await EnsureRole("RH", "Recursos Humanos", "Badge");
        await EnsureRole("Financeiro", "Departamento Financeiro", "AccountBalance");

        // Seed Module Permissions
        await SeedModulePermissionsAsync(db, roleManager, logger);

        // Ensure Admin Tenant exists
        var adminTenant = await EnsureTenant("admin", "Administração");

        var adminEmail = "admin@erp.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            // Get default admin password from configuration (not hardcoded)
            var securityOptions = scope.ServiceProvider.GetRequiredService<IOptions<SecurityOptions>>();
            var defaultPassword = securityOptions.Value.DefaultAdminPassword;

            if (string.IsNullOrWhiteSpace(defaultPassword))
            {
                throw new InvalidOperationException(
                    "Default admin password not configured. Please set Security:DefaultAdminPassword in appsettings.json or use environment variables.");
            }

            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                TenantId = adminTenant.Id
            };
            var created = await userManager.CreateAsync(admin, defaultPassword);
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrador");
                
                // Add membership
                db.TenantMemberships.Add(new TenantMembership 
                { 
                    TenantId = adminTenant.Id, 
                    UserId = admin.Id,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
        else if (admin.TenantId == null)
        {
            // Update existing admin if it has no tenant
            admin.TenantId = adminTenant.Id;
            await userManager.UpdateAsync(admin);
            
            // Ensure membership exists
            if (!await db.TenantMemberships.AnyAsync(m => m.TenantId == adminTenant.Id && m.UserId == admin.Id))
            {
                db.TenantMemberships.Add(new TenantMembership 
                { 
                    TenantId = adminTenant.Id, 
                    UserId = admin.Id,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        try
        {
            var demoSeedOptions = scope.ServiceProvider.GetRequiredService<IOptions<DemoSeedOptions>>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            // SECURITY: Block demo data seeding in production
            if (demoSeedOptions.Value.Enabled && !env.IsProduction())
            {
                var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
                await demoSeeder.SeedAsync();
            }
            else if (demoSeedOptions.Value.Enabled && env.IsProduction())
            {
                logger.LogWarning("[Seed] DemoSeed is enabled but skipped in production environment.");
            }
        }
        catch (Exception demoSeedEx)
        {
            logger.LogError(demoSeedEx, "Demo data seed error");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Identity seed error");
    }
}

/// <summary>
/// Seeds module permissions and assigns them to roles
/// </summary>
static async Task SeedModulePermissionsAsync(ApplicationDbContext db, RoleManager<ApplicationRole> roleManager, ILogger logger)
{
    try
    {
        // Define all modules
        var modules = new[]
        {
            new ModulePermission { ModuleKey = ModuleKeys.Dashboard, DisplayName = "Dashboard", Description = "Painel principal com visão geral", Icon = "Dashboard", DisplayOrder = 1 },
            new ModulePermission { ModuleKey = ModuleKeys.Sales, DisplayName = "Vendas", Description = "Gestão de vendas e clientes", Icon = "ShoppingCart", DisplayOrder = 2 },
            new ModulePermission { ModuleKey = ModuleKeys.ServiceOrder, DisplayName = "Ordens de Serviço", Description = "Gestão de ordens de serviço", Icon = "Build", DisplayOrder = 3 },
            new ModulePermission { ModuleKey = ModuleKeys.Inventory, DisplayName = "Estoque", Description = "Controle de produtos e movimentações", Icon = "Inventory", DisplayOrder = 4 },
            new ModulePermission { ModuleKey = ModuleKeys.Financial, DisplayName = "Financeiro", Description = "Contas a pagar/receber, fornecedores", Icon = "AccountBalance", DisplayOrder = 5 },
            new ModulePermission { ModuleKey = ModuleKeys.HR, DisplayName = "RH", Description = "Recursos Humanos e folha de pagamento", Icon = "Groups", DisplayOrder = 6 },
            new ModulePermission { ModuleKey = ModuleKeys.Assets, DisplayName = "Ativos", Description = "Gestão de patrimônio", Icon = "Devices", DisplayOrder = 7 },
            new ModulePermission { ModuleKey = ModuleKeys.Kanban, DisplayName = "Kanban", Description = "Quadros de tarefas", Icon = "ViewKanban", DisplayOrder = 8 },
            new ModulePermission { ModuleKey = ModuleKeys.Reports, DisplayName = "Relatórios", Description = "Relatórios gerenciais", Icon = "Assessment", DisplayOrder = 9 },
            new ModulePermission { ModuleKey = ModuleKeys.Admin, DisplayName = "Administração", Description = "Configurações do sistema", Icon = "AdminPanelSettings", DisplayOrder = 10 }
        };

        // Add modules if they don't exist
        foreach (var module in modules)
        {
            var existing = await db.ModulePermissions.FirstOrDefaultAsync(m => m.ModuleKey == module.ModuleKey);
            if (existing == null)
            {
                db.ModulePermissions.Add(module);
            }
        }
        await db.SaveChangesAsync();

        // Reload modules from DB to get IDs
        var dbModules = await db.ModulePermissions.ToDictionaryAsync(m => m.ModuleKey, m => m.Id);

        // Define available actions by module
        var moduleActions = new Dictionary<string, (string ActionKey, string DisplayName, string? Description, int DisplayOrder)[]>
        {
            [ModuleKeys.Dashboard] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar o dashboard", 1),
                (ModuleActionKeys.Dashboard.ViewWidgets, "Visualizar widgets", "Visualizar widgets do painel", 2),
                (ModuleActionKeys.Dashboard.ViewSensitiveWidgets, "Visualizar indicadores sensíveis", "Visualizar widgets com valores sensíveis", 3)
            ],
            [ModuleKeys.Sales] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar módulo de vendas", 1),
                (ModuleActionKeys.Common.Create, "Criar vendas", "Registrar nova venda", 2),
                (ModuleActionKeys.Common.Update, "Editar vendas", "Alterar dados de venda", 3),
                (ModuleActionKeys.Sales.Finalize, "Finalizar vendas", "Marcar vendas como finalizadas", 4),
                (ModuleActionKeys.Sales.Cancel, "Cancelar vendas", "Cancelar vendas pendentes", 5),
                (ModuleActionKeys.Sales.ViewHistory, "Ver histórico", "Visualizar listagem/histórico de vendas", 6),
                (ModuleActionKeys.Sales.ViewValues, "Ver valores", "Visualizar valores e totais de venda", 7),
                (ModuleActionKeys.Sales.ManageCustomers, "Gerenciar clientes", "Criar/editar clientes do módulo", 8),
                (ModuleActionKeys.Sales.ExportPdf, "Exportar PDF", "Exportar venda para PDF", 9)
            ],
            [ModuleKeys.ServiceOrder] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar ordens de serviço", 1),
                (ModuleActionKeys.Common.Create, "Criar ordens", "Criar novas ordens de serviço", 2),
                (ModuleActionKeys.Common.Update, "Editar ordens", "Editar ordens existentes", 3),
                (ModuleActionKeys.ServiceOrder.Finalize, "Finalizar ordens", "Finalizar ordens de serviço", 4),
                (ModuleActionKeys.ServiceOrder.Reopen, "Reabrir ordens", "Reabrir ordens finalizadas", 5),
                (ModuleActionKeys.ServiceOrder.Cancel, "Cancelar ordens", "Cancelar ordens em andamento", 6),
                (ModuleActionKeys.ServiceOrder.ViewCosts, "Ver custos", "Visualizar custos e margens", 7)
            ],
            [ModuleKeys.Inventory] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar estoque", 1),
                (ModuleActionKeys.Common.Create, "Cadastrar produtos", "Criar produtos e itens", 2),
                (ModuleActionKeys.Common.Update, "Editar produtos", "Editar produtos e itens", 3),
                (ModuleActionKeys.Common.Delete, "Excluir produtos", "Excluir produtos e itens", 4),
                (ModuleActionKeys.Inventory.AdjustStock, "Ajustar estoque", "Executar ajustes de estoque", 5),
                (ModuleActionKeys.Inventory.ViewCosts, "Ver custos", "Visualizar custo dos produtos", 6),
                (ModuleActionKeys.Inventory.ManageCategories, "Gerenciar categorias", "Gerenciar categorias do estoque", 7)
            ],
            [ModuleKeys.Financial] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar financeiro", 1),
                (ModuleActionKeys.Common.Create, "Criar lançamentos", "Criar contas e lançamentos", 2),
                (ModuleActionKeys.Common.Update, "Editar lançamentos", "Editar contas e lançamentos", 3),
                (ModuleActionKeys.Common.Delete, "Excluir lançamentos", "Excluir contas e lançamentos", 4),
                (ModuleActionKeys.Financial.Approve, "Aprovar lançamentos", "Aprovar operações financeiras", 5),
                (ModuleActionKeys.Financial.ViewBalances, "Ver saldos", "Visualizar saldos e fechamento", 6),
                (ModuleActionKeys.Financial.ManageSuppliers, "Gerenciar fornecedores", "Gerenciar cadastro de fornecedores", 7),
                (ModuleActionKeys.Financial.ManageCostCenters, "Gerenciar centros de custo", "Gerenciar centros de custo", 8)
            ],
            [ModuleKeys.HR] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar RH", 1),
                (ModuleActionKeys.Common.Create, "Criar registros", "Criar registros de RH", 2),
                (ModuleActionKeys.Common.Update, "Editar registros", "Editar registros de RH", 3),
                (ModuleActionKeys.Common.Delete, "Excluir registros", "Excluir registros de RH", 4),
                (ModuleActionKeys.HR.ManageAttendance, "Gerenciar ponto", "Gerenciar apontamentos e presença", 5),
                (ModuleActionKeys.HR.ManagePayroll, "Gerenciar folha", "Gerenciar folha de pagamento", 6),
                (ModuleActionKeys.HR.ViewSalaryData, "Ver dados salariais", "Visualizar valores salariais", 7)
            ],
            [ModuleKeys.Assets] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar ativos", 1),
                (ModuleActionKeys.Common.Create, "Cadastrar ativos", "Cadastrar novos ativos", 2),
                (ModuleActionKeys.Common.Update, "Editar ativos", "Editar ativos existentes", 3),
                (ModuleActionKeys.Common.Delete, "Excluir ativos", "Excluir ativos", 4),
                (ModuleActionKeys.Assets.Transfer, "Transferir ativos", "Transferir ativo entre locais", 5),
                (ModuleActionKeys.Assets.Depreciation, "Gerenciar depreciação", "Gerenciar depreciação de ativos", 6)
            ],
            [ModuleKeys.Kanban] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar quadros kanban", 1),
                (ModuleActionKeys.Common.Create, "Criar cartões", "Criar cards e tarefas", 2),
                (ModuleActionKeys.Common.Update, "Editar cartões", "Editar cards e tarefas", 3),
                (ModuleActionKeys.Common.Delete, "Excluir cartões", "Excluir cards e tarefas", 4),
                (ModuleActionKeys.Kanban.ManageBoards, "Gerenciar quadros", "Gerenciar quadros e colunas", 5),
                (ModuleActionKeys.Kanban.ManageMembers, "Gerenciar membros", "Gerenciar membros dos quadros", 6)
            ],
            [ModuleKeys.Reports] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar relatórios", 1),
                (ModuleActionKeys.Reports.Sales, "Relatórios de vendas", "Acessar relatórios de vendas", 2),
                (ModuleActionKeys.Reports.Financial, "Relatórios financeiros", "Acessar relatórios financeiros", 3),
                (ModuleActionKeys.Reports.Inventory, "Relatórios de estoque", "Acessar relatórios de estoque", 4),
                (ModuleActionKeys.Reports.HR, "Relatórios de RH", "Acessar relatórios de RH", 5),
                (ModuleActionKeys.Common.Export, "Exportar relatórios", "Exportar relatórios", 6)
            ],
            [ModuleKeys.Admin] =
            [
                (ModuleActionKeys.Common.ViewPage, "Visualizar página", "Acessar administração", 1),
                (ModuleActionKeys.Admin.ManageRoles, "Gerenciar permissões", "Gerenciar papéis e permissões", 2),
                (ModuleActionKeys.Admin.ManageTenants, "Gerenciar tenants", "Gerenciar tenants e configurações", 3),
                (ModuleActionKeys.Admin.ViewAudit, "Ver auditoria", "Visualizar logs de auditoria", 4),
                (ModuleActionKeys.Admin.ViewLgpd, "Ver acessos LGPD", "Visualizar relatório LGPD", 5)
            ]
        };

        // Seed module action definitions
        foreach (var (moduleKey, actions) in moduleActions)
        {
            if (!dbModules.TryGetValue(moduleKey, out var moduleId))
                continue;

            foreach (var action in actions)
            {
                var exists = await db.ModuleActionPermissions.AnyAsync(a =>
                    a.ModulePermissionId == moduleId &&
                    a.ActionKey == action.ActionKey);

                if (!exists)
                {
                    db.ModuleActionPermissions.Add(new ModuleActionPermission
                    {
                        ModulePermissionId = moduleId,
                        ActionKey = action.ActionKey,
                        DisplayName = action.DisplayName,
                        Description = action.Description,
                        DisplayOrder = action.DisplayOrder,
                        IsActive = true
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        var moduleActionIdsByModule = await db.ModuleActionPermissions
            .Where(a => a.IsActive)
            .GroupBy(a => a.ModulePermissionId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(a => a.Id).ToList());

        // Define role-module mappings
        var roleModules = new Dictionary<string, string[]>
        {
            // Administrador gets all modules (handled in code, but seed anyway for completeness)
            ["Administrador"] = new[] { ModuleKeys.Dashboard, ModuleKeys.Sales, ModuleKeys.ServiceOrder, ModuleKeys.Inventory, ModuleKeys.Financial, ModuleKeys.HR, ModuleKeys.Assets, ModuleKeys.Kanban, ModuleKeys.Reports, ModuleKeys.Admin },

            // Gerente gets most modules except Admin
            ["Gerente"] = new[] { ModuleKeys.Dashboard, ModuleKeys.Sales, ModuleKeys.ServiceOrder, ModuleKeys.Inventory, ModuleKeys.Financial, ModuleKeys.HR, ModuleKeys.Assets, ModuleKeys.Kanban, ModuleKeys.Reports },

            // Vendedor gets Sales, ServiceOrder, Dashboard, Kanban
            ["Vendedor"] = new[] { ModuleKeys.Dashboard, ModuleKeys.Sales, ModuleKeys.ServiceOrder, ModuleKeys.Kanban },

            // Estoque gets Inventory, Dashboard, Reports
            ["Estoque"] = new[] { ModuleKeys.Dashboard, ModuleKeys.Inventory, ModuleKeys.Reports },

            // RH gets HR, Dashboard, Reports
            ["RH"] = new[] { ModuleKeys.Dashboard, ModuleKeys.HR, ModuleKeys.Reports },

            // Financeiro gets Financial, Dashboard, Reports
            ["Financeiro"] = new[] { ModuleKeys.Dashboard, ModuleKeys.Financial, ModuleKeys.Reports }
        };

        // Assign modules to roles
        foreach (var (roleName, moduleKeys) in roleModules)
        {
            var role = await db.Set<ApplicationRole>().FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) continue;

            foreach (var moduleKey in moduleKeys)
            {
                if (!dbModules.TryGetValue(moduleKey, out var moduleId)) continue;

                var exists = await db.RoleModulePermissions
                    .AnyAsync(rmp => rmp.RoleId == role.Id && rmp.ModulePermissionId == moduleId);

                if (!exists)
                {
                    db.RoleModulePermissions.Add(new RoleModulePermission
                    {
                        RoleId = role.Id,
                        ModulePermissionId = moduleId,
                        GrantedAt = DateTime.UtcNow
                    });
                }

                if (!moduleActionIdsByModule.TryGetValue(moduleId, out var actionIds))
                    continue;

                foreach (var actionId in actionIds)
                {
                    var actionExists = await db.RoleModuleActionPermissions
                        .AnyAsync(rmap => rmap.RoleId == role.Id && rmap.ModuleActionPermissionId == actionId);

                    if (!actionExists)
                    {
                        db.RoleModuleActionPermissions.Add(new RoleModuleActionPermission
                        {
                            RoleId = role.Id,
                            ModuleActionPermissionId = actionId,
                            GrantedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seed] Module permissions seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Seed] Module permissions seed error");
    }
}

// --- Executa a aplicação ---
app.Run(); // Inicia o servidor e começa a ouvir requisições

// Tornar o Program visível para testes de integração com WebApplicationFactory
public partial class Program { }
