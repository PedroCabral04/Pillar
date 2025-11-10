using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace erp.Tests.Auth;

public class TwoFactorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TwoFactorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnableTwoFactor_ShouldReturn_QrAndSharedKey_ForAuthenticatedUser()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as seeded admin
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@erp.local",
            password = "Admin@123!",
            rememberMe = false
        });
        loginResp.EnsureSuccessStatusCode();

        // Call enable 2FA
        var enableResp = await client.PostAsJsonAsync("/api/two-factor/enable", new { });
        enableResp.EnsureSuccessStatusCode();

        var payload = await enableResp.Content.ReadFromJsonAsync<EnableTwoFactorResponseDto>();
        payload.Should().NotBeNull();
        payload!.SharedKey.Should().NotBeNullOrWhiteSpace();
        payload!.QrCodeBase64.Should().NotBeNullOrWhiteSpace();
        payload!.AuthenticatorUri.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public async Task VerifySetup_WithInvalidCode_ShouldReturn_BadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as seeded admin
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@erp.local",
            password = "Admin@123!",
            rememberMe = false
        });
        loginResp.EnsureSuccessStatusCode();

        // Start setup to ensure a key exists
        var _ = await client.PostAsJsonAsync("/api/two-factor/enable", new { });

        // Try verify with an invalid code
        var verifyResp = await client.PostAsJsonAsync("/api/two-factor/verify-setup", new { code = "000000" });
        verifyResp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    private class EnableTwoFactorResponseDto
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
    }
}
