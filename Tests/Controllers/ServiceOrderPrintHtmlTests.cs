using System.Reflection;
using erp.Controllers;
using erp.DTOs.ServiceOrders;

namespace erp.Tests.Controllers;

public class ServiceOrderPrintHtmlTests
{
    [Fact]
    public void GeneratePrintHtml_UsesCompactA4PrintLayout()
    {
        var order = new ServiceOrderDto
        {
            OrderNumber = "OS-001",
            EntryDate = new DateTime(2026, 6, 3),
            Status = "Open",
            StatusDisplay = "Aberta",
            DeviceBrand = "Samsung",
            DeviceModel = "A10",
            ProblemDescription = "Tela quebrada",
            CustomerNotes = "Cliente pediu urgencia",
            TotalAmount = 120,
            NetAmount = 120,
            Items =
            [
                new ServiceOrderItemDto { Description = "Troca de tela", ServiceType = "Servico", Price = 120 }
            ]
        };

        var method = typeof(ServiceOrdersController).GetMethod(
            "GeneratePrintHtml",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var html = (string)method!.Invoke(null, [order, "Pillar", null])!;

        html.Should().Contain("@page { size: A4; margin: 8mm; }");
        html.Should().Contain("page-break-inside: avoid");
        html.Should().Contain("font-size: 10px");
    }
}
