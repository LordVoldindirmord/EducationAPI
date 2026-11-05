using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Helpers;

namespace IntegrationTests.Tests.Auth;

[Collection(AuthIntegrationCollection.Name)]
public class AuthEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    [Fact]
    public async Task Register_AcceptsJson_ReturnsApiResponseAndConfirmationLink()
    {
        var user = await RegisterAsync(confirm: false);
        App.Email.Last.To.Should().Be(user.Email);
        EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "token").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmEmail_AcceptsLinkParameters_ReturnsLoginResponseWithStringRole()
    {
        var user = await RegisterAsync(confirm: false);
        var result = await SendAsync(HttpMethod.Post, "/api/auth/confirm-email", new
        {
            UserId = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "userId"),
            Token = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "token"),
        });
        var data = result.GetProperty("data");
        data.GetProperty("email").GetString().Should().Be(user.Email);
        data.GetProperty("login").GetString().Should().Be(user.Login);
        data.GetProperty("role").GetString().Should().Be("user");
        data.GetProperty("userId").GetGuid().Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_IssuesCookie_WithRequestedPersistence(bool rememberMe)
    {
        var user = await RegisterAsync();
        using var response = await Client.PostAsJsonAsync("/api/auth/login", new
        { LoginOrEmail = user.Email, user.Password, RememberMe = rememberMe });
        var json = await AssertApiAsync(response, HttpStatusCode.OK);
        json.GetProperty("data").GetProperty("email").GetString().Should().Be(user.Email);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("EducationPlatform="));
        cookie.Should().Contain("httponly");
        cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase).Should().Be(rememberMe);
        using var protectedResponse = await Client.PostAsJsonAsync("/api/payment/refund", new { TransactionId = "missing" });
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "the cookie must authenticate the next request");
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401ApiResponse() =>
        await SendAsync(HttpMethod.Post, "/api/auth/login", new { LoginOrEmail = "missing", Password = "Password1!" }, HttpStatusCode.Unauthorized);

    [Fact]
    public async Task ResendConfirmation_AcceptsEmail_Returns200()
    {
        var user = await RegisterAsync(confirm: false);
        await SendAsync(HttpMethod.Post, "/api/auth/resend-confirmation", new { user.Email });
        App.Email.Sent.Should().HaveCount(2);
        App.Email.Last.To.Should().Be(user.Email);
    }

    [Fact]
    public async Task ForgotPassword_AcceptsEmail_ReturnsResetLink()
    {
        var user = await RegisterAsync();
        await SendAsync(HttpMethod.Post, "/api/auth/forgot-password", new { user.Email });
        App.Email.Last.Subject.Should().Be("Сброс пароля");
        EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "token").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetPassword_AcceptsJson_Returns200()
    {
        var user = await RegisterAsync();
        await SendAsync(HttpMethod.Post, "/api/auth/forgot-password", new { user.Email });
        await SendAsync(HttpMethod.Post, "/api/auth/reset-password", new
        {
            UserId = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "userId"),
            Token = EmailLinkParser.ExtractQueryParam(App.Email.Last.Body, "token"),
            NewPassword = "NewPassword1!", ConfirmNewPassword = "NewPassword1!",
        });
    }

    [Fact]
    public async Task Logout_ExpiresCookie_AndNextProtectedRequestReturns401()
    {
        await LoginAsync();
        using var response = await Client.PostAsync("/api/auth/logout", null);
        await AssertApiAsync(response, HttpStatusCode.OK);
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("EducationPlatform=;") && c.Contains("expires="));
        using var protectedResponse = await Client.PostAsJsonAsync("/api/payment/refund", new { TransactionId = "missing" });
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        protectedResponse.Headers.Location.Should().BeNull();
    }
}
