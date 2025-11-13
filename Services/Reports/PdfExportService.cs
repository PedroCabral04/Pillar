using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using erp.DTOs.Reports;

namespace erp.Services.Reports;

public interface IPdfExportService
{
    byte[] ExportSalesReportToPdf(SalesReportResultDto report, SalesReportFilterDto filter);
    byte[] ExportCashFlowReportToPdf(CashFlowReportDto report, FinancialReportFilterDto filter);
    byte[] ExportProfitLossReportToPdf(ProfitLossReportDto report, FinancialReportFilterDto filter);
    byte[] ExportStockLevelsReportToPdf(StockLevelsReportDto report, InventoryReportFilterDto filter);
    byte[] ExportHeadcountReportToPdf(HeadcountReportDto report, HRReportFilterDto filter);
}

public class PdfExportService : IPdfExportService
{
    public PdfExportService()
    {
        // Set QuestPDF license (Community license for free use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportSalesReportToPdf(SalesReportResultDto report, SalesReportFilterDto filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Relatório de Vendas")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Filter info
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Período: {filter.StartDate?.ToString("dd/MM/yyyy") ?? "Início"} até {filter.EndDate?.ToString("dd/MM/yyyy") ?? "Hoje"}");
                        });

                        // Summary
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(summaryCol =>
                        {
                            summaryCol.Item().Text("Resumo").Bold().FontSize(14);
                            summaryCol.Item().Text($"Total de Vendas: {report.Summary.TotalSales}");
                            summaryCol.Item().Text($"Receita Total: {report.Summary.TotalRevenue:C2}");
                            summaryCol.Item().Text($"Descontos: {report.Summary.TotalDiscounts:C2}");
                            summaryCol.Item().Text($"Receita Líquida: {report.Summary.NetRevenue:C2}");
                            summaryCol.Item().Text($"Ticket Médio: {report.Summary.AverageTicket:C2}");
                        });

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);  // Número
                                columns.RelativeColumn();     // Cliente
                                columns.ConstantColumn(70);  // Data
                                columns.ConstantColumn(80);  // Valor
                                columns.ConstantColumn(60);  // Status
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Número").Bold();
                                header.Cell().Element(CellStyle).Text("Cliente").Bold();
                                header.Cell().Element(CellStyle).Text("Data").Bold();
                                header.Cell().Element(CellStyle).Text("Valor").Bold();
                                header.Cell().Element(CellStyle).Text("Status").Bold();

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            // Rows
                            foreach (var item in report.Items.Take(50)) // Limit to 50 items per page
                            {
                                table.Cell().Element(CellStyle).Text(item.SaleNumber);
                                table.Cell().Element(CellStyle).Text(item.CustomerName);
                                table.Cell().Element(CellStyle).Text(item.SaleDate.ToString("dd/MM/yyyy"));
                                table.Cell().Element(CellStyle).Text(item.NetAmount.ToString("C2"));
                                table.Cell().Element(CellStyle).Text(item.Status);

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5);
                                }
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportCashFlowReportToPdf(CashFlowReportDto report, FinancialReportFilterDto filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Relatório de Fluxo de Caixa")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Summary
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(summaryCol =>
                        {
                            summaryCol.Item().Text("Resumo Financeiro").Bold().FontSize(14);
                            summaryCol.Item().Text($"Total Receitas: {report.Summary.TotalRevenue:C2}");
                            summaryCol.Item().Text($"Total Despesas: {report.Summary.TotalExpenses:C2}");
                            summaryCol.Item().Text($"Fluxo de Caixa Líquido: {report.Summary.NetCashFlow:C2}").Bold();
                            summaryCol.Item().Text($"Contas a Receber Pendentes: {report.Summary.PendingReceivables:C2}");
                            summaryCol.Item().Text($"Contas a Pagar Pendentes: {report.Summary.PendingPayables:C2}");
                        });

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);  // Data
                                columns.RelativeColumn(2);    // Descrição
                                columns.ConstantColumn(60);  // Tipo
                                columns.RelativeColumn();     // Categoria
                                columns.ConstantColumn(80);  // Valor
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Data").Bold();
                                header.Cell().Element(CellStyle).Text("Descrição").Bold();
                                header.Cell().Element(CellStyle).Text("Tipo").Bold();
                                header.Cell().Element(CellStyle).Text("Categoria").Bold();
                                header.Cell().Element(CellStyle).Text("Valor").Bold();

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var item in report.Items.Take(40))
                            {
                                table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yyyy"));
                                table.Cell().Element(CellStyle).Text(item.Description);
                                table.Cell().Element(CellStyle).Text(item.Type);
                                table.Cell().Element(CellStyle).Text(item.Category);
                                table.Cell().Element(CellStyle).Text(item.Amount.ToString("C2"));

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5);
                                }
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportProfitLossReportToPdf(ProfitLossReportDto report, FinancialReportFilterDto filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Demonstrativo de Resultados (DRE)")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Text($"Período: {filter.StartDate?.ToString("dd/MM/yyyy")} até {filter.EndDate?.ToString("dd/MM/yyyy")}");

                        // Main financial statement
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                            });

                            table.Cell().Text("Receita Total").Bold();
                            table.Cell().AlignRight().Text(report.TotalRevenue.ToString("C2"));

                            table.Cell().Text("(-) Custo dos Produtos Vendidos");
                            table.Cell().AlignRight().Text(report.CostOfGoodsSold.ToString("C2"));

                            table.Cell().Text("(=) Lucro Bruto").Bold();
                            table.Cell().AlignRight().Text(report.GrossProfit.ToString("C2")).Bold();

                            table.Cell().Text("(-) Despesas Operacionais");
                            table.Cell().AlignRight().Text(report.OperatingExpenses.ToString("C2"));

                            table.Cell().Text("(=) Resultado Operacional").Bold();
                            table.Cell().AlignRight().Text(report.OperatingIncome.ToString("C2")).Bold();

                            table.Cell().Text("(=) Lucro Líquido").Bold().FontSize(12);
                            table.Cell().AlignRight().Text(report.NetIncome.ToString("C2")).Bold().FontSize(12);
                        });

                        // Margins
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text($"Margem Bruta: {report.GrossProfitMargin:F2}%").Bold();
                            row.RelativeItem().Text($"Margem Líquida: {report.NetProfitMargin:F2}%").Bold();
                        });

                        // Expenses by category
                        if (report.ExpensesByCategory.Any())
                        {
                            column.Item().Text("Despesas por Categoria").Bold().FontSize(12);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Categoria").Bold();
                                    header.Cell().Text("Valor").Bold();
                                    header.Cell().Text("%").Bold();
                                });

                                foreach (var expense in report.ExpensesByCategory)
                                {
                                    table.Cell().Text(expense.Category);
                                    table.Cell().Text(expense.Amount.ToString("C2"));
                                    table.Cell().Text(expense.Percentage.ToString("F1") + "%");
                                }
                            });
                        }
                    });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportStockLevelsReportToPdf(StockLevelsReportDto report, InventoryReportFilterDto filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .Text("Relatório de Níveis de Estoque")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Summary
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text($"Total de Produtos: {report.Summary.TotalProducts}");
                            row.RelativeItem().Text($"Produtos em Estoque: {report.Summary.ProductsInStock}");
                            row.RelativeItem().Text($"Estoque Baixo: {report.Summary.ProductsLowStock}");
                            row.RelativeItem().Text($"Sem Estoque: {report.Summary.ProductsOutOfStock}");
                        });

                        column.Item().Text($"Valor Total do Estoque: {report.Summary.TotalInventoryValue:C2}").Bold().FontSize(12);

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);  // SKU
                                columns.RelativeColumn(2);    // Nome
                                columns.RelativeColumn();     // Categoria
                                columns.ConstantColumn(60);  // Estoque
                                columns.ConstantColumn(60);  // Mínimo
                                columns.ConstantColumn(60);  // Custo
                                columns.ConstantColumn(70);  // Valor Total
                                columns.ConstantColumn(70);  // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("SKU").Bold();
                                header.Cell().Text("Produto").Bold();
                                header.Cell().Text("Categoria").Bold();
                                header.Cell().Text("Estoque").Bold();
                                header.Cell().Text("Mínimo").Bold();
                                header.Cell().Text("Custo").Bold();
                                header.Cell().Text("Valor Total").Bold();
                                header.Cell().Text("Status").Bold();
                            });

                            foreach (var item in report.Items.Take(50))
                            {
                                table.Cell().Text(item.Sku);
                                table.Cell().Text(item.ProductName);
                                table.Cell().Text(item.Category);
                                table.Cell().Text($"{item.CurrentStock:N2} {item.Unit}");
                                table.Cell().Text($"{item.MinimumStock:N2}");
                                table.Cell().Text(item.CostPrice.ToString("C2"));
                                table.Cell().Text(item.TotalValue.ToString("C2"));
                                table.Cell().Text(item.Status);
                            }
                        });
                    });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportHeadcountReportToPdf(HeadcountReportDto report, HRReportFilterDto filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Relatório de Headcount (RH)")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Summary
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(summaryCol =>
                        {
                            summaryCol.Item().Text("Resumo Geral").Bold().FontSize(14);
                            summaryCol.Item().Text($"Total de Colaboradores: {report.Summary.TotalEmployees}");
                            summaryCol.Item().Text($"Total de Departamentos: {report.Summary.TotalDepartments}");
                            summaryCol.Item().Text($"Total de Cargos: {report.Summary.TotalPositions}");
                            summaryCol.Item().Text($"Tempo Médio de Casa: {report.Summary.AverageTenure:F1} anos");
                        });

                        // By Department
                        column.Item().Text("Por Departamento").Bold().FontSize(12);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Departamento").Bold();
                                header.Cell().Text("Funcionários").Bold();
                                header.Cell().Text("%").Bold();
                            });

                            foreach (var item in report.ByDepartment)
                            {
                                table.Cell().Text(item.Department);
                                table.Cell().Text(item.EmployeeCount.ToString());
                                table.Cell().Text(item.Percentage.ToString("F1") + "%");
                            }
                        });

                        // By Position
                        column.Item().Text("Por Cargo").Bold().FontSize(12);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Cargo").Bold();
                                header.Cell().Text("Funcionários").Bold();
                                header.Cell().Text("%").Bold();
                            });

                            foreach (var item in report.ByPosition.Take(10))
                            {
                                table.Cell().Text(item.Position);
                                table.Cell().Text(item.EmployeeCount.ToString());
                                table.Cell().Text(item.Percentage.ToString("F1") + "%");
                            }
                        });
                    });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }
}
