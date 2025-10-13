using Microsoft.EntityFrameworkCore; 
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
using erp.Security;
using erp.Models.Identity;
using erp.Services.Dashboard;
using erp.Services.Dashboard.Providers.Sales;
using erp.Services.Dashboard.Providers.Finance;
// using ApexCharts; // TODO: Instalar pacote ApexCharts se necessário

var builder = WebApplication.CreateBuilder(args);

// --- Configure Serviços ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

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
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest // allow HTTP in dev to avoid antiforgery/cookie issues
        : CookieSecurePolicy.Always; // only over HTTPS in prod
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
    });
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => {
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
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
// builder.Services.AddApexCharts(); // TODO: Instalar pacote ApexCharts se necessário
builder.Services.AddBlazoredLocalStorage();

// Antiforgery hardening (used by UseAntiforgery)
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    o.Cookie.Name = "erp.csrf";
    // HeaderName can be customized if you post forms via JS: o.HeaderName = "X-CSRF-TOKEN";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
                                                options.UseNpgsql(connectionString ?? "Host=localhost;Database=erp;Username=postgres;Password=123"));

// Registra DAOs e Serviços
builder.Services.AddScoped<IUserDao, UserDao>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<IApiService, ApiService>();

// ------- REGISTRO DO MAPPERLY -------
builder.Services.AddScoped<UserMapper, UserMapper>();

// --- Constrói a aplicação ---
var app = builder.Build();

// Enable forwarded headers so HTTPS scheme is honored behind reverse proxies
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

// --- Configura o pipeline de requisições HTTP ---
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
    Secure = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always
});

app.UseAntiforgery(); // Adiciona proteção contra CSRF

// Adiciona middlewares de autenticação e autorização (se aplicável)
// A ordem é importante: UseAuthentication antes de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// API Key enforcement placeholder: only activates if Security:ApiKey is configured
app.UseMiddleware<ApiKeyMiddleware>();

// Mapeia os endpoints
app.MapControllers(); // Mapeia rotas para os Controllers de API
app.MapRazorComponents<App>() // Mapeia os componentes Blazor
    .AddInteractiveServerRenderMode();
// Mapeie outros endpoints (Minimal APIs, etc.) aqui, se necessário

// Seed inicial de Identity (ambiente de dev)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Identity seed error: {ex.Message}");
    }
}

// --- Executa a aplicação ---
app.Run(); // Inicia o servidor e começa a ouvir requisições