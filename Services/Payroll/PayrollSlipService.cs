using System.Security.Cryptography;
using erp.Data;
using erp.Models.Identity;
using erp.Models.Payroll;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace erp.Services.Payroll;

public interface IPayrollSlipService
{
    Task<PayrollSlip> GenerateAsync(PayrollResult result, int generatedById, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(PayrollSlip slip, CancellationToken cancellationToken = default);
}

public class PayrollSlipService : IPayrollSlipService
{
    private static bool _questPdfConfigured;

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PayrollSlipService> _logger;

    public PayrollSlipService(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<PayrollSlipService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;

        if (!_questPdfConfigured)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
            _questPdfConfigured = true;
        }
    }

    public async Task<PayrollSlip> GenerateAsync(PayrollResult result, int generatedById, CancellationToken cancellationToken = default)
    {
        if (result.PayrollPeriod == null)
        {
            throw new InvalidOperationException("O período da folha deve ser carregado para gerar o holerite.");
        }

        var generatedBy = await _context.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == generatedById, cancellationToken)
            ?? throw new KeyNotFoundException("Usuário solicitante não encontrado.");

        var pdfBytes = BuildPdf(result);
        var relativePath = BuildRelativePath(result);
        var physicalPath = Path.Combine(GetRootDirectory(), relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        await File.WriteAllBytesAsync(physicalPath, pdfBytes, cancellationToken);

        var hash = Convert.ToHexString(SHA256.HashData(pdfBytes));

        var slip = result.Slip ?? new PayrollSlip { PayrollResultId = result.Id };
        slip.FilePath = NormalizePath(relativePath);
        slip.FileHash = hash;
        slip.ContentType = "application/pdf";
        slip.FileSize = pdfBytes.LongLength;
        slip.GeneratedAt = DateTime.UtcNow;
        slip.GeneratedById = generatedById;
        slip.GeneratedBy = generatedBy;

        if (result.Slip == null)
        {
            result.Slip = slip;
            _context.PayrollSlips.Add(slip);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return slip;
    }

    public async Task<byte[]> ReadAsync(PayrollSlip slip, CancellationToken cancellationToken = default)
    {
        var physicalPath = Path.Combine(GetRootDirectory(), slip.FilePath ?? string.Empty);
        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning("Holerite {SlipId} não encontrado em {Path}. Será regenerado no próximo acesso.", slip.Id, physicalPath);
            return Array.Empty<byte>();
        }

        return await File.ReadAllBytesAsync(physicalPath, cancellationToken);
    }

    private static string BuildRelativePath(PayrollResult result)
    {
        var period = result.PayrollPeriod!;
        var fileName = $"holerite-{result.EmployeeId}-{period.ReferenceYear}-{period.ReferenceMonth}.pdf";
        return Path.Combine("payroll-slips", $"period-{period.ReferenceYear}-{period.ReferenceMonth:00}", fileName);
    }

    private string GetRootDirectory()
    {
        var root = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        return root;
    }

    private static string NormalizePath(string relativePath)
    {
        return relativePath.Replace("\\", "/");
    }

    private static byte[] BuildPdf(PayrollResult result)
    {
        var period = result.PayrollPeriod!;
        var headerColor = Colors.Blue.Medium;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Column(column =>
                {
                    column.Item().Text("Pillar ERP").SemiBold().FontSize(18).FontColor(headerColor);
                    column.Item().Text($"Holerite referente a {period.ReferenceMonth:00}/{period.ReferenceYear}");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().BorderBottom(1).BorderColor(headerColor).PaddingBottom(5)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(info =>
                            {
                                info.Item().Text("Colaborador").SemiBold();
                                info.Item().Text(result.EmployeeNameSnapshot);
                                info.Item().Text($"CPF: {result.EmployeeCpfSnapshot ?? "-"}").FontSize(10);
                            });

                            row.RelativeItem().Column(info =>
                            {
                                info.Item().Text("Departamento").SemiBold();
                                info.Item().Text(result.DepartmentSnapshot ?? "-");
                                info.Item().Text(result.PositionSnapshot ?? "").FontSize(10);
                            });
                        });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Descrição").SemiBold();
                            header.Cell().AlignRight().Text("Referência").SemiBold();
                            header.Cell().AlignRight().Text("Proventos").SemiBold();
                            header.Cell().AlignRight().Text("Descontos").SemiBold();
                        });

                        foreach (var component in result.Components.OrderBy(c => c.Sequence))
                        {
                            table.Cell().Text(component.Description);
                            table.Cell().AlignRight().Text(component.ReferenceQuantity?.ToString("0.##") ?? "-");
                            table.Cell().AlignRight().Text(component.Type == PayrollComponentType.Earning ? component.Amount.ToString("C") : "-");
                            table.Cell().AlignRight().Text(component.Type != PayrollComponentType.Earning ? component.Amount.ToString("C") : "-");
                        }
                    });

                    column.Item().BorderTop(1).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Column(summary =>
                        {
                            summary.Item().Text($"Total de Proventos: {result.TotalEarnings:C}");
                            summary.Item().Text($"Total de Descontos: {result.TotalDeductions:C}");
                        });

                        row.RelativeItem().AlignRight().Text($"Líquido: {result.NetAmount:C}").FontSize(16).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text($"Gerado em {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC");
            });
        });

        return document.GeneratePdf();
    }
}
