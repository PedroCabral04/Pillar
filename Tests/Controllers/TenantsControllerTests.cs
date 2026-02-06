using erp.Controllers;
using erp.DTOs.Tenancy;
using erp.Security;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace erp.Tests.Controllers;

public class TenantsControllerTests
{
    private readonly Mock<ITenantService> _tenantService = new();
    private readonly Mock<ITenantBrandingService> _brandingService = new();
    private readonly Mock<IFileValidationService> _fileValidationService = new();
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _controller = new TenantsController(_tenantService.Object, _brandingService.Object, _fileValidationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "mock"))
                }
            }
        };
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsOkWithMembers()
    {
        var members = new List<TenantMemberDto>
        {
            new(1, "user1", "User One", "user1@acme.com", false, DateTime.UtcNow, null)
        };

        _tenantService.Setup(s => s.GetMembersAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(members);

        var result = await _controller.GetMembersAsync(10, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(members);
    }

    [Fact]
    public async Task AssignMemberAsync_WhenTenantExists_ReturnsOk()
    {
        var member = new TenantMemberDto(2, "user2", "User Two", "user2@acme.com", false, DateTime.UtcNow, null);
        _tenantService
            .Setup(s => s.AssignMemberAsync(10, 2, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _controller.AssignMemberAsync(10, 2, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(member);
    }

    [Fact]
    public async Task AssignMemberAsync_WhenTenantMissing_ReturnsNotFound()
    {
        _tenantService
            .Setup(s => s.AssignMemberAsync(99, 2, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.AssignMemberAsync(99, 2, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RevokeMemberAsync_ReturnsNoContent()
    {
        var result = await _controller.RevokeMemberAsync(10, 2, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _tenantService.Verify(s => s.RevokeMemberAsync(10, 2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
