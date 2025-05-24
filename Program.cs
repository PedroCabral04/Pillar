using Microsoft.EntityFrameworkCore; 
using MudBlazor.Services;
using erp.Components;
using erp.DAOs;
using erp.Data;
using erp.Mappings;
using erp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configure Serviços ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adiciona serviços de terceiros.
builder.Services.AddMudServices();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); 
builder.Services.AddDbContext<AppDbContext>(options =>
                                                options.UseNpgsql(connectionString ?? "Host=localhost;Database=erp;Username=postgres;Password=123"));

// Registra DAOs e Serviços
builder.Services.AddScoped<IUserDao, UserDao>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ThemeService>();

// ------- REGISTRO DO MAPPERLY -------
builder.Services.AddScoped<UserMapper, UserMapper>(); 

// --- Constrói a aplicação ---
var app = builder.Build();

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

app.UseAntiforgery(); // Adiciona proteção contra CSRF

// Adiciona middlewares de autenticação e autorização (se aplicável)
// A ordem é importante: UseAuthentication antes de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Mapeia os endpoints
app.MapControllers(); // Mapeia rotas para os Controllers de API
app.MapRazorComponents<App>() // Mapeia os componentes Blazor
    .AddInteractiveServerRenderMode();
// Mapeie outros endpoints (Minimal APIs, etc.) aqui, se necessário

// --- Executa a aplicação ---
app.Run(); // Inicia o servidor e começa a ouvir requisições