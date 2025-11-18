using System.Threading;
using erp.Models.Identity;
using erp.Models.Tenancy;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace erp.Tests.Services.Tenancy;

[Collection(TenantProvisioningCollection.CollectionName)]
public class TenantProvisioningServiceTests
{
    private static readonly string[] ExpectedDefaultRoles = { "Administrador", "Gerente", "Vendedor" };

    private readonly TenantProvisioningService _sut;
    private readonly TenantDbContextFactory _tenantDbContextFactory;
    private readonly TenantDatabaseOptions _options;
    private int _tenantIdSequence;

    public TenantProvisioningServiceTests(PostgresTestContainerFixture fixture)
    {
        _tenantDbContextFactory = new TenantDbContextFactory(null);
        _options = new TenantDatabaseOptions
        {
            TemplateConnectionString = fixture.BuildTemplateConnectionString(),
            AdminDatabase = "postgres",
            DatabasePrefix = "pillar_ut_",
            AutoActivateTenants = true
        };

        _sut = new TenantProvisioningService(
            Options.Create(_options),
            _tenantDbContextFactory,
            NullLogger<TenantProvisioningService>.Instance,
            new PasswordHasher<ApplicationUser>());
    }

    [Fact]
    public async Task ProvisionAsync_AppliesMigrationsAndSeedsIsolatedTenants()
    {
        var tenantA = CreateTenant("tenant_a");
        var tenantB = CreateTenant("tenant_b");

        await _sut.ProvisionAsync(tenantA);
        await _sut.ProvisionAsync(tenantB);

        await using var tenantAContext = _tenantDbContextFactory.CreateDbContext(tenantA.ConnectionString!);
        await using var tenantBContext = _tenantDbContextFactory.CreateDbContext(tenantB.ConnectionString!);

        var tenantAMigrations = await tenantAContext.Database.GetAppliedMigrationsAsync();
        var tenantBMigrations = await tenantBContext.Database.GetAppliedMigrationsAsync();

        tenantAMigrations.Should().NotBeEmpty("migrations must run for the first tenant");
        tenantBMigrations.Should().NotBeEmpty("migrations must run for the second tenant");

        var rolesForTenantA = await tenantAContext.Set<ApplicationRole>().AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        var rolesForTenantB = await tenantBContext.Set<ApplicationRole>().AsNoTracking().OrderBy(r => r.Name).ToListAsync();

        rolesForTenantA.Select(r => r.Name).Should().BeEquivalentTo(ExpectedDefaultRoles);
        rolesForTenantB.Select(r => r.Name).Should().BeEquivalentTo(ExpectedDefaultRoles);
        rolesForTenantA.Should().OnlyContain(r => r.TenantId == tenantA.Id);
        rolesForTenantB.Should().OnlyContain(r => r.TenantId == tenantB.Id);

        var tenantAUsers = await tenantAContext.Set<ApplicationUser>().AsNoTracking().ToListAsync();
        var tenantBUsers = await tenantBContext.Set<ApplicationUser>().AsNoTracking().ToListAsync();

        var tenantAAdminEmail = tenantA.PrimaryContactEmail!.ToLowerInvariant();
        var tenantBAdminEmail = tenantB.PrimaryContactEmail!.ToLowerInvariant();

        tenantAUsers.Should().ContainSingle(u => u.Email == tenantAAdminEmail);
        tenantBUsers.Should().ContainSingle(u => u.Email == tenantBAdminEmail);

        await tenantAContext.Set<ApplicationRole>().AddAsync(new ApplicationRole
        {
            Name = "Tenant A Only",
            NormalizedName = "TENANT_A_ONLY",
            TenantId = tenantA.Id
        });
        await tenantAContext.SaveChangesAsync();

        var tenantBHasTenantARole = await tenantBContext.Set<ApplicationRole>()
            .AnyAsync(role => role.NormalizedName == "TENANT_A_ONLY");
        tenantBHasTenantARole.Should().BeFalse("custom data saved in tenant A should never leak into tenant B");
    }

    private Tenant CreateTenant(string slugPrefix)
    {
        var slug = $"{slugPrefix}_{Guid.NewGuid():N}".ToLowerInvariant();
        var newId = Interlocked.Increment(ref _tenantIdSequence);

        return new Tenant
        {
            Id = newId,
            Name = slug.Replace('_', ' '),
            Slug = slug,
            PrimaryContactName = $"Owner {slugPrefix}",
            PrimaryContactEmail = $"{slugPrefix}@example.com",
            PrimaryContactPhone = "+55 11 99999-0000"
        };
    }
}
