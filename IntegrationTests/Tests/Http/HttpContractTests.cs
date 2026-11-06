using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using EducationAPI.Controllers.Web;
using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace IntegrationTests.Tests.Http;

[Collection(AuthIntegrationCollection.Name)]
public class HttpContractTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    // One contract case per action. Also used to catch newly added, uncovered endpoints.
    public static TheoryData<string, string> BodyEndpoints => new()
    {
        { "POST", "/api/auth/register" }, { "POST", "/api/auth/login" },
        { "POST", "/api/auth/confirm-email" }, { "POST", "/api/auth/forgot-password" },
        { "POST", "/api/auth/reset-password" }, { "POST", "/api/auth/resend-confirmation" },
        { "POST", "/api/category/create" }, { "PUT", "/api/category/update" }, { "DELETE", "/api/category/delete" },
        { "POST", "/api/course/create" }, { "PUT", "/api/course/update" },
        { "DELETE", "/api/course/delete" }, { "PUT", "/api/course/publish" },
        { "POST", "/api/payment/course" }, { "POST", "/api/payment/role" }, { "POST", "/api/payment/refund" },
        { "POST", "/api/yookassa/webhook" },
    };

    public static TheoryData<string, string> RoleEndpoints
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (var row in ProtectedEndpoints.Where(row => !((string)row[1]).StartsWith("/api/payment/")))
                cases.Add((string)row[0], (string)row[1]);
            return cases;
        }
    }

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "POST", "/api/category/create" }, { "PUT", "/api/category/update" }, { "DELETE", "/api/category/delete" },
        { "POST", "/api/course/create" }, { "PUT", "/api/course/update" },
        { "DELETE", "/api/course/delete" }, { "PUT", "/api/course/publish" },
        { "POST", "/api/payment/course" }, { "POST", "/api/payment/role" }, { "POST", "/api/payment/refund" },
    };

    [Fact]
    public void EndpointInventory_CoversEveryControllerAction()
    {
        var actual = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods().SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>()
                .SelectMany(a => a.HttpMethods.Select(verb => $"{verb} /{t.GetCustomAttribute<RouteAttribute>()!.Template}/{a.Template}"))))
            .ToArray();
        var expected = BodyEndpoints.Select(row => $"{row[0]} {row[1]}").Append("POST /api/auth/logout");
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(BodyEndpoints))]
    public async Task RequiredBody_IsValidatedByHttpPipeline(string method, string route)
    {
        await LoginAsync("admin");
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (route is "/api/course/create" or "/api/course/update")
            request.Content = new MultipartFormDataContent();
        else request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.SendAsync(request);
        await AssertApiAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_Anonymous_Returns401WithoutRedirect(string method, string route)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        using var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(RoleEndpoints))]
    public async Task ProtectedEndpoint_OrdinaryUser_EnforcesRolePolicy(string method, string route)
    {
        await LoginAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        using var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.Location.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(BodyEndpoints))]
    [InlineData("POST", "/api/auth/logout")]
    public async Task Route_RejectsUnsupportedGetMethod(string method, string route)
    {
        using var response = await Client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().Contain(method);
    }

    [Fact]
    public async Task JsonEndpoint_RejectsTextPlain()
    {
        using var response = await Client.PostAsync("/api/auth/login", new StringContent("{}"));
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task JsonEndpoint_MalformedJson_Returns400ApiResponse()
    {
        using var body = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/auth/login", body);
        await AssertApiAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidModel_Returns400ApiResponse() =>
        await SendAsync(HttpMethod.Post, "/api/auth/register", new { Email = "invalid" }, HttpStatusCode.BadRequest);
}
