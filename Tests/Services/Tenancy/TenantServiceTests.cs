using System;
using System.Threading.Tasks;
using erp.Data;
using erp.DTOs.Tenancy;
using erp.Mappings;
using erp.Models.Tenancy;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace erp.Tests.Services.Tenancy;

public class TenantServiceTests
{
    [Fact]
    public async Task CreateAsync_WithDuplicateDatabaseName_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(out var context, out var provisioningServiceMock);
        context.Tenants.Add(new Tenant
        {
            Name = "Tenant 1",
            Slug = "tenant-1",
            Status = TenantStatus.Active,
            DatabaseName = "pillar_demo"
        });
        await context.SaveChangesAsync();

        var dto = new CreateTenantDto
        {
            Name = "Tenant 2",
            Slug = "tenant-2",
            DatabaseName = "PILLAR_DEMO",
            ProvisionDatabase = false
        };

        // Act
        var act = () => service.CreateAsync(dto, userId: 42);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DatabaseName 'PILLAR_DEMO' já está em uso por outro tenant.");

        provisioningServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenDatabaseNameConflicts_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(out var context, out _);
        var tenantA = new Tenant
        {
            Name = "Tenant A",
            Slug = "tenant-a",
            Status = TenantStatus.Active,
            DatabaseName = "pillar_a"
        };
        var tenantB = new Tenant
        {
            Name = "Tenant B",
            Slug = "tenant-b",
            Status = TenantStatus.Active,
            DatabaseName = "pillar_b"
        };

        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var updateDto = new UpdateTenantDto
        {
            Name = tenantB.Name,
            Status = tenantB.Status,
            DatabaseName = "PILLAR_A"
        };

        // Act
        var act = () => service.UpdateAsync(tenantB.Id, updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DatabaseName 'PILLAR_A' já está em uso por outro tenant.");
    }

    [Fact]
    public async Task GetConnectionInfoAsync_ReturnsDatabaseDetails()
    {
        // Arrange
        var service = CreateService(out var context, out _);
        var tenant = new Tenant
        {
            Name = "Tenant Database",
            Slug = "tenant-database",
            Status = TenantStatus.Active,
            DatabaseName = "pillar_db",
            ConnectionString = "Host=localhost;Database=pillar_db",
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetConnectionInfoAsync(tenant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DatabaseName.Should().Be("pillar_db");
        result.ConnectionString.Should().Be("Host=localhost;Database=pillar_db");
        result.Slug.Should().Be("tenant-database");
    }

    private static TenantService CreateService(out ApplicationDbContext context, out Mock<ITenantProvisioningService> provisioningServiceMock)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new TenantMapper();
        var logger = Mock.Of<ILogger<TenantService>>();
        provisioningServiceMock = new Mock<ITenantProvisioningService>(MockBehavior.Strict);

        return new TenantService(context, mapper, logger, provisioningServiceMock.Object);
    }
}
