using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using erp.DTOs.Reports;
using erp.DTOs.Sales;

namespace erp.Services.Reports;

public interface IPdfExportService
{
    byte[] ExportSalesReportToPdf(SalesReportResultDto report, SalesReportFilterDto filter);
    byte[] ExportCashFlowReportToPdf(CashFlowReportDto report, FinancialReportFilterDto filter);
    byte[] ExportProfitLossReportToPdf(ProfitLossReportDto report, FinancialReportFilterDto filter);
    byte[] ExportStockLevelsReportToPdf(StockLevelsReportDto report, InventoryReportFilterDto filter);
    byte[] ExportHeadcountReportToPdf(HeadcountReportDto report, HRReportFilterDto filter);
    byte[] ExportSaleToPdf(SaleDto sale, string? logoPath = null);
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
                            summaryCol.Item().Text($"Receita Total: {CurrencyFormatService.FormatStatic(report.Summary.TotalRevenue)}");
                            summaryCol.Item().Text($"Descontos: {CurrencyFormatService.FormatStatic(report.Summary.TotalDiscounts)}");
                            summaryCol.Item().Text($"Receita Líquida: {CurrencyFormatService.FormatStatic(report.Summary.NetRevenue)}");
                            summaryCol.Item().Text($"Ticket Médio: {CurrencyFormatService.FormatStatic(report.Summary.AverageTicket)}");
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
                                table.Cell().Element(CellStyle).Text(CurrencyFormatService.FormatStatic(item.NetAmount));
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
                            summaryCol.Item().Text($"Total Receitas: {CurrencyFormatService.FormatStatic(report.Summary.TotalRevenue)}");
                            summaryCol.Item().Text($"Total Despesas: {CurrencyFormatService.FormatStatic(report.Summary.TotalExpenses)}");
                            summaryCol.Item().Text($"Fluxo de Caixa Líquido: {CurrencyFormatService.FormatStatic(report.Summary.NetCashFlow)}").Bold();
                            summaryCol.Item().Text($"Contas a Receber Pendentes: {CurrencyFormatService.FormatStatic(report.Summary.PendingReceivables)}");
                            summaryCol.Item().Text($"Contas a Pagar Pendentes: {CurrencyFormatService.FormatStatic(report.Summary.PendingPayables)}");
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
                                table.Cell().Element(CellStyle).Text(CurrencyFormatService.FormatStatic(item.Amount));

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
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.TotalRevenue));

                            table.Cell().Text("(-) Custo dos Produtos Vendidos");
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.CostOfGoodsSold));

                            table.Cell().Text("(=) Lucro Bruto").Bold();
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.GrossProfit)).Bold();

                            table.Cell().Text("(-) Despesas Operacionais");
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.OperatingExpenses));

                            table.Cell().Text("(=) Resultado Operacional").Bold();
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.OperatingIncome)).Bold();

                            table.Cell().Text("(=) Lucro Líquido").Bold().FontSize(12);
                            table.Cell().AlignRight().Text(CurrencyFormatService.FormatStatic(report.NetIncome)).Bold().FontSize(12);
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
                                    table.Cell().Text(CurrencyFormatService.FormatStatic(expense.Amount));
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

                        column.Item().Text($"Valor Total do Estoque: {CurrencyFormatService.FormatStatic(report.Summary.TotalInventoryValue)}").Bold().FontSize(12);

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
                                table.Cell().Text(CurrencyFormatService.FormatStatic(item.CostPrice));
                                table.Cell().Text(CurrencyFormatService.FormatStatic(item.TotalValue));
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

    public byte[] ExportSaleToPdf(SaleDto sale, string? logoPath = null)
    {
        var headerColor = Colors.Blue.Medium;
        
        var statusColor = sale.Status switch
        {
            "Pendente" => Colors.Orange.Medium,
            "Finalizada" => Colors.Green.Medium,
            "Cancelada" => Colors.Red.Medium,
            _ => Colors.Grey.Medium
        };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        row.RelativeItem().Column(logoCol =>
                        {
                            // Try to load logo if path provided
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                try
                                {
                                    logoCol.Item().Height(50).Image(logoPath, ImageScaling.FitHeight);
                                }
                                catch
                                {
                                    logoCol.Item().Text("Pillar ERP").Bold().FontSize(24).FontColor(headerColor);
                                }
                            }
                            else
                            {
                                logoCol.Item().Text("Pillar ERP").Bold().FontSize(24).FontColor(headerColor);
                            }
                            logoCol.Item().Text("Sistema de Gestão Empresarial").FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(saleCol =>
                        {
                            saleCol.Item().Text(sale.SaleNumber).Bold().FontSize(18).FontColor(headerColor);
                            saleCol.Item().AlignRight().Background(statusColor).Padding(4)
                                .Text(sale.Status).FontColor(Colors.White).FontSize(10).Bold();
                        });
                    });

                    headerCol.Item().PaddingTop(10).LineHorizontal(2).LineColor(headerColor);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(15);

                    // Sale and Customer Info
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(infoCol =>
                        {
                            infoCol.Item().Text("Informações da Venda").Bold().FontSize(12).FontColor(headerColor);
                            infoCol.Item().PaddingTop(5);
                            infoCol.Item().Text($"Data da Venda: {sale.SaleDate:dd/MM/yyyy HH:mm}");
                            infoCol.Item().Text($"Data de Criação: {sale.CreatedAt:dd/MM/yyyy HH:mm}");
                            infoCol.Item().Text($"Pagamento: {sale.PaymentMethod ?? "Não informado"}");
                        });

                        row.ConstantItem(15);

                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(infoCol =>
                        {
                            infoCol.Item().Text("Cliente e Vendedor").Bold().FontSize(12).FontColor(headerColor);
                            infoCol.Item().PaddingTop(5);
                            infoCol.Item().Text($"Cliente: {sale.CustomerName ?? "Não informado"}");
                            infoCol.Item().Text($"Vendedor: {sale.UserName}");
                        });
                    });

                    // Items Table
                    column.Item().Text($"Itens da Venda ({sale.Items.Count} itens)").Bold().FontSize(12).FontColor(headerColor);
                    
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);   // SKU
                            columns.RelativeColumn(2);     // Produto
                            columns.ConstantColumn(50);   // Qtd
                            columns.ConstantColumn(70);   // Preço Unit.
                            columns.ConstantColumn(60);   // Desconto
                            columns.ConstantColumn(80);   // Subtotal
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("SKU");
                            header.Cell().Element(HeaderCellStyle).Text("Produto");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qtd");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Preço Unit.");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Desconto");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Subtotal");

                            static IContainer HeaderCellStyle(IContainer container)
                            {
                                return container
                                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
                                    .Background(Colors.Blue.Medium)
                                    .PaddingVertical(6)
                                    .PaddingHorizontal(4);
                            }
                        });

                        // Rows
                        foreach (var item in sale.Items)
                        {
                            table.Cell().Element(CellStyle).Text(item.ProductSku).FontSize(9);
                            table.Cell().Element(CellStyle).Text(item.ProductName);
                            table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text(CurrencyFormatService.FormatStatic(item.UnitPrice));
                            table.Cell().Element(CellStyle).AlignRight().Text(CurrencyFormatService.FormatStatic(item.Discount));
                            table.Cell().Element(CellStyle).AlignRight().Text(CurrencyFormatService.FormatStatic(item.Total)).Bold();

                            static IContainer CellStyle(IContainer container)
                            {
                                return container
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(4);
                            }
                        }
                    });

                    // Notes if any
                    if (!string.IsNullOrWhiteSpace(sale.Notes))
                    {
                        column.Item().Background(Colors.Orange.Lighten4).BorderLeft(4).BorderColor(Colors.Orange.Medium).Padding(10).Column(notesCol =>
                        {
                            notesCol.Item().Text("Observações").Bold().FontColor(Colors.Orange.Darken2);
                            notesCol.Item().PaddingTop(5).Text(sale.Notes);
                        });
                    }

                    // Totals
                    column.Item().AlignRight().Width(250).Background(headerColor).Padding(15).Column(totalsCol =>
                    {
                        totalsCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal dos Itens").FontColor(Colors.White);
                            row.ConstantItem(80).AlignRight().Text(CurrencyFormatService.FormatStatic(sale.TotalAmount)).FontColor(Colors.White);
                        });
                        totalsCol.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Desconto Geral").FontColor(Colors.White);
                            row.ConstantItem(80).AlignRight().Text($"- {CurrencyFormatService.FormatStatic(sale.DiscountAmount)}").FontColor(Colors.White);
                        });
                        totalsCol.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.White);
                        totalsCol.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(14).FontColor(Colors.White);
                            row.ConstantItem(80).AlignRight().Text(CurrencyFormatService.FormatStatic(sale.NetAmount)).Bold().FontSize(14).FontColor(Colors.White);
                        });
                    });
                });

                page.Footer().Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    footerCol.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignCenter().Text("Pillar ERP").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
