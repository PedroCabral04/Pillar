using Microsoft.EntityFrameworkCore; 
using DotNetEnv;
using System;
using MudBlazor.Services;
using erp.Components;
using erp.DAOs;
using erp.Data;
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
using erp.Services.Dashboard;
using erp.Services.Dashboard.Providers.Sales;
using erp.Services.Dashboard.Providers.Finance;
using erp.Services.Dashboard.Providers.Inventory;
using erp.Services.Sales;
using erp.Services.Seeding;
using System.Reflection;
using Microsoft.Extensions.Options;
using ApexCharts;

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

// --- Aspire Service Defaults ---
builder.AddServiceDefaults();

// --- Configure Serviços ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

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
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    // Harden cookie
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // prevents CSRF on cross-site navigations, still works for same-site
        // Use SameAsRequest to support TLS termination at the proxy (Coolify/nginx)
        // and avoid antiforgery exceptions when the incoming connection to Kestrel is HTTP
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
builder.Services.Configure<DemoSeedOptions>(builder.Configuration.GetSection("DemoSeed"));
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var resolver = serviceProvider.GetRequiredService<erp.Services.Tenancy.ITenantConnectionResolver>();
    var effectiveConnection = resolver.GetCurrentConnectionString();
    options.UseNpgsql(
        effectiveConnection ?? connectionString ?? "Host=localhost;Database=erp;Username=postgres;Password=123",
        npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
    );
});

builder.Services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
{
    var resolver = serviceProvider.GetRequiredService<erp.Services.Tenancy.ITenantConnectionResolver>();
    var effectiveConnection = resolver.GetCurrentConnectionString();
    options.UseNpgsql(
        effectiveConnection ?? connectionString ?? "Host=localhost;Database=erp;Username=postgres;Password=123",
        npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
    );
}, ServiceLifetime.Scoped);

// Registra DAOs e Serviços
builder.Services.AddScoped<IUserDao, UserDao>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<PreferenceService>();
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

// Financial services
builder.Services.AddScoped<erp.Services.Financial.IAccountingService, erp.Services.Financial.AccountingService>();
builder.Services.AddScoped<erp.Services.Financial.ISupplierService, erp.Services.Financial.SupplierService>();
builder.Services.AddScoped<erp.Services.Financial.IFinancialCategoryService, erp.Services.Financial.FinancialCategoryService>();
builder.Services.AddScoped<erp.Services.Financial.ICostCenterService, erp.Services.Financial.CostCenterService>();
builder.Services.AddScoped<erp.Services.Financial.IAccountReceivableService, erp.Services.Financial.AccountReceivableService>();
builder.Services.AddScoped<erp.Services.Financial.IAccountPayableService, erp.Services.Financial.AccountPayableService>();
builder.Services.AddScoped<erp.Services.Financial.IFinancialDashboardService, erp.Services.Financial.FinancialDashboardService>();

// Financial DAOs
builder.Services.AddScoped<erp.DAOs.Financial.ISupplierDao, erp.DAOs.Financial.SupplierDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IFinancialCategoryDao, erp.DAOs.Financial.FinancialCategoryDao>();
builder.Services.AddScoped<erp.DAOs.Financial.ICostCenterDao, erp.DAOs.Financial.CostCenterDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IAccountReceivableDao, erp.DAOs.Financial.AccountReceivableDao>();
builder.Services.AddScoped<erp.DAOs.Financial.IAccountPayableDao, erp.DAOs.Financial.AccountPayableDao>();

// Financial validation services (BrazilianDocumentValidator is static, no DI needed)
builder.Services.AddHttpClient<erp.Services.Financial.Validation.IViaCepService, erp.Services.Financial.Validation.ViaCepService>();
builder.Services.AddHttpClient<erp.Services.Financial.Validation.IReceitaWsService, erp.Services.Financial.Validation.ReceitaWsService>();

// Asset Management services
builder.Services.AddScoped<erp.DAOs.Assets.IAssetDao, erp.DAOs.Assets.AssetDao>();
builder.Services.AddScoped<erp.Services.Assets.IAssetService, erp.Services.Assets.AssetService>();
builder.Services.AddScoped<erp.Services.Assets.IQRCodeService, erp.Services.Assets.QRCodeService>();
builder.Services.AddScoped<erp.Services.Assets.IFileStorageService, erp.Services.Assets.LocalFileStorageService>();
builder.Services.AddScoped<erp.Mappings.AssetMapper>();

// Time tracking services
builder.Services.AddScoped<erp.Services.TimeTracking.ITimeTrackingService, erp.Services.TimeTracking.TimeTrackingService>();

// Payroll services
builder.Services.AddScoped<erp.Services.Payroll.IPayrollCalculationService, erp.Services.Payroll.PayrollCalculationService>();
builder.Services.AddScoped<erp.Services.Payroll.IPayrollSlipService, erp.Services.Payroll.PayrollSlipService>();
builder.Services.AddScoped<erp.Services.Payroll.IPayrollService, erp.Services.Payroll.PayrollService>();

// Audit services
builder.Services.AddScoped<erp.Services.Audit.IAuditService, erp.Services.Audit.AuditService>();

// Chatbot services
builder.Services.AddScoped<erp.Services.Chatbot.IChatbotService, erp.Services.Chatbot.ChatbotService>();

// Browser Service (Mobile/Responsive)
builder.Services.AddScoped<erp.Services.Browser.IBrowserService, erp.Services.Browser.BrowserService>();

// Tenancy services
builder.Services.AddScoped<erp.Services.Tenancy.ITenantService, erp.Services.Tenancy.TenantService>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantContextAccessor, erp.Services.Tenancy.TenantContextAccessor>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantResolver, erp.Services.Tenancy.DefaultTenantResolver>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantDbContextFactory, erp.Services.Tenancy.TenantDbContextFactory>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantProvisioningService, erp.Services.Tenancy.TenantProvisioningService>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantConnectionResolver, erp.Services.Tenancy.TenantConnectionResolver>();
builder.Services.AddScoped<erp.Services.Tenancy.ITenantBrandingProvider, erp.Services.Tenancy.TenantBrandingProvider>();
builder.Services.Configure<erp.Services.Tenancy.TenantDatabaseOptions>(builder.Configuration.GetSection("MultiTenancy:Database"));

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
builder.Services.AddScoped<UserMapper, UserMapper>();
builder.Services.AddScoped<ProductMapper, ProductMapper>();
builder.Services.AddScoped<StockMovementMapper, StockMovementMapper>();
builder.Services.AddScoped<StockCountMapper, StockCountMapper>();
builder.Services.AddScoped<SalesMapper, SalesMapper>();
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

app.UseStaticFiles(); // Permite servir arquivos estáticos como CSS, JS, imagens

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
    try
    {
        // Bootstrap flag: forces EnsureCreated on first deploys when migrations aren't available
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var bootstrapEnv = Environment.GetEnvironmentVariable("DB_BOOTSTRAP");
        var bootstrapCfg = config.GetValue<bool>("Database:Bootstrap");
        var dbBootstrap = bootstrapCfg || string.Equals(bootstrapEnv, "true", StringComparison.OrdinalIgnoreCase);

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // --- AUTO-FIX: Baseline migrations if missing (Self-Healing for Coolify) ---
        try 
        {
            // Check if AspNetUsers exists (implies DB was created with EnsureCreated)
            var userTableExists = await db.Database.SqlQueryRaw<int>(
                "SELECT count(*)::int FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AspNetUsers'")
                .FirstOrDefaultAsync() > 0;

            // Check if history table exists
            var historyTableExists = await db.Database.SqlQueryRaw<int>(
                "SELECT count(*)::int FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'")
                .FirstOrDefaultAsync() > 0;

            if (userTableExists && !historyTableExists)
            {
                Console.WriteLine("[DB] CRITICAL: Existing database detected without migration history. Injecting baseline...");
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                        ""MigrationId"" character varying(150) NOT NULL,
                        ""ProductVersion"" character varying(32) NOT NULL,
                        CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                    );
                    
                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES
                    ('20251013022123_first', '9.0.0'),
                    ('20251013055802_AddInventoryModule', '9.0.0'),
                    ('20251013115625_AddSalesModule', '9.0.0'),
                    ('20251013120656_AddSaleOrderForeignKeyToStockMovement', '9.0.0'),
                    ('20251013121723_StandardizeTableNaming', '9.0.0'),
                    ('20251107224217_AddAuditSystem', '9.0.0'),
                    ('20251108001102_FixAuditSystemIdCapture', '9.0.0'),
                    ('20251108025347_OptimizeAuditIndexes', '9.0.0'),
                    ('20251108025813_OptimizeAuditIndexesComposite', '9.0.0'),
                    ('20251108031849_AddAuditReadTracking', '9.0.0'),
                    ('20251109234656_AddTimeTrackingModule', '9.0.0'),
                    ('20251110025704_AddPayrollTimeTracking', '9.0.0'),
                    ('20251110052846_AddFinancialModule', '9.0.0'),
                    ('20251113232410_assets', '9.0.0'),
                    ('20251124204325_slq', '9.0.0')
                    ON CONFLICT DO NOTHING;
                ");
                Console.WriteLine("[DB] Baseline injected successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Baseline check failed (ignoring): {ex.Message}");
        }
        // ------------------------------------------------

        // Prefer Migrate when migrations exist; allow forcing EnsureCreated via DB_BOOTSTRAP
        var anyModelMigrations = db.Database.GetMigrations().Any();
        var bootstrapMode = Environment.GetEnvironmentVariable("DB_BOOTSTRAP")?.Trim().ToLowerInvariant();
        var bootstrapDropCreate = bootstrapMode == "dropcreate";
        var bootstrapEnabled = bootstrapDropCreate || dbBootstrap;

        if (bootstrapEnabled)
        {
            if (bootstrapDropCreate)
            {
                Console.WriteLine("[DB] Bootstrap DROP+CREATE enabled -> EnsureDeleted() + EnsureCreated()");
                try { db.Database.EnsureDeleted(); } catch (Exception ex) { Console.WriteLine($"[DB] EnsureDeleted warning: {ex.Message}"); }
                db.Database.EnsureCreated();
            }
            else
            {
                Console.WriteLine("[DB] Bootstrap mode enabled -> EnsureCreated()/CreateTables()");
                var created = db.Database.EnsureCreated();
                if (!created)
                {
                    try
                    {
                        var databaseCreator = (RelationalDatabaseCreator)db.Database.GetService<IRelationalDatabaseCreator>();
                        databaseCreator.CreateTables();
                        Console.WriteLine("[DB] CreateTables executed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DB] CreateTables fallback failed: {ex.Message}");
                    }
                }
            }
        }
        else if (anyModelMigrations)
        {
            Console.WriteLine("[DB] Applying migrations -> Migrate()");
            db.Database.Migrate();
        }
        else
        {
            Console.WriteLine("[DB] No migrations found -> EnsureCreated()");
            db.Database.EnsureCreated();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        async Task EnsureRole(string name)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = name, NormalizedName = name.ToUpperInvariant() });
            }
        }

        await EnsureRole("Administrador");
        await EnsureRole("Gerente");
        await EnsureRole("Vendedor");

        var adminEmail = "admin@erp.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };
            var created = await userManager.CreateAsync(admin, "Admin@123!");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrador");
            }
        }

        try
        {
            var demoSeedOptions = scope.ServiceProvider.GetRequiredService<IOptions<DemoSeedOptions>>();
            if (demoSeedOptions.Value.Enabled)
            {
                var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
                await demoSeeder.SeedAsync();
            }
        }
        catch (Exception demoSeedEx)
        {
            Console.WriteLine($"Demo data seed error: {demoSeedEx.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Identity seed error: {ex.Message}");
    }
}

// --- Executa a aplicação ---
app.Run(); // Inicia o servidor e começa a ouvir requisições

// Tornar o Program visível para testes de integração com WebApplicationFactory
public partial class Program { }