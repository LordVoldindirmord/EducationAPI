using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Helpers;

namespace IntegrationTests.Tests.Http;

[Collection(AuthIntegrationCollection.Name)]
public class PaymentEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    private async Task<Guid> PublishedCourseAsync()
    {
        await LoginAsync("admin");
        var courseId = await CreateCourseAsync(await CreateCategoryAsync());
        await SendAsync(HttpMethod.Put, "/api/course/publish", new { CourseId = courseId, IsPublished = true });
        await SendAsync(HttpMethod.Post, "/api/auth/logout", null);
        await LoginAsync();
        return courseId;
    }

    [Fact]
    public async Task BuyCourse_BindsJson_ReturnsPaymentResponse()
    {
        var courseId = await PublishedCourseAsync();
        var json = await SendAsync(HttpMethod.Post, "/api/payment/course", new { CourseId = courseId, ReturnUrl = "https://frontend.test/return" });
        json.GetProperty("data").GetProperty("transactionId").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("data").GetProperty("confirmationUrl").GetString().Should().Be("https://payments.test/checkout");
    }

    [Fact]
    public async Task BuyRole_BindsJson_ReturnsPaymentResponse()
    {
        await LoginAsync();
        var json = await SendAsync(HttpMethod.Post, "/api/payment/role", new { RoleName = "course_creator", ReturnUrl = "https://frontend.test/return" });
        json.GetProperty("data").GetProperty("transactionId").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("data").GetProperty("confirmationUrl").GetString().Should().Be("https://payments.test/checkout");
    }

    [Fact]
    public async Task Refund_BindsJson_Returns200()
    {
        var courseId = await PublishedCourseAsync();
        var payment = await SendAsync(HttpMethod.Post, "/api/payment/course", new { CourseId = courseId, ReturnUrl = "https://frontend.test/return" });
        var transactionId = payment.GetProperty("data").GetProperty("transactionId").GetString();
        using var webhook = await Client.PostAsJsonAsync("/api/yookassa/webhook", new { @event = "payment.succeeded", @object = new { id = transactionId } });
        webhook.StatusCode.Should().Be(HttpStatusCode.OK);
        await SendAsync(HttpMethod.Post, "/api/payment/refund", new { TransactionId = transactionId });
    }

    [Theory]
    [InlineData("payment.succeeded")]
    [InlineData("payment.canceled")]
    [InlineData("refund.succeeded")]
    [InlineData("unknown.event")]
    public async Task Webhook_AcceptsProviderEnvelope_WithoutAuthentication(string eventName)
    {
        using var response = await Client.PostAsJsonAsync("/api/yookassa/webhook", new { @event = eventName, @object = new { id = "unknown-payment" } });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }
}
