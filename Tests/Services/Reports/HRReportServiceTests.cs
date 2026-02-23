using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Identity;
using erp.Models.TimeTracking;
using erp.Services.Reports;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace erp.Tests.Services.Reports;

public class HRReportServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var tenantAccessor = new Mock<ITenantContextAccessor>();
        tenantAccessor.SetupGet(x => x.Current).Returns(new TenantContext());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenantContextAccessor: tenantAccessor.Object);
    }

    [Fact]
    public async Task GenerateAttendanceReport_ComputesEmployeeAttendance()
    {
        await using var context = CreateContext();

        context.Departments.Add(new Department { Id = 1, TenantId = 1, Name = "TI" });
        context.Positions.Add(new Position { Id = 1, TenantId = 1, Title = "Dev" });

        context.Users.Add(new ApplicationUser
        {
            Id = 10,
            UserName = "maria",
            FullName = "Maria",
            Email = "maria@erp.local",
            DepartmentId = 1,
            PositionId = 1
        });

        context.PayrollPeriods.Add(new PayrollPeriod { Id = 1, ReferenceMonth = 1, ReferenceYear = 2026 });
        context.PayrollEntries.Add(new PayrollEntry
        {
            Id = 1,
            PayrollPeriodId = 1,
            EmployeeId = 10,
            Faltas = 2,
            Atrasos = 1,
            HorasExtras = 5
        });

        await context.SaveChangesAsync();

        var service = new HRReportService(context, NullLogger<HRReportService>.Instance);
        var result = await service.GenerateAttendanceReportAsync(new HRReportFilterDto());

        result.Summary.TotalEmployees.Should().Be(1);
        result.Summary.TotalAbsences.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Items[0].EmployeeName.Should().Be("Maria");
    }

    [Fact]
    public async Task GenerateHeadcountReport_GroupsByDepartmentAndPosition()
    {
        await using var context = CreateContext();

        context.Departments.AddRange(
            new Department { Id = 1, TenantId = 1, Name = "TI" },
            new Department { Id = 2, TenantId = 1, Name = "Financeiro" });
        context.Positions.Add(new Position { Id = 10, TenantId = 1, Title = "Analista" });

        context.Users.AddRange(
            new ApplicationUser { Id = 1, UserName = "u1", Email = "u1@erp.local", DepartmentId = 1, PositionId = 10 },
            new ApplicationUser { Id = 2, UserName = "u2", Email = "u2@erp.local", DepartmentId = 1, PositionId = 10 },
            new ApplicationUser { Id = 3, UserName = "u3", Email = "u3@erp.local", DepartmentId = 2, PositionId = 10 });

        await context.SaveChangesAsync();

        var service = new HRReportService(context, NullLogger<HRReportService>.Instance);
        var result = await service.GenerateHeadcountReportAsync(new HRReportFilterDto());

        result.Summary.TotalEmployees.Should().Be(3);
        result.ByDepartment.Should().HaveCount(2);
        result.ByDepartment.First().EmployeeCount.Should().Be(2);
        result.ByPosition.Should().ContainSingle();
    }
}
