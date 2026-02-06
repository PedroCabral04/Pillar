using erp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace erp.Tests.Controllers;

public class FinancialDashboardControllerRouteTests
{
    [Fact]
    public void GetDashboardData_ShouldExposeFinancialDashboardAliasRoute()
    {
        var method = typeof(FinancialDashboardController).GetMethod(nameof(FinancialDashboardController.GetDashboardData));

        method.Should().NotBeNull();
        var getAttributes = method!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>()
            .ToList();

        getAttributes.Should().Contain(a => a.Template == "/api/financial-dashboard");
    }
}
