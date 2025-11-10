using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Identity;
using erp.Models.TimeTracking;
using erp.Services.Reports;

namespace erp.Tests.Services.Reports;

/// <summary>
/// Testes unitários para o serviço de relatórios de RH
/// </summary>
public class HRReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HRReportService>> _mockLogger;
    private readonly HRReportService _service;

    public HRReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<HRReportService>>();
        _service = new HRReportService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var department1 = new Department
        {
            Id = 1,
            Name = "TI",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var department2 = new Department
        {
            Id = 2,
            Name = "Vendas",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var position1 = new Position
        {
            Id = 1,
            Title = "Desenvolvedor",
            DepartmentId = 1,
            MinSalary = 3000,
            MaxSalary = 10000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var position2 = new Position
        {
            Id = 2,
            Title = "Vendedor",
            DepartmentId = 2,
            MinSalary = 2000,
            MaxSalary = 5000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new ApplicationUser
        {
            Id = 1,
            UserName = "joao@empresa.com",
            Email = "joao@empresa.com",
            Name = "João Silva",
            DepartmentId = 1,
            PositionId = 1,
            HireDate = DateTime.UtcNow.AddYears(-2),
            ContractType = "CLT",
            IsActive = true
        };

        var user2 = new ApplicationUser
        {
            Id = 2,
            UserName = "maria@empresa.com",
            Email = "maria@empresa.com",
            Name = "Maria Santos",
            DepartmentId = 1,
            PositionId = 1,
            HireDate = DateTime.UtcNow.AddYears(-1),
            ContractType = "CLT",
            IsActive = true
        };

        var user3 = new ApplicationUser
        {
            Id = 3,
            UserName = "pedro@empresa.com",
            Email = "pedro@empresa.com",
            Name = "Pedro Costa",
            DepartmentId = 2,
            PositionId = 2,
            HireDate = DateTime.UtcNow.AddMonths(-6),
            ContractType = "PJ",
            IsActive = true
        };

        var user4 = new ApplicationUser
        {
            Id = 4,
            UserName = "ana@empresa.com",
            Email = "ana@empresa.com",
            Name = "Ana Oliveira",
            DepartmentId = 2,
            PositionId = 2,
            HireDate = DateTime.UtcNow.AddYears(-3),
            TerminationDate = DateTime.UtcNow.AddMonths(-1),
            ContractType = "CLT",
            IsActive = false
        };

        // Time tracking entries (attendance)
        var today = DateTime.UtcNow.Date;
        
        // User 1 - Perfect attendance
        for (int i = 0; i < 20; i++)
        {
            _context.TimeEntries.Add(new TimeEntry
            {
                Id = i + 1,
                UserId = 1,
                Date = today.AddDays(-i),
                ClockIn = today.AddDays(-i).AddHours(8),
                ClockOut = today.AddDays(-i).AddHours(17),
                Status = "Presente",
                CreatedAt = today.AddDays(-i)
            });
        }

        // User 2 - Some absences and late arrivals
        for (int i = 0; i < 20; i++)
        {
            var status = i % 5 == 0 ? "Ausente" : i % 3 == 0 ? "Atrasado" : "Presente";
            var clockIn = status == "Ausente" ? (DateTime?)null : today.AddDays(-i).AddHours(status == "Atrasado" ? 9 : 8);
            var clockOut = status == "Ausente" ? (DateTime?)null : today.AddDays(-i).AddHours(17);

            _context.TimeEntries.Add(new TimeEntry
            {
                Id = 21 + i,
                UserId = 2,
                Date = today.AddDays(-i),
                ClockIn = clockIn,
                ClockOut = clockOut,
                Status = status,
                CreatedAt = today.AddDays(-i)
            });
        }

        // User 3 - Recent hire, good attendance
        for (int i = 0; i < 15; i++)
        {
            _context.TimeEntries.Add(new TimeEntry
            {
                Id = 41 + i,
                UserId = 3,
                Date = today.AddDays(-i),
                ClockIn = today.AddDays(-i).AddHours(8),
                ClockOut = today.AddDays(-i).AddHours(17),
                Status = "Presente",
                CreatedAt = today.AddDays(-i)
            });
        }

        _context.Departments.AddRange(department1, department2);
        _context.Positions.AddRange(position1, position2);
        _context.Users.AddRange(user1, user2, user3, user4);
        _context.SaveChanges();
    }

    #region Attendance Tests

    [Fact]
    public async Task GenerateAttendanceReport_ReturnsAllActiveEmployees()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(3); // Only active employees
        result.Employees.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateAttendanceReport_CalculatesAttendanceRates()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        
        var user1Data = result.Employees.FirstOrDefault(e => e.EmployeeId == 1);
        user1Data.Should().NotBeNull();
        user1Data!.AttendanceRate.Should().Be(100); // Perfect attendance
        
        var user2Data = result.Employees.FirstOrDefault(e => e.EmployeeId == 2);
        user2Data.Should().NotBeNull();
        user2Data!.AttendanceRate.Should().BeLessThan(100); // Has absences
    }

    [Fact]
    public async Task GenerateAttendanceReport_CountsPresentDaysCorrectly()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        
        var user1Data = result.Employees.FirstOrDefault(e => e.EmployeeId == 1);
        user1Data.Should().NotBeNull();
        user1Data!.PresentDays.Should().BeGreaterThan(0);
        user1Data.WorkDays.Should().Be(user1Data.PresentDays + user1Data.AbsentDays);
    }

    [Fact]
    public async Task GenerateAttendanceReport_CountsLateDays()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        
        var user2Data = result.Employees.FirstOrDefault(e => e.EmployeeId == 2);
        user2Data.Should().NotBeNull();
        user2Data!.LateDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAttendanceReport_WithDepartmentFilter_ReturnsFilteredEmployees()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            DepartmentId = 1 // TI
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(2); // Only TI employees
        result.Employees.Should().OnlyContain(e => e.DepartmentName == "TI");
    }

    [Fact]
    public async Task GenerateAttendanceReport_CalculatesAverageAttendanceRate()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.AverageAttendanceRate.Should().BeGreaterThan(0);
        result.AverageAttendanceRate.Should().BeLessOrEqualTo(100);
    }

    #endregion

    #region Turnover Tests

    [Fact]
    public async Task GenerateTurnoverReport_CalculatesEmployeeCounts()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.EmployeesAtStart.Should().BeGreaterThan(0);
        result.EmployeesAtEnd.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateTurnoverReport_CountsNewHires()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddMonths(-7),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.NewHires.Should().BeGreaterThan(0); // User 3 was hired 6 months ago
    }

    [Fact]
    public async Task GenerateTurnoverReport_CountsTerminations()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Terminations.Should().BeGreaterThan(0); // User 4 was terminated
    }

    [Fact]
    public async Task GenerateTurnoverReport_CalculatesTurnoverRate()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TurnoverRate.Should().BeGreaterThanOrEqualTo(0);
        result.TurnoverRate.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public async Task GenerateTurnoverReport_WithDepartmentFilter_ReturnsFilteredData()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            DepartmentId = 1
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.DepartmentName.Should().Be("TI");
    }

    [Fact]
    public async Task GenerateTurnoverReport_GroupsByPeriod()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Periods.Should().NotBeEmpty();
    }

    #endregion

    #region Headcount Tests

    [Fact]
    public async Task GenerateHeadcountReport_CountsTotalEmployees()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(3); // Only active employees
        result.ActiveEmployees.Should().Be(3);
    }

    [Fact]
    public async Task GenerateHeadcountReport_GroupsByDepartment()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.ByDepartment.Should().HaveCount(2); // TI and Vendas
        
        var tiDept = result.ByDepartment.FirstOrDefault(d => d.DepartmentName == "TI");
        tiDept.Should().NotBeNull();
        tiDept!.EmployeeCount.Should().Be(2);
        
        var salesDept = result.ByDepartment.FirstOrDefault(d => d.DepartmentName == "Vendas");
        salesDept.Should().NotBeNull();
        salesDept!.EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateHeadcountReport_GroupsByPosition()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.ByPosition.Should().HaveCount(2); // Desenvolvedor and Vendedor
        
        var devPosition = result.ByPosition.FirstOrDefault(p => p.PositionTitle == "Desenvolvedor");
        devPosition.Should().NotBeNull();
        devPosition!.EmployeeCount.Should().Be(2);
    }

    [Fact]
    public async Task GenerateHeadcountReport_GroupsByContractType()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.ByContractType.Should().HaveCount(2); // CLT and PJ
        
        var clt = result.ByContractType.FirstOrDefault(c => c.ContractType == "CLT");
        clt.Should().NotBeNull();
        clt!.EmployeeCount.Should().Be(2);
        
        var pj = result.ByContractType.FirstOrDefault(c => c.ContractType == "PJ");
        pj.Should().NotBeNull();
        pj!.EmployeeCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateHeadcountReport_CalculatesAverageTenure()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.AverageTenure.Should().BeGreaterThan(0);
        // User 1: 2 years, User 2: 1 year, User 3: 6 months
        // Average should be around 1.17 years
        result.AverageTenure.Should().BeApproximately(1.17, 0.5);
    }

    [Fact]
    public async Task GenerateHeadcountReport_WithDepartmentFilter_ReturnsFilteredData()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            DepartmentId = 1 // TI
        };

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(2);
        result.ByDepartment.Should().HaveCount(1);
        result.ByDepartment.First().DepartmentName.Should().Be("TI");
    }

    [Fact]
    public async Task GenerateHeadcountReport_IncludesGenderDistribution()
    {
        // Arrange
        var user1 = _context.Users.Find(1);
        var user2 = _context.Users.Find(2);
        var user3 = _context.Users.Find(3);
        
        user1!.Gender = "Masculino";
        user2!.Gender = "Feminino";
        user3!.Gender = "Masculino";
        
        await _context.SaveChangesAsync();

        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.ByGender.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateAttendanceReport_WithNoTimeEntries_ReturnsZeroAttendance()
    {
        // Arrange
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        await _context.SaveChangesAsync();

        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(3);
        result.Employees.Should().OnlyContain(e => e.PresentDays == 0);
    }

    [Fact]
    public async Task GenerateTurnoverReport_WithNoChanges_ReturnsZeroTurnover()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-5) // Period with no hires/terminations
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.NewHires.Should().Be(0);
        result.Terminations.Should().Be(0);
    }

    [Fact]
    public async Task GenerateHeadcountReport_WithInactiveEmployees_ExcludesInactive()
    {
        // Arrange
        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(3); // User 4 is inactive
        result.ActiveEmployees.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAttendanceReport_WithWeekendDays_HandlesCorrectly()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Employees.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateTurnoverReport_WithFutureDate_HandlesGracefully()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.GenerateTurnoverReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.NewHires.Should().Be(0);
        result.Terminations.Should().Be(0);
    }

    [Fact]
    public async Task GenerateHeadcountReport_WithNoEmployees_ReturnsEmptyReport()
    {
        // Arrange
        var allUsers = _context.Users.ToList();
        foreach (var user in allUsers)
        {
            user.IsActive = false;
        }
        await _context.SaveChangesAsync();

        var filter = new HRReportFilterDto();

        // Act
        var result = await _service.GenerateHeadcountReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmployees.Should().Be(0);
        result.ByDepartment.Should().BeEmpty();
        result.ByPosition.Should().BeEmpty();
        result.ByContractType.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAttendanceReport_WithMultipleStatuses_CountsCorrectly()
    {
        // Arrange
        var filter = new HRReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-20),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateAttendanceReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var user2Data = result.Employees.FirstOrDefault(e => e.EmployeeId == 2);
        user2Data.Should().NotBeNull();
        
        // Verify sum of all status types equals work days
        var totalCounted = user2Data!.PresentDays + user2Data.AbsentDays + user2Data.LateDays;
        totalCounted.Should().BeLessOrEqualTo(user2Data.WorkDays);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
