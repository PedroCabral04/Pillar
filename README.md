# Pillar ERP

Sistema ERP modular construído com Blazor (Server), .NET 9, MudBlazor e ASP.NET Core Identity. Inclui dashboard, gestão de usuários (admin), Kanban pessoal, preferências do usuário e hardening de cookies/CSRF. Suporta modo opcional de API Key para rotas `/api`.

## 🚀 Deploy Rápido

### Coolify (Recomendado)
Veja o guia completo de deploy no Coolify: **[COOLIFY_DEPLOYMENT.md](COOLIFY_DEPLOYMENT.md)**

### Docker
```bash
docker-compose up -d
```

Acesse em `http://localhost:8080`

- Projeto: [erp.csproj](erp.csproj)
- Pipeline e segurança: [Program.cs](Program.cs)
- App host: [`erp.Components.App`](Components/App.razor)
- Menu e navegação: [Components/Layout/NavMenu.razor](Components/Layout/NavMenu.razor)
- Páginas:
  - Home (protegida): [Components/Pages/Home.razor](Components/Pages/Home.razor)
  - Dashboard: [Components/Pages/Dashboard/Index.razor](Components/Pages/Dashboard/Index.razor)
  - Admin • Usuários: [Components/Pages/Admin/Users.razor](Components/Pages/Admin/Users.razor)
  - Kanban: [Components/Pages/Kanban/MyBoard.razor](Components/Pages/Kanban/MyBoard.razor)
  - Ajustes (Configurações): [Components/Pages/Settings.razor](Components/Pages/Settings.razor)
  - Login: [Components/Pages/Auth/Login.razor](Components/Pages/Auth/Login.razor)
- Imports de UI e serviços: [Components/_Imports.razor](Components/_Imports.razor)
- Notas de autenticação e segurança: [DTOs/Auth/README.md](DTOs/Auth/README.md)

## Tecnologias

- .NET 9 (ASP.NET Core, Blazor Server)
- MudBlazor UI
- ASP.NET Core Identity (roles/claims)
- Entity Framework Core + PostgreSQL (Npgsql)
- Swagger (desenvolvimento)
- ApexCharts
- Blazored.LocalStorage

Veja [erp.csproj](erp.csproj) para a lista completa de pacotes.

## Funcionalidades

- Autenticação baseada em cookie com Identity (dev seed habilitado)
- Políticas de cookies e antiforgery ativas
  - [`app.UseCookiePolicy`](Program.cs)
  - [`app.UseAntiforgery`](Program.cs)
- Dashboard com seleção de período: [Components/Pages/Dashboard/Index.razor](Components/Pages/Dashboard/Index.razor)
- Administração de usuários com tabela server-side, busca, validação e atribuição de funções: [Components/Pages/Admin/Users.razor](Components/Pages/Admin/Users.razor)
- Kanban pessoal: [Components/Pages/Kanban/MyBoard.razor](Components/Pages/Kanban/MyBoard.razor)
- Ajustes de preferências (tema, widgets, tabelas/colunas, notificações): [Components/Pages/Settings.razor](Components/Pages/Settings.razor)
- Navegação principal: [Components/Layout/NavMenu.razor](Components/Layout/NavMenu.razor)
- Swagger UI em desenvolvimento

## Requisitos

- .NET SDK 9.x
- PostgreSQL 14+ (recomendado 16+)
- Certificado HTTPS de desenvolvimento (dotnet dev-certs)

## Configuração

1) Configurar a connection string do PostgreSQL

Use appsettings.json ou variáveis de ambiente (recomendado em dev/CI/CD):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=pillar;Username=postgres;Password=postgres"
  }
}
```

No Linux (bash), você pode exportar:

```bash
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=pillar;Username=postgres;Password=postgres'
```

2) (Opcional) Habilitar API Key para rotas /api

Defina a chave:

```bash
export Security__ApiKey='minha-chave-secreta-super-segura'
```

O middleware é registrado em [`app.UseMiddleware<ApiKeyMiddleware>()`](Program.cs). Consulte diretrizes em [DTOs/Auth/README.md](DTOs/Auth/README.md).

3) Certificado HTTPS de desenvolvimento

```bash
dotnet dev-certs https --trust
```

## Banco de dados e Migrations

O projeto chama `db.Database.Migrate()` em startup ([Program.cs](Program.cs)). Se o diretório `Migrations` não existir (está no .gitignore), crie uma migration local:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial
dotnet ef database update
```

Na primeira execução em desenvolvimento, um usuário admin pode ser criado (veja notas em [DTOs/Auth/README.md](DTOs/Auth/README.md)).

Credenciais de desenvolvimento (padrão, se seed habilitado):
- Admin: `admin@erp.local` / `Admin@123!`

Use apenas em dev.

## Executando em desenvolvimento

```bash
dotnet restore
dotnet run
```

Aplicação:
- UI: https://localhost:5001
- Swagger (dev): https://localhost:5001/swagger

Rotas principais:
- Home (autenticada): `/`
- Dashboard: `/dashboard`
- Admin • Usuários: `/admin/users`
- Kanban: `/kanban`
- Ajustes: `/ajustes`
- Login: `/login`

## Segurança

Aplicado:
- Cookies seguros e SameSite Lax globais: [`app.UseCookiePolicy`](Program.cs)
- Antiforgery global: [`app.UseAntiforgery`](Program.cs)
- HTTPS/HSTS (produção)
- Login/Logout via cookie (ver [Components/Pages/Auth/Login.razor](Components/Pages/Auth/Login.razor))

Recomendações adicionais: ver [DTOs/Auth/README.md](DTOs/Auth/README.md) (autorize controladores `[Authorize]`, rate limiting, 2FA, CSP, persistência de DataProtection, auditoria, etc).

## Estrutura de pastas (parcial)

- UI e layout:
  - [Components/App.razor](Components/App.razor)
  - [Components/Layout/NavMenu.razor](Components/Layout/NavMenu.razor)
  - [Components/_Imports.razor](Components/_Imports.razor)
- Páginas:
  - [Components/Pages/Home.razor](Components/Pages/Home.razor)
  - [Components/Pages/Dashboard/Index.razor](Components/Pages/Dashboard/Index.razor)
  - [Components/Pages/Admin/Users.razor](Components/Pages/Admin/Users.razor)
  - [Components/Pages/Kanban/MyBoard.razor](Components/Pages/Kanban/MyBoard.razor)
  - [Components/Pages/Settings.razor](Components/Pages/Settings.razor)
  - [Components/Pages/Auth/Login.razor](Components/Pages/Auth/Login.razor)
- Domínio e modelos:
  - [Models/User.cs](Models/User.cs)
- Configuração e serviços:
  - [Program.cs](Program.cs)
  - Preferências/tema/serviços (ver injeções nas páginas)
- Documentação de auth:
  - [DTOs/Auth/README.md](DTOs/Auth/README.md)
