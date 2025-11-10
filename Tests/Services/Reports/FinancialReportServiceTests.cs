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
using erp.Models.Financial;
using erp.Services.Reports;

namespace erp.Tests.Services.Reports;

/// <summary>
/// Testes unitários para o serviço de relatórios financeiros
/// </summary>
public class FinancialReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<FinancialReportService>> _mockLogger;
    private readonly FinancialReportService _service;

    public FinancialReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<FinancialReportService>>();
        _service = new FinancialReportService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var category1 = new FinancialCategory
        {
            Id = 1,
            Name = "Vendas",
            Type = "Receita",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var category2 = new FinancialCategory
        {
            Id = 2,
            Name = "Fornecedores",
            Type = "Despesa",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var category3 = new FinancialCategory
        {
            Id = 3,
            Name = "Salários",
            Type = "Despesa",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Accounts Receivable
        var receivable1 = new AccountReceivable
        {
            Id = 1,
            Description = "Venda Cliente 1",
            Amount = 1000,
            DueDate = DateTime.UtcNow.AddDays(-5),
            Status = "Pago",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var receivable2 = new AccountReceivable
        {
            Id = 2,
            Description = "Venda Cliente 2",
            Amount = 1500,
            DueDate = DateTime.UtcNow.AddDays(5),
            Status = "Pendente",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        // Accounts Payable
        var payable1 = new AccountPayable
        {
            Id = 1,
            Description = "Fornecedor A",
            Amount = 500,
            DueDate = DateTime.UtcNow.AddDays(-3),
            Status = "Pago",
            CategoryId = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var payable2 = new AccountPayable
        {
            Id = 2,
            Description = "Salários Mensais",
            Amount = 3000,
            DueDate = DateTime.UtcNow.AddDays(10),
            Status = "Pendente",
            CategoryId = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var payable3 = new AccountPayable
        {
            Id = 3,
            Description = "Fornecedor B",
            Amount = 800,
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = "Atrasado",
            CategoryId = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };

        _context.FinancialCategories.AddRange(category1, category2, category3);
        _context.AccountsReceivable.AddRange(receivable1, receivable2);
        _context.AccountsPayable.AddRange(payable1, payable2, payable3);
        _context.SaveChanges();
    }

    #region Cash Flow Tests

    [Fact]
    public async Task GenerateCashFlowReport_WithNoFilters_ReturnsAllTransactions()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5); // 2 receivables + 3 payables
        result.TotalRevenue.Should().Be(2500); // 1000 + 1500
        result.TotalExpenses.Should().Be(4300); // 500 + 3000 + 800
        result.NetCashFlow.Should().Be(-1800); // 2500 - 4300
    }

    [Fact]
    public async Task GenerateCashFlowReport_WithDateRange_ReturnsFilteredTransactions()
    {
        // Arrange
        var filter = new FinancialReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(i => 
            i.Date >= filter.StartDate && 
            i.Date <= filter.EndDate);
    }

    [Fact]
    public async Task GenerateCashFlowReport_OrdersByDateAscending()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var dates = result.Items.Select(i => i.Date).ToList();
        dates.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GenerateCashFlowReport_IncludesCategories()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(i => !string.IsNullOrEmpty(i.Category));
        result.Items.Should().Contain(i => i.Category == "Vendas");
        result.Items.Should().Contain(i => i.Category == "Fornecedores");
        result.Items.Should().Contain(i => i.Category == "Salários");
    }

    [Fact]
    public async Task GenerateCashFlowReport_DistinguishesRevenueAndExpenses()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var revenues = result.Items.Where(i => i.Type == "Receita").ToList();
        var expenses = result.Items.Where(i => i.Type == "Despesa").ToList();
        
        revenues.Should().HaveCount(2);
        expenses.Should().HaveCount(3);
    }

    #endregion

    #region Profit & Loss Tests

    [Fact]
    public async Task GenerateProfitLossReport_CalculatesCorrectly()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(2500);
        result.TotalExpenses.Should().Be(4300);
        result.NetIncome.Should().Be(-1800);
        result.NetProfitMargin.Should().BeApproximately(-72, 1); // -1800/2500 * 100
    }

    [Fact]
    public async Task GenerateProfitLossReport_GroupsExpensesByCategory()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.ExpensesByCategory.Should().HaveCount(2); // Fornecedores and Salários
        
        var suppliersCategory = result.ExpensesByCategory.FirstOrDefault(e => e.Category == "Fornecedores");
        suppliersCategory.Should().NotBeNull();
        suppliersCategory!.Amount.Should().Be(1300); // 500 + 800
        
        var salariesCategory = result.ExpensesByCategory.FirstOrDefault(e => e.Category == "Salários");
        salariesCategory.Should().NotBeNull();
        salariesCategory!.Amount.Should().Be(3000);
    }

    [Fact]
    public async Task GenerateProfitLossReport_WithDateFilter_ReturnsFilteredData()
    {
        // Arrange
        var filter = new FinancialReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().BeGreaterThanOrEqualTo(0);
        result.TotalExpenses.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GenerateProfitLossReport_WithNoExpenses_ReturnsZeroExpenses()
    {
        // Arrange
        _context.AccountsPayable.RemoveRange(_context.AccountsPayable);
        await _context.SaveChangesAsync();

        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalExpenses.Should().Be(0);
        result.ExpensesByCategory.Should().BeEmpty();
        result.NetIncome.Should().Be(result.TotalRevenue);
    }

    #endregion

    #region Balance Sheet Tests

    [Fact]
    public async Task GenerateBalanceSheetReport_CalculatesAssetsAndLiabilities()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().BeGreaterThanOrEqualTo(0);
        result.TotalLiabilities.Should().BeGreaterThanOrEqualTo(0);
        result.Equity.Should().Be(result.TotalAssets - result.TotalLiabilities);
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_CurrentAssets_IncludesReceivables()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.CurrentAssets.Should().BeGreaterThan(0);
        // Current assets should include pending receivables
        result.CurrentAssets.Should().BeGreaterThanOrEqualTo(1500); // receivable2
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_CurrentLiabilities_IncludesPayables()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.CurrentLiabilities.Should().BeGreaterThan(0);
        // Current liabilities should include pending and overdue payables
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_EquityBalance()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        // Assets = Liabilities + Equity (fundamental accounting equation)
        result.TotalAssets.Should().Be(result.TotalLiabilities + result.Equity);
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_WithDateFilter_UsesCorrectCutoffDate()
    {
        // Arrange
        var filter = new FinancialReportFilterDto
        {
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalAssets.Should().BeGreaterThanOrEqualTo(0);
        result.TotalLiabilities.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateCashFlowReport_WithNoData_ReturnsEmptyReport()
    {
        // Arrange
        _context.AccountsReceivable.RemoveRange(_context.AccountsReceivable);
        _context.AccountsPayable.RemoveRange(_context.AccountsPayable);
        await _context.SaveChangesAsync();

        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalRevenue.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.NetCashFlow.Should().Be(0);
    }

    [Fact]
    public async Task GenerateProfitLossReport_WithOnlyRevenue_ShowsProfit()
    {
        // Arrange
        _context.AccountsPayable.RemoveRange(_context.AccountsPayable);
        await _context.SaveChangesAsync();

        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().BeGreaterThan(0);
        result.TotalExpenses.Should().Be(0);
        result.NetIncome.Should().Be(result.TotalRevenue);
        result.NetProfitMargin.Should().Be(100);
    }

    [Fact]
    public async Task GenerateCashFlowReport_WithCategoryFilter_ReturnsFilteredData()
    {
        // Arrange
        var filter = new FinancialReportFilterDto
        {
            CategoryId = 1 // Vendas category
        };

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(i => i.Category == "Vendas");
    }

    [Fact]
    public async Task GenerateProfitLossReport_HandlesNullCategory()
    {
        // Arrange
        var receivableWithoutCategory = new AccountReceivable
        {
            Id = 10,
            Description = "Receita sem categoria",
            Amount = 100,
            DueDate = DateTime.UtcNow,
            Status = "Pago",
            CategoryId = null,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccountsReceivable.Add(receivableWithoutCategory);
        await _context.SaveChangesAsync();

        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateProfitLossReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().BeGreaterThan(2500);
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_WithFutureDate_IncludesFutureObligations()
    {
        // Arrange
        var filter = new FinancialReportFilterDto
        {
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.GenerateBalanceSheetReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalLiabilities.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateCashFlowReport_IncludesAllStatuses()
    {
        // Arrange
        var filter = new FinancialReportFilterDto();

        // Act
        var result = await _service.GenerateCashFlowReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var statuses = result.Items.Select(i => i.Status).Distinct().ToList();
        statuses.Should().Contain("Pago");
        statuses.Should().Contain("Pendente");
        statuses.Should().Contain("Atrasado");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
