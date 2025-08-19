# Pillar – Auth, Cookies, and API Key Mode

## Enable API Key mode (for /api routes)
The middleware `Security/ApiKeyMiddleware.cs` checks header `X-Api-Key` on requests to paths starting with `/api`.
It is disabled until you configure a key.

Options to turn it on:

- appsettings.json (or appsettings.Production.json)

```
{
  "Security": {
    "ApiKey": "your-strong-random-key"
  }
}
```

- Environment variable (Linux/zsh)

```
export Security__ApiKey="your-strong-random-key"
# then run
 dotnet run
```

Clients must send:

```
X-Api-Key: your-strong-random-key
```

Scope: Only `/api/*` endpoints are checked. If the key is missing or invalid, the API returns 401.

## Current Identity implementation
- ASP.NET Core Identity (AddIdentityCore) with int keys.
  - Models: `Models/Identity/ApplicationUser`, `Models/Identity/ApplicationRole`.
  - DbContext: `Data/ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser, ApplicationRole, int>`.
- Cookie auth configured with the Identity application scheme.
  - Login path: `/login`, Logout: `/api/auth/logout`, cookie name: `erp.auth`.
- Login/Logout API: `Controllers/AuthController` using `SignInManager`/`UserManager`.
- Blazor UI:
  - Standalone login screen (no app shell) at `Components/Pages/Auth/Login.razor`.
  - Router sends unauthenticated users to login (`Components/Routes.razor`).
  - Admin/users/settings pages require `[Authorize]`.
- Seed (Development): roles (Administrador, Gerente, Vendedor) and admin user (`admin@erp.local` / `Admin@123!`).

## Cookie and CSRF hardening (already applied)
- Auth cookie: HttpOnly, SameSite=Lax, Secure=Always, Path=/, IsEssential=true, sliding expiration.
- Antiforgery cookie: HttpOnly, SameSite=Lax, Secure=Always, name `erp.csrf`.
- Global `CookiePolicy`: MinimumSameSite=Lax, Secure=Always.

Note: Login/Logout actions ignore antiforgery by design. You can tighten logout later.

## Recommended next steps (security)
- Protect APIs: add `[Authorize]` to API controllers and use browser `fetch` (with `credentials: 'include'`) from the UI, or call services directly (no HTTP) from components.
- Logout CSRF: require antiforgery for `/api/auth/logout` and send the header token from the UI.
- Rate limiting: add `AddRateLimiter` and limit `/api/auth/login` to mitigate credential stuffing.
- 2FA and email flows: enable email confirmation, password reset, and optional TOTP.
- Persistent DataProtection keys for scaling (file/Redis/Azure) so cookies remain valid across instances.
- Security headers: HSTS (prod), CSP, X-Content-Type-Options, Referrer-Policy.
- Auditing: log login failures/success and admin actions.

## Recommended next steps (performance/UX)
- Cache role lookups/claims or pre-load claims to reduce repeated DB calls.
- Add loading indicators to login/logout flows to avoid “flash” effects.
- Index Identity tables on common queries (NormalizedEmail/NormalizedUserName).
- Consider `ResponseCompression` for static assets; keep HTTPS/HTTP2 enabled.

## Quick reference
- API key header: `X-Api-Key`
- Turn on API key: set `Security:ApiKey` via config or env var.
- Admin login (dev): `admin@erp.local` / `Admin@123!`
- Files to check:
  - Program setup: `Program.cs`
  - Middleware: `Security/ApiKeyMiddleware.cs`
  - Auth API: `Controllers/AuthController.cs`
  - Router/layout: `Components/Routes.razor`, `Components/Layout/AuthLayout.razor`
  - Login page: `Components/Pages/Auth/Login.razor`
