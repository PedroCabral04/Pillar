using System.Linq;
using erp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace erp.Tests.Controllers;

public class PositionsControllerRouteTests
{
    [Fact]
    public void Controller_ShouldExposePositionsRoute()
    {
        var routeAttribute = typeof(PositionsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .SingleOrDefault();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/positions");
    }
}