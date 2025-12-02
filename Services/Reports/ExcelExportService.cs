using OfficeOpenXml;
using OfficeOpenXml.Style;
using erp.DTOs.Reports;
using System.Drawing;

namespace erp.Services.Reports;

public interface IExcelExportService
{
    byte[] ExportSalesReportToExcel(SalesReportResultDto report, SalesReportFilterDto filter);
    byte[] ExportCashFlowReportToExcel(CashFlowReportDto report, FinancialReportFilterDto filter);
    byte[] ExportProfitLossReportToExcel(ProfitLossReportDto report, FinancialReportFilterDto filter);
    byte[] ExportStockLevelsReportToExcel(StockLevelsReportDto report, InventoryReportFilterDto filter);
    byte[] ExportStockMovementReportToExcel(StockMovementReportDto report, InventoryReportFilterDto filter);
    byte[] ExportHeadcountReportToExcel(HeadcountReportDto report, HRReportFilterDto filter);
}

public class ExcelExportService : IExcelExportService
{
    public ExcelExportService()
    {
        // Set EPPlus license context (NonCommercial for free use, or Commercial if you have a license)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public byte[] ExportSalesReportToExcel(SalesReportResultDto report, SalesReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Vendas");

        // Title
        worksheet.Cells["A1"].Value = "Relatório de Vendas";
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Filter info
        worksheet.Cells["A2"].Value = $"Período: {filter.StartDate?.ToString("dd/MM/yyyy") ?? "Início"} até {filter.EndDate?.ToString("dd/MM/yyyy") ?? "Hoje"}";
        worksheet.Cells["A2:H2"].Merge = true;

        // Summary
        var summaryRow = 4;
        worksheet.Cells[$"A{summaryRow}"].Value = "Resumo";
        worksheet.Cells[$"A{summaryRow}"].Style.Font.Bold = true;
        
        worksheet.Cells[$"A{summaryRow + 1}"].Value = "Total de Vendas:";
        worksheet.Cells[$"B{summaryRow + 1}"].Value = report.Summary.TotalSales;
        
        worksheet.Cells[$"A{summaryRow + 2}"].Value = "Receita Total:";
        worksheet.Cells[$"B{summaryRow + 2}"].Value = report.Summary.TotalRevenue;
        worksheet.Cells[$"B{summaryRow + 2}"].Style.Numberformat.Format = "R$ #,##0.00";
        
        worksheet.Cells[$"A{summaryRow + 3}"].Value = "Descontos:";
        worksheet.Cells[$"B{summaryRow + 3}"].Value = report.Summary.TotalDiscounts;
        worksheet.Cells[$"B{summaryRow + 3}"].Style.Numberformat.Format = "R$ #,##0.00";
        
        worksheet.Cells[$"A{summaryRow + 4}"].Value = "Receita Líquida:";
        worksheet.Cells[$"B{summaryRow + 4}"].Value = report.Summary.NetRevenue;
        worksheet.Cells[$"B{summaryRow + 4}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"B{summaryRow + 4}"].Style.Font.Bold = true;

        // Headers
        var headerRow = summaryRow + 6;
        worksheet.Cells[$"A{headerRow}"].Value = "Número";
        worksheet.Cells[$"B{headerRow}"].Value = "Data";
        worksheet.Cells[$"C{headerRow}"].Value = "Cliente";
        worksheet.Cells[$"D{headerRow}"].Value = "Vendedor";
        worksheet.Cells[$"E{headerRow}"].Value = "Total";
        worksheet.Cells[$"F{headerRow}"].Value = "Desconto";
        worksheet.Cells[$"G{headerRow}"].Value = "Líquido";
        worksheet.Cells[$"H{headerRow}"].Value = "Status";

        using (var range = worksheet.Cells[$"A{headerRow}:H{headerRow}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
        }

        // Data rows
        var currentRow = headerRow + 1;
        foreach (var item in report.Items)
        {
            worksheet.Cells[$"A{currentRow}"].Value = item.SaleNumber;
            worksheet.Cells[$"B{currentRow}"].Value = item.SaleDate;
            worksheet.Cells[$"B{currentRow}"].Style.Numberformat.Format = "dd/mm/yyyy";
            worksheet.Cells[$"C{currentRow}"].Value = item.CustomerName;
            worksheet.Cells[$"D{currentRow}"].Value = item.SalespersonName;
            worksheet.Cells[$"E{currentRow}"].Value = item.TotalAmount;
            worksheet.Cells[$"E{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"F{currentRow}"].Value = item.DiscountAmount;
            worksheet.Cells[$"F{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"G{currentRow}"].Value = item.NetAmount;
            worksheet.Cells[$"G{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"H{currentRow}"].Value = item.Status;
            currentRow++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public byte[] ExportCashFlowReportToExcel(CashFlowReportDto report, FinancialReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Fluxo de Caixa");

        // Title
        worksheet.Cells["A1"].Value = "Relatório de Fluxo de Caixa";
        worksheet.Cells["A1:F1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Summary
        var summaryRow = 3;
        worksheet.Cells[$"A{summaryRow}"].Value = "Resumo Financeiro";
        worksheet.Cells[$"A{summaryRow}"].Style.Font.Bold = true;
        
        worksheet.Cells[$"A{summaryRow + 1}"].Value = "Total Receitas:";
        worksheet.Cells[$"B{summaryRow + 1}"].Value = report.Summary.TotalRevenue;
        worksheet.Cells[$"B{summaryRow + 1}"].Style.Numberformat.Format = "R$ #,##0.00";
        
        worksheet.Cells[$"A{summaryRow + 2}"].Value = "Total Despesas:";
        worksheet.Cells[$"B{summaryRow + 2}"].Value = report.Summary.TotalExpenses;
        worksheet.Cells[$"B{summaryRow + 2}"].Style.Numberformat.Format = "R$ #,##0.00";
        
        worksheet.Cells[$"A{summaryRow + 3}"].Value = "Fluxo Líquido:";
        worksheet.Cells[$"B{summaryRow + 3}"].Value = report.Summary.NetCashFlow;
        worksheet.Cells[$"B{summaryRow + 3}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{summaryRow + 3}:B{summaryRow + 3}"].Style.Font.Bold = true;
        
        worksheet.Cells[$"A{summaryRow + 4}"].Value = "Contas a Receber Pendentes:";
        worksheet.Cells[$"B{summaryRow + 4}"].Value = report.Summary.PendingReceivables;
        worksheet.Cells[$"B{summaryRow + 4}"].Style.Numberformat.Format = "R$ #,##0.00";
        
        worksheet.Cells[$"A{summaryRow + 5}"].Value = "Contas a Pagar Pendentes:";
        worksheet.Cells[$"B{summaryRow + 5}"].Value = report.Summary.PendingPayables;
        worksheet.Cells[$"B{summaryRow + 5}"].Style.Numberformat.Format = "R$ #,##0.00";

        // Headers
        var headerRow = summaryRow + 7;
        worksheet.Cells[$"A{headerRow}"].Value = "Data";
        worksheet.Cells[$"B{headerRow}"].Value = "Descrição";
        worksheet.Cells[$"C{headerRow}"].Value = "Tipo";
        worksheet.Cells[$"D{headerRow}"].Value = "Categoria";
        worksheet.Cells[$"E{headerRow}"].Value = "Valor";
        worksheet.Cells[$"F{headerRow}"].Value = "Status";

        using (var range = worksheet.Cells[$"A{headerRow}:F{headerRow}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
        }

        // Data rows
        var currentRow = headerRow + 1;
        foreach (var item in report.Items)
        {
            worksheet.Cells[$"A{currentRow}"].Value = item.Date;
            worksheet.Cells[$"A{currentRow}"].Style.Numberformat.Format = "dd/mm/yyyy";
            worksheet.Cells[$"B{currentRow}"].Value = item.Description;
            worksheet.Cells[$"C{currentRow}"].Value = item.Type;
            worksheet.Cells[$"D{currentRow}"].Value = item.Category;
            worksheet.Cells[$"E{currentRow}"].Value = item.Amount;
            worksheet.Cells[$"E{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"F{currentRow}"].Value = item.Status;
            currentRow++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public byte[] ExportProfitLossReportToExcel(ProfitLossReportDto report, FinancialReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("DRE");

        // Title
        worksheet.Cells["A1"].Value = "Demonstrativo de Resultados (DRE)";
        worksheet.Cells["A1:B1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A2"].Value = $"Período: {filter.StartDate?.ToString("dd/MM/yyyy")} até {filter.EndDate?.ToString("dd/MM/yyyy")}";
        worksheet.Cells["A2:B2"].Merge = true;

        // Main statement
        var row = 4;
        worksheet.Cells[$"A{row}"].Value = "Receita Total";
        worksheet.Cells[$"B{row}"].Value = report.TotalRevenue;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;

        row++;
        worksheet.Cells[$"A{row}"].Value = "(-) Custo dos Produtos Vendidos";
        worksheet.Cells[$"B{row}"].Value = report.CostOfGoodsSold;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";

        row++;
        worksheet.Cells[$"A{row}"].Value = "(=) Lucro Bruto";
        worksheet.Cells[$"B{row}"].Value = report.GrossProfit;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;

        row++;
        worksheet.Cells[$"A{row}"].Value = "(-) Despesas Operacionais";
        worksheet.Cells[$"B{row}"].Value = report.OperatingExpenses;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";

        row++;
        worksheet.Cells[$"A{row}"].Value = "(=) Resultado Operacional";
        worksheet.Cells[$"B{row}"].Value = report.OperatingIncome;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;

        row += 2;
        worksheet.Cells[$"A{row}"].Value = "(=) LUCRO LÍQUIDO";
        worksheet.Cells[$"B{row}"].Value = report.NetIncome;
        worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{row}:B{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:B{row}"].Style.Font.Size = 12;
        using (var range = worksheet.Cells[$"A{row}:B{row}"])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
        }

        row += 2;
        worksheet.Cells[$"A{row}"].Value = "Margem Bruta:";
        worksheet.Cells[$"B{row}"].Value = $"{report.GrossProfitMargin:F2}%";
        
        row++;
        worksheet.Cells[$"A{row}"].Value = "Margem Líquida:";
        worksheet.Cells[$"B{row}"].Value = $"{report.NetProfitMargin:F2}%";

        // Expenses by category
        if (report.ExpensesByCategory.Any())
        {
            row += 3;
            worksheet.Cells[$"A{row}"].Value = "Despesas por Categoria";
            worksheet.Cells[$"A{row}:C{row}"].Merge = true;
            worksheet.Cells[$"A{row}"].Style.Font.Bold = true;

            row++;
            worksheet.Cells[$"A{row}"].Value = "Categoria";
            worksheet.Cells[$"B{row}"].Value = "Valor";
            worksheet.Cells[$"C{row}"].Value = "%";
            using (var range = worksheet.Cells[$"A{row}:C{row}"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }

            foreach (var expense in report.ExpensesByCategory)
            {
                row++;
                worksheet.Cells[$"A{row}"].Value = expense.Category;
                worksheet.Cells[$"B{row}"].Value = expense.Amount;
                worksheet.Cells[$"B{row}"].Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells[$"C{row}"].Value = $"{expense.Percentage:F1}%";
            }
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public byte[] ExportStockLevelsReportToExcel(StockLevelsReportDto report, InventoryReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Níveis de Estoque");

        // Title
        worksheet.Cells["A1"].Value = "Relatório de Níveis de Estoque";
        worksheet.Cells["A1:I1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Summary
        var summaryRow = 3;
        worksheet.Cells[$"A{summaryRow}"].Value = $"Total de Produtos: {report.Summary.TotalProducts}";
        worksheet.Cells[$"C{summaryRow}"].Value = $"Em Estoque: {report.Summary.ProductsInStock}";
        worksheet.Cells[$"E{summaryRow}"].Value = $"Estoque Baixo: {report.Summary.ProductsLowStock}";
        worksheet.Cells[$"G{summaryRow}"].Value = $"Sem Estoque: {report.Summary.ProductsOutOfStock}";

        summaryRow++;
        worksheet.Cells[$"A{summaryRow}"].Value = "Valor Total do Estoque:";
        worksheet.Cells[$"B{summaryRow}"].Value = report.Summary.TotalInventoryValue;
        worksheet.Cells[$"B{summaryRow}"].Style.Numberformat.Format = "R$ #,##0.00";
        worksheet.Cells[$"A{summaryRow}:B{summaryRow}"].Style.Font.Bold = true;

        // Headers
        var headerRow = summaryRow + 2;
        worksheet.Cells[$"A{headerRow}"].Value = "SKU";
        worksheet.Cells[$"B{headerRow}"].Value = "Produto";
        worksheet.Cells[$"C{headerRow}"].Value = "Categoria";
        worksheet.Cells[$"D{headerRow}"].Value = "Estoque Atual";
        worksheet.Cells[$"E{headerRow}"].Value = "Estoque Mínimo";
        worksheet.Cells[$"F{headerRow}"].Value = "Unidade";
        worksheet.Cells[$"G{headerRow}"].Value = "Custo";
        worksheet.Cells[$"H{headerRow}"].Value = "Valor Total";
        worksheet.Cells[$"I{headerRow}"].Value = "Status";

        using (var range = worksheet.Cells[$"A{headerRow}:I{headerRow}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        // Data rows
        var currentRow = headerRow + 1;
        foreach (var item in report.Items)
        {
            worksheet.Cells[$"A{currentRow}"].Value = item.Sku;
            worksheet.Cells[$"B{currentRow}"].Value = item.ProductName;
            worksheet.Cells[$"C{currentRow}"].Value = item.Category;
            worksheet.Cells[$"D{currentRow}"].Value = item.CurrentStock;
            worksheet.Cells[$"D{currentRow}"].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[$"E{currentRow}"].Value = item.MinimumStock;
            worksheet.Cells[$"E{currentRow}"].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[$"F{currentRow}"].Value = item.Unit;
            worksheet.Cells[$"G{currentRow}"].Value = item.CostPrice;
            worksheet.Cells[$"G{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"H{currentRow}"].Value = item.TotalValue;
            worksheet.Cells[$"H{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"I{currentRow}"].Value = item.Status;

            // Color code status
            if (item.Status == "Sem Estoque")
            {
                worksheet.Cells[$"I{currentRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[$"I{currentRow}"].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
            }
            else if (item.Status == "Estoque Baixo")
            {
                worksheet.Cells[$"I{currentRow}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[$"I{currentRow}"].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            currentRow++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public byte[] ExportStockMovementReportToExcel(StockMovementReportDto report, InventoryReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Movimentações");

        // Title
        worksheet.Cells["A1"].Value = "Relatório de Movimentação de Estoque";
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Summary
        var summaryRow = 3;
        worksheet.Cells[$"A{summaryRow}"].Value = $"Total de Movimentações: {report.Summary.TotalMovements}";
        worksheet.Cells[$"C{summaryRow}"].Value = $"Valor Total: {CurrencyFormatService.FormatStatic(report.Summary.TotalValueMovements)}";

        // Headers
        var headerRow = summaryRow + 2;
        worksheet.Cells[$"A{headerRow}"].Value = "Data";
        worksheet.Cells[$"B{headerRow}"].Value = "Produto";
        worksheet.Cells[$"C{headerRow}"].Value = "Tipo";
        worksheet.Cells[$"D{headerRow}"].Value = "Motivo";
        worksheet.Cells[$"E{headerRow}"].Value = "Quantidade";
        worksheet.Cells[$"F{headerRow}"].Value = "Custo Unit.";
        worksheet.Cells[$"G{headerRow}"].Value = "Custo Total";
        worksheet.Cells[$"H{headerRow}"].Value = "Usuário";

        using (var range = worksheet.Cells[$"A{headerRow}:H{headerRow}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        }

        // Data rows
        var currentRow = headerRow + 1;
        foreach (var item in report.Items)
        {
            worksheet.Cells[$"A{currentRow}"].Value = item.Date;
            worksheet.Cells[$"A{currentRow}"].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
            worksheet.Cells[$"B{currentRow}"].Value = item.ProductName;
            worksheet.Cells[$"C{currentRow}"].Value = item.Type;
            worksheet.Cells[$"D{currentRow}"].Value = item.Reason;
            worksheet.Cells[$"E{currentRow}"].Value = item.Quantity;
            worksheet.Cells[$"E{currentRow}"].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[$"F{currentRow}"].Value = item.UnitCost;
            worksheet.Cells[$"F{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"G{currentRow}"].Value = item.TotalCost;
            worksheet.Cells[$"G{currentRow}"].Style.Numberformat.Format = "R$ #,##0.00";
            worksheet.Cells[$"H{currentRow}"].Value = item.UserName;
            currentRow++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public byte[] ExportHeadcountReportToExcel(HeadcountReportDto report, HRReportFilterDto filter)
    {
        using var package = new ExcelPackage();
        
        // Summary sheet
        var summarySheet = package.Workbook.Worksheets.Add("Resumo");
        summarySheet.Cells["A1"].Value = "Relatório de Headcount";
        summarySheet.Cells["A1"].Style.Font.Size = 16;
        summarySheet.Cells["A1"].Style.Font.Bold = true;

        var row = 3;
        summarySheet.Cells[$"A{row}"].Value = "Total de Colaboradores:";
        summarySheet.Cells[$"B{row}"].Value = report.Summary.TotalEmployees;
        row++;
        summarySheet.Cells[$"A{row}"].Value = "Total de Departamentos:";
        summarySheet.Cells[$"B{row}"].Value = report.Summary.TotalDepartments;
        row++;
        summarySheet.Cells[$"A{row}"].Value = "Total de Cargos:";
        summarySheet.Cells[$"B{row}"].Value = report.Summary.TotalPositions;
        row++;
        summarySheet.Cells[$"A{row}"].Value = "Tempo Médio de Casa (anos):";
        summarySheet.Cells[$"B{row}"].Value = report.Summary.AverageTenure;
        summarySheet.Cells[$"B{row}"].Style.Numberformat.Format = "0.0";

        // By Department sheet
        var deptSheet = package.Workbook.Worksheets.Add("Por Departamento");
        deptSheet.Cells["A1"].Value = "Departamento";
        deptSheet.Cells["B1"].Value = "Funcionários";
        deptSheet.Cells["C1"].Value = "Percentual";
        deptSheet.Cells["A1:C1"].Style.Font.Bold = true;

        row = 2;
        foreach (var item in report.ByDepartment)
        {
            deptSheet.Cells[$"A{row}"].Value = item.Department;
            deptSheet.Cells[$"B{row}"].Value = item.EmployeeCount;
            deptSheet.Cells[$"C{row}"].Value = item.Percentage / 100;
            deptSheet.Cells[$"C{row}"].Style.Numberformat.Format = "0.0%";
            row++;
        }

        // By Position sheet
        var posSheet = package.Workbook.Worksheets.Add("Por Cargo");
        posSheet.Cells["A1"].Value = "Cargo";
        posSheet.Cells["B1"].Value = "Funcionários";
        posSheet.Cells["C1"].Value = "Percentual";
        posSheet.Cells["A1:C1"].Style.Font.Bold = true;

        row = 2;
        foreach (var item in report.ByPosition)
        {
            posSheet.Cells[$"A{row}"].Value = item.Position;
            posSheet.Cells[$"B{row}"].Value = item.EmployeeCount;
            posSheet.Cells[$"C{row}"].Value = item.Percentage / 100;
            posSheet.Cells[$"C{row}"].Style.Numberformat.Format = "0.0%";
            row++;
        }

        summarySheet.Cells.AutoFitColumns();
        deptSheet.Cells.AutoFitColumns();
        posSheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }
}
