using System.Security.Cryptography;
using System.Text.RegularExpressions;
using erp.Data;
using erp.Models.Identity;
using erp.Models.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace erp.Services.Tenancy;

public class TenantProvisioningService : ITenantProvisioningService
{
    private static readonly Regex DbNameRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);

    private static readonly string[] DefaultRoleNames = new[] { "Administrador", "Gerente", "Vendedor" };
    private const string DefaultAdminRoleName = "Administrador";

    private readonly TenantDatabaseOptions _options;
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public TenantProvisioningService(
        IOptions<TenantDatabaseOptions> options,
        ITenantDbContextFactory dbContextFactory,
        ILogger<TenantProvisioningService> logger,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task ProvisionAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant is null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        var connectionString = PrepareConnectionString(tenant);
        await EnsureDatabaseExistsAsync(connectionString, cancellationToken);
        await RunMigrationsAsync(connectionString, cancellationToken);
        await SeedTenantDefaultsAsync(tenant, connectionString, cancellationToken);

        if (_options.AutoActivateTenants)
        {
            tenant.Status = TenantStatus.Active;
            tenant.ActivatedAt ??= DateTime.UtcNow;
        }

        _logger.LogInformation("Tenant {Tenant} provisioned with database {Database}", tenant.Slug, tenant.DatabaseName);
    }

    private string PrepareConnectionString(Tenant tenant)
    {
        tenant.DatabaseName ??= GenerateDatabaseName(tenant.Slug);

        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            return tenant.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(_options.TemplateConnectionString))
        {
            throw new InvalidOperationException("Template connection string is not configured for multi-tenancy.");
        }

        var cs = _options.TemplateConnectionString
            .Replace("{SLUG}", tenant.Slug, StringComparison.OrdinalIgnoreCase)
            .Replace("{DB}", tenant.DatabaseName, StringComparison.OrdinalIgnoreCase);

        tenant.ConnectionString = cs;
        return cs;
    }

    private string GenerateDatabaseName(string slug)
    {
        var sanitizedSlug = DbNameRegex.Replace(slug, "_");
        sanitizedSlug = sanitizedSlug.Trim('_');
        if (string.IsNullOrWhiteSpace(sanitizedSlug))
        {
            sanitizedSlug = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        return ($"{_options.DatabasePrefix}{sanitizedSlug}").ToLowerInvariant();
    }

    private async Task EnsureDatabaseExistsAsync(string tenantConnectionString, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(tenantConnectionString ?? throw new ArgumentNullException(nameof(tenantConnectionString)));
        var databaseName = builder.Database ?? throw new InvalidOperationException("Tenant connection string must include a database name.");

        // Switch to admin database to create the tenant database if missing
        var adminBuilder = new NpgsqlConnectionStringBuilder(tenantConnectionString)
        {
            Database = _options.AdminDatabase
        };

        await using var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await adminConnection.OpenAsync(cancellationToken);

        var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", adminConnection);
        existsCommand.Parameters.AddWithValue("@name", databaseName);
        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
        if (!exists)
        {
            var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", adminConnection);
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Database {Database} created", databaseName);
        }
    }

    private async Task RunMigrationsAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var tenantContext = _dbContextFactory.CreateDbContext(connectionString);
        await tenantContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task SeedTenantDefaultsAsync(Tenant tenant, string connectionString, CancellationToken cancellationToken)
    {
        await using var tenantContext = _dbContextFactory.CreateDbContext(connectionString);

        var roleSet = tenantContext.Set<ApplicationRole>();
        var existingRoleNames = await roleSet
            .AsNoTracking()
            .Select(r => r.NormalizedName!)
            .ToListAsync(cancellationToken);

        var rolesAdded = false;
        foreach (var roleName in DefaultRoleNames)
        {
            var normalizedName = roleName.ToUpperInvariant();
            if (existingRoleNames.Contains(normalizedName))
            {
                continue;
            }

            await roleSet.AddAsync(new ApplicationRole
            {
                Name = roleName,
                NormalizedName = normalizedName,
                TenantId = tenant.Id
            }, cancellationToken);

            rolesAdded = true;
        }

        if (rolesAdded)
        {
            await tenantContext.SaveChangesAsync(cancellationToken);
        }

        var adminRoleId = await roleSet
            .Where(r => r.NormalizedName == DefaultAdminRoleName.ToUpperInvariant())
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId == 0)
        {
            _logger.LogWarning("Administrador role not found for tenant {Tenant}", tenant.Slug);
            return;
        }

        var identityUsers = tenantContext.Set<ApplicationUser>();
        var adminEmail = (tenant.PrimaryContactEmail ?? $"admin@{tenant.Slug}.local").ToLowerInvariant();
        var normalizedAdminEmail = adminEmail.ToUpperInvariant();

        var adminUser = await identityUsers
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedAdminEmail, cancellationToken);

        var temporaryPassword = string.Empty;
        var userCreated = false;

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                NormalizedUserName = normalizedAdminEmail,
                Email = adminEmail,
                NormalizedEmail = normalizedAdminEmail,
                EmailConfirmed = true,
                FullName = tenant.PrimaryContactName ?? $"{tenant.Name} Admin",
                PhoneNumber = tenant.PrimaryContactPhone,
                PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(tenant.PrimaryContactPhone),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("D")
            };

            temporaryPassword = GenerateTemporaryPassword();
            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, temporaryPassword);

            await identityUsers.AddAsync(adminUser, cancellationToken);
            await tenantContext.SaveChangesAsync(cancellationToken);
            userCreated = true;
        }

        var identityUserRoles = tenantContext.Set<IdentityUserRole<int>>();
        var hasAdminRole = await identityUserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleId, cancellationToken);

        if (!hasAdminRole)
        {
            identityUserRoles.Add(new IdentityUserRole<int>
            {
                UserId = adminUser.Id,
                RoleId = adminRoleId
            });

            await tenantContext.SaveChangesAsync(cancellationToken);
        }

        if (userCreated)
        {
            _logger.LogInformation(
                "Tenant {Tenant} admin user {Email} seeded with temporary password {Password}",
                tenant.Slug,
                adminEmail,
                temporaryPassword);
        }
    }

    private static string GenerateTemporaryPassword(int length = 12)
    {
        const string uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*?_-";
        const string all = uppercase + lowercase + digits + special;

        if (length < 8)
        {
            length = 8;
        }

        var password = new char[length];
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < length; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        Shuffle(password);
        return new string(password);
    }

    private static void Shuffle(Span<char> buffer)
    {
        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }
}
