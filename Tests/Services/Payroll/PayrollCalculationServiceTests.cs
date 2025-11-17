using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using erp.Data;
using erp.Models.Identity;
using erp.Models.Payroll;
using erp.Models.TimeTracking;
using erp.Services.Payroll;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace erp.Tests.Services.Payroll;

public class PayrollCalculationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PayrollCalculationService _service;
    private readonly Mock<ILogger<PayrollCalculationService>> _loggerMock = new();

    public PayrollCalculationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new PayrollCalculationService(_context, _loggerMock.Object);
        SeedData();
    }

    [Fact]
    public async Task CalculateAsync_ShouldGeneratePayrollResults()
    {
        // Act
        var period = await _service.CalculateAsync(1, requestedById: 99);

        // Assert
        period.Status.Should().Be(PayrollPeriodStatus.Calculated);
        period.Results.Should().HaveCount(1);

        var result = period.Results[0];
        result.EmployeeNameSnapshot.Should().Be("Maria da Silva");
        result.TotalEarnings.Should().Be(5000m);
        result.TotalDeductions.Should().BeGreaterThan(0);
        result.NetAmount.Should().BeLessThan(result.TotalEarnings);
        result.Components.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_ShouldRespectOvertimeAndAbsences()
    {
        // Arrange
        var entry = await _context.PayrollEntries.FirstAsync();
        entry.HorasExtras = 20m;
        entry.Faltas = 10m;
        await _context.SaveChangesAsync();

        // Act
        var period = await _service.CalculateAsync(1, requestedById: 99);
        var result = period.Results[0];

        // Assert
        result.TotalEarnings.Should().BeGreaterThan(5000m); // overtime added
        result.GrossAmount.Should().BeLessThan(result.TotalEarnings); // absence discounts subtract
        result.Components.Should().Contain(c => c.Code == "HE");
        result.Components.Should().Contain(c => c.Code == "FALTAS");
    }

    [Fact]
    public async Task CalculateAsync_WithApprovedPeriod_ShouldThrow()
    {
        // Arrange
        var period = await _context.PayrollPeriods.FirstAsync();
        period.Status = PayrollPeriodStatus.Approved;
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = () => _service.CalculateAsync(1, 1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private void SeedData()
    {
        var user = new ApplicationUser
        {
            Id = 10,
            UserName = "maria@erp.local",
            Email = "maria@erp.local",
            FullName = "Maria da Silva",
            Salary = 5000m,
            IsActive = true
        };

        _context.Set<ApplicationUser>().Add(user);

        var period = new PayrollPeriod
        {
            Id = 1,
            ReferenceMonth = 10,
            ReferenceYear = 2025,
            Status = PayrollPeriodStatus.Draft,
            Entries = new List<PayrollEntry>
            {
                new()
                {
                    EmployeeId = 10,
                    Employee = user,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _context.PayrollPeriods.Add(period);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
