using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace erp.Extensions;

/// <summary>
/// Extensions for DbContext configuration
/// </summary>
public static class DbContextExtensions
{
    private static readonly string[] WeakPasswordPatterns =
    {
        "123", "password", "Password123", "admin", "root", "postgres", "test", "demo"
    };

    /// <summary>
    /// Validates connection string checking for weak passwords
    /// </summary>
    public static void ValidateConnectionString(
        this IServiceCollection services,
        string? connectionString,
        IWebHostEnvironment environment,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return; // Will be validated later
        }

        // Check for common/default password patterns
        var passwordMatch = System.Text.RegularExpressions.Regex.Match(
            connectionString,
            $@"Password=(?:{string.Join('|', WeakPasswordPatterns)})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (passwordMatch.Success)
        {
            var message = "Weak database password detected. The connection string contains a " +
                         "common/default password. Please use a strong, unique password.";

            if (environment.IsProduction())
            {
                throw new InvalidOperationException(message);
            }

            logger?.LogWarning(message);
        }

        // Check for placeholder passwords
        var placeholderMatch = System.Text.RegularExpressions.Regex.Match(
            connectionString,
            @"Password=(?:YOUR_SECURE|CHANGE_ON_FIRST_LOGIN|'')",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (placeholderMatch.Success)
        {
            throw new InvalidOperationException(
                "Database password not configured. Please set a secure password in ConnectionStrings__DefaultConnection environment variable. " +
                "See appsettings.example.json for configuration details.");
        }
    }

    /// <summary>
    /// Configures DbContext for PostgreSQL with migrations
    /// </summary>
    public static DbContextOptionsBuilder UseNpgsqlWithMigrations(
        this DbContextOptionsBuilder optionsBuilder,
        string? connectionString,
        System.Reflection.Assembly migrationsAssembly)
    {
        return optionsBuilder.UseNpgsql(
            connectionString ?? "Host=localhost;Database=erp;Username=postgres",
            npgsql => npgsql.MigrationsAssembly(migrationsAssembly.FullName)
        );
    }
}
