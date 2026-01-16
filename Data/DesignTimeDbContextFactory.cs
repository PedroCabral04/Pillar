using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace erp.Data;

/// <summary>
/// Factory for creating a DbContext at design time (for migrations).
/// Reads connection string from appsettings.json or user secrets.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Try to get connection string from multiple sources
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DbContextSettings:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string not found. Please set it in one of these locations:\n" +
                "1. appsettings.json: \"ConnectionStrings\": { \"DefaultConnection\": \"...\" }\n" +
                "2. appsettings.Development.json: \"ConnectionStrings\": { \"DefaultConnection\": \"...\" }\n" +
                "3. User Secrets: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"\n" +
                "4. Environment variable: set ConnectionStrings__DefaultConnection=\"...\"\n\n" +
                "Example:\n" +
                "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Database=erp;Username=postgres;Password=your_password\"");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
