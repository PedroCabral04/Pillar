using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;

namespace erp.Tests.Services.Tenancy;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public class TenantProvisioningCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    public const string CollectionName = "Tenant Provisioning";
}

public sealed class PostgresTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresTestContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("postgres")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public PostgreSqlContainer Container => _container;

    public string BuildTemplateConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = "{DB}"
        };

        return builder.ToString();
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
