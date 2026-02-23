using erp.Data;
using erp.Mappings;
using erp.Models.Inventory;
using erp.Models.Identity;
using erp.Services.Inventory;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using OfficeOpenXml;
using System.Reflection;
using Xunit;

namespace erp.Tests.Services.Inventory;

public class ProductImportOptimizationTests
{
    [Fact]
    public async Task ImportProductsFromExcel_UsesSingleSaveChangesForMultipleValidRows()
    {
        var tenantContext = BuildTenantContext(1);
        var tenantAccessor = new Mock<ITenantContextAccessor>();
        tenantAccessor.SetupGet(x => x.Current).Returns(tenantContext);
        var saveChangesCounter = new SaveChangesCounterInterceptor();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(saveChangesCounter)
            .Options;

        await using var context = new ApplicationDbContext(options, tenantContextAccessor: tenantAccessor.Object);

        context.ProductCategories.Add(new ProductCategory
        {
            Id = 1,
            Name = "Informatica",
            Code = "INFO",
            IsActive = true
        });

        context.Users.Add(new ApplicationUser
        {
            Id = 1,
            UserName = "import.tester",
            Email = "import.tester@local"
        });

        await context.SaveChangesAsync();
        saveChangesCounter.Reset();

        var service = new InventoryService(context, new ProductMapper());
        await using var fileStream = BuildTemplateWithTwoValidRows();

        var result = await service.ImportProductsFromExcelAsync(fileStream, userId: 1, CancellationToken.None);

        result.ImportedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        saveChangesCounter.Count.Should().Be(2);
    }

    [Fact]
    public async Task ImportProductsFromExcel_CreatesCategoryAsActive_WhenAtivaColumnIsBlank()
    {
        var tenantContext = BuildTenantContext(1);
        var tenantAccessor = new Mock<ITenantContextAccessor>();
        tenantAccessor.SetupGet(x => x.Current).Returns(tenantContext);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options, tenantContextAccessor: tenantAccessor.Object);

        context.Users.Add(new ApplicationUser
        {
            Id = 1,
            UserName = "import.tester",
            Email = "import.tester@local"
        });

        await context.SaveChangesAsync();

        var service = new InventoryService(context, new ProductMapper());
        await using var fileStream = BuildTemplateWithNewCategoryAndBlankAtiva();

        var result = await service.ImportProductsFromExcelAsync(fileStream, userId: 1, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(0);

        var importedCategory = await context.ProductCategories
            .AsNoTracking()
            .SingleAsync(c => c.Code == "NOVA-CAT");

        importedCategory.Name.Should().Be("Nova Categoria");
        importedCategory.IsActive.Should().BeTrue();
    }

    private static TenantContext BuildTenantContext(int tenantId)
    {
        var context = new TenantContext();
        var property = typeof(TenantContext)
            .GetProperty(nameof(TenantContext.TenantId), BindingFlags.Instance | BindingFlags.Public);

        property!.SetValue(context, tenantId);
        return context;
    }

    private static MemoryStream BuildTemplateWithTwoValidRows()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Produtos");

        worksheet.Cells[1, 1].Value = "SKU";
        worksheet.Cells[1, 2].Value = "Nome";
        worksheet.Cells[1, 3].Value = "Categoria";
        worksheet.Cells[1, 4].Value = "Preco de Venda";

        worksheet.Cells[2, 1].Value = "PROD-001";
        worksheet.Cells[2, 2].Value = "Mouse";
        worksheet.Cells[2, 3].Value = "1";
        worksheet.Cells[2, 4].Value = "150.00";

        worksheet.Cells[3, 1].Value = "";
        worksheet.Cells[3, 2].Value = "Teclado";
        worksheet.Cells[3, 3].Value = "Informatica";
        worksheet.Cells[3, 4].Value = "90.00";

        return new MemoryStream(package.GetAsByteArray());
    }

    private static MemoryStream BuildTemplateWithNewCategoryAndBlankAtiva()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();

        var productsSheet = package.Workbook.Worksheets.Add("Produtos");
        productsSheet.Cells[1, 1].Value = "SKU";
        productsSheet.Cells[1, 2].Value = "Nome";
        productsSheet.Cells[1, 3].Value = "Categoria";
        productsSheet.Cells[1, 4].Value = "Preco de Venda";

        productsSheet.Cells[2, 1].Value = "PROD-001";
        productsSheet.Cells[2, 2].Value = "Produto Nova Categoria";
        productsSheet.Cells[2, 3].Value = "Nova Categoria";
        productsSheet.Cells[2, 4].Value = "120.00";

        var categoriesSheet = package.Workbook.Worksheets.Add("Categorias");
        categoriesSheet.Cells[1, 1].Value = "ID";
        categoriesSheet.Cells[1, 2].Value = "Nome";
        categoriesSheet.Cells[1, 3].Value = "Codigo";
        categoriesSheet.Cells[1, 4].Value = "Ativa";

        categoriesSheet.Cells[2, 1].Value = string.Empty;
        categoriesSheet.Cells[2, 2].Value = "Nova Categoria";
        categoriesSheet.Cells[2, 3].Value = "NOVA-CAT";
        categoriesSheet.Cells[2, 4].Value = string.Empty;

        return new MemoryStream(package.GetAsByteArray());
    }

    private sealed class SaveChangesCounterInterceptor : SaveChangesInterceptor
    {
        public int Count { get; private set; }

        public void Reset()
        {
            Count = 0;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
