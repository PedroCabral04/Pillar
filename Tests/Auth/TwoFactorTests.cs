using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Text.Json;
using System.Web;
using OtpNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;

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
        await ResetUserTwoFactorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as seeded admin
        var loginResp = await client.PostAsJsonAsync("/api/autenticacao/login", new
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
        await ResetUserTwoFactorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as seeded admin
        var loginResp = await client.PostAsJsonAsync("/api/autenticacao/login", new
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

    [Fact]
    public async Task Full2FAFlow_Should_Enable_Verify_LoginWithAuthenticator_And_RememberMachine()
    {
        await ResetUserTwoFactorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1) Login with admin credentials (no 2FA yet)
        var loginResp = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        loginResp.EnsureSuccessStatusCode();

        // 2) Enable 2FA and capture secret from AuthenticatorUri
        var enableResp = await client.PostAsJsonAsync("/api/two-factor/enable", new { });
        enableResp.EnsureSuccessStatusCode();
        var enableDto = await enableResp.Content.ReadFromJsonAsync<EnableTwoFactorResponseDto>();
        enableDto.Should().NotBeNull();
        enableDto!.AuthenticatorUri.Should().StartWith("otpauth://totp/");

        // Parse secret= from otpauth uri
        var secret = ParseSecretFromOtpauthUri(enableDto.AuthenticatorUri);
        secret.Should().NotBeNullOrWhiteSpace();

        // 3) Generate a current TOTP for setup verification and verify
        var code = GenerateCurrentTotp(secret!);
        var verifySetupResp = await client.PostAsJsonAsync("/api/two-factor/verify-setup", new { code });
        verifySetupResp.EnsureSuccessStatusCode();
        var codesDto = await verifySetupResp.Content.ReadFromJsonAsync<RecoveryCodesResponseDto>();
        codesDto.Should().NotBeNull();
        codesDto!.RecoveryCodes.Should().NotBeNull();

        // 4) Logout
        var logoutResp = await client.PostAsync("/api/autenticacao/logout", content: null);
        logoutResp.EnsureSuccessStatusCode();

        // 5) Login again: should now require 2FA
        var login2 = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        login2.EnsureSuccessStatusCode();
        var loginPayload = JsonSerializer.Deserialize<LoginTwoFactorProbe>(await login2.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        loginPayload.Should().NotBeNull();
        loginPayload!.RequiresTwoFactor.Should().BeTrue();

        // 6) Provide authenticator code and set rememberMachine = true
        var code2 = GenerateCurrentTotp(secret!);
        var verify2fa = await client.PostAsJsonAsync("/api/autenticacao/verify-2fa", new { code = code2, rememberMachine = true, isRecoveryCode = false });
        verify2fa.EnsureSuccessStatusCode();

        // 7) Logout again
        var logout2 = await client.PostAsync("/api/autenticacao/logout", content: null);
        logout2.EnsureSuccessStatusCode();

        // 8) Login again: should NOT require 2FA due to remembered machine
        var login3 = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        login3.EnsureSuccessStatusCode();
        var text3 = await login3.Content.ReadAsStringAsync();
        text3.Should().NotContain("requiresTwoFactor");
    }

    [Fact]
    public async Task RecoveryCode_Login_Should_Succeed_Once_And_Then_Be_Invalid()
    {
        await ResetUserTwoFactorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Login and enable 2FA if not already
        var loginResp = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        loginResp.EnsureSuccessStatusCode();

        var enableResp = await client.PostAsJsonAsync("/api/two-factor/enable", new { });
        enableResp.EnsureSuccessStatusCode();
        var enableDto = await enableResp.Content.ReadFromJsonAsync<EnableTwoFactorResponseDto>();
        var secret = ParseSecretFromOtpauthUri(enableDto!.AuthenticatorUri);
        var code = GenerateCurrentTotp(secret!);
        var verifySetupResp = await client.PostAsJsonAsync("/api/two-factor/verify-setup", new { code });
        verifySetupResp.EnsureSuccessStatusCode();
        var codesDto = await verifySetupResp.Content.ReadFromJsonAsync<RecoveryCodesResponseDto>();
        codesDto!.RecoveryCodes.Should().NotBeNull();
        codesDto!.RecoveryCodes!.Count.Should().BeGreaterThan(0);
        var recoveryCode = codesDto.RecoveryCodes!.First();

        // Logout
        await client.PostAsync("/api/autenticacao/logout", content: null);

        // Login again requires 2FA
        var login2 = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        login2.EnsureSuccessStatusCode();

        // Use recovery code successfully
        var useRecovery = await client.PostAsJsonAsync("/api/autenticacao/verify-2fa", new { code = recoveryCode, rememberMachine = false, isRecoveryCode = true });
        useRecovery.EnsureSuccessStatusCode();

        // Logout and login again to attempt reuse (should fail)
        await client.PostAsync("/api/autenticacao/logout", content: null);
        var login3 = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        login3.EnsureSuccessStatusCode();
        var reuse = await client.PostAsJsonAsync("/api/autenticacao/verify-2fa", new { code = recoveryCode, rememberMachine = false, isRecoveryCode = true });
        reuse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable2FA_Should_Stop_Requiring_TwoFactor()
    {
        await ResetUserTwoFactorAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Ensure logged in and 2FA enabled
        await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        var enableResp = await client.PostAsJsonAsync("/api/two-factor/enable", new { });
        var enableDto = await enableResp.Content.ReadFromJsonAsync<EnableTwoFactorResponseDto>();
        var secret = ParseSecretFromOtpauthUri(enableDto!.AuthenticatorUri);
        var code = GenerateCurrentTotp(secret!);
        await client.PostAsJsonAsync("/api/two-factor/verify-setup", new { code });

        // Disable
        var disableResp = await client.PostAsJsonAsync("/api/two-factor/disable", new { });
        disableResp.EnsureSuccessStatusCode();

        // Logout and login should not require 2FA
        await client.PostAsync("/api/autenticacao/logout", content: null);
        var login2 = await client.PostAsJsonAsync("/api/autenticacao/login", new { email = "admin@erp.local", password = "Admin@123!", rememberMe = false });
        login2.EnsureSuccessStatusCode();
        var text = await login2.Content.ReadAsStringAsync();
        text.Should().NotContain("requiresTwoFactor");
    }

    private static string? ParseSecretFromOtpauthUri(string uri)
    {
        // uri format: otpauth://totp/Issuer:email?secret=XXXX&issuer=Issuer&digits=6
        var idx = uri.IndexOf('?');
        if (idx < 0) return null;
        var query = uri.Substring(idx + 1);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "secret")
                return kv[1];
        }
        return null;
    }

    private static string GenerateCurrentTotp(string base32Secret)
    {
        var key = Base32Encoding.ToBytes(base32Secret);
        var totp = new Totp(key, step: 30, totpSize: 6);
        return totp.ComputeTotp(DateTime.UtcNow);
    }

    private async Task ResetUserTwoFactorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("admin@erp.local");
        if (user != null)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);
        }
    }

    private class EnableTwoFactorResponseDto
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
    }

    private class RecoveryCodesResponseDto
    {
        public List<string>? RecoveryCodes { get; set; }
    }

    private class LoginTwoFactorProbe
    {
        public bool RequiresTwoFactor { get; set; }
        public string? Message { get; set; }
        public string? Email { get; set; }
    }
}
