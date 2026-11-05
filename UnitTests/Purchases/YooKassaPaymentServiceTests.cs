using System.Net;
using System.Text;
using System.Text.Json;
using Education.Service.Purchases.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Yandex.Checkout.V3;

namespace UnitTests.Purchases;

public class YooKassaPaymentServiceTests : IDisposable
{
    private readonly StubHandler _handler = new();
    private readonly HttpClient _http;
    private readonly YooKassaPaymentService _sut;
    private readonly CaptureLogger _logger = new();
    public YooKassaPaymentServiceTests()
    {
        _http = new HttpClient(_handler) { BaseAddress = new Uri("https://api.yookassa.test/v3/") };
        _sut = new YooKassaPaymentService(new AsyncClient(_http, false, new Client("shop", "key")), _logger);
    }
    public void Dispose() => _http.Dispose();
    private const string PaymentJson = "{\"id\":\"payment-id\",\"status\":\"succeeded\",\"paid\":true,\"amount\":{\"value\":\"125.50\",\"currency\":\"RUB\"},\"confirmation\":{\"type\":\"redirect\",\"confirmation_url\":\"https://pay.test/checkout\"}}";

    [Fact]
    public async Task Create_SendsAmountCurrencyAndRedirect_ReturnsGatewayIdentifiers()
    {
        _handler.Json = PaymentJson;
        var result = await _sut.CreatePaymentAsync(125.50m, "RUB", "Course", "https://front.test/return");
        result.StatusCode.Should().Be(StatusCode.OK);
        result.Data!.TransactionId.Should().Be("payment-id");
        result.Data.ConfirmationUrl.Should().Be("https://pay.test/checkout");
        _handler.Method.Should().Be(HttpMethod.Post);
        var body = JsonSerializer.Deserialize<JsonElement>(_handler.Body!);
        body.GetProperty("amount").GetProperty("value").GetDecimal().Should().Be(125.50m);
        body.GetProperty("amount").GetProperty("currency").GetString().Should().Be("RUB");
        body.GetProperty("confirmation").GetProperty("return_url").GetString().Should().Be("https://front.test/return");
        body.GetProperty("capture").GetBoolean().Should().BeTrue();
        body.GetProperty("description").GetString().Should().Be("Course");
    }

    [Theory]
    [InlineData("succeeded", true, StatusCode.OK)]
    [InlineData("succeeded", false, StatusCode.BadRequest)]
    [InlineData("pending", false, StatusCode.BadRequest)]
    [InlineData("canceled", false, StatusCode.BadRequest)]
    public async Task Confirm_RequiresSucceededAndPaid(string status, bool paid, StatusCode expected)
    {
        _handler.Json = JsonSerializer.Serialize(new { id = "payment-id", status, paid });
        (await _sut.ConfirmPaymentAsync("payment-id")).StatusCode.Should().Be(expected);
        _handler.Method.Should().Be(HttpMethod.Get);
        _handler.Path.Should().EndWith("payments/payment-id");
    }

    [Fact]
    public async Task Confirm_NullPayment_ReturnsNotFound()
    {
        _handler.Json = "null";
        (await _sut.ConfirmPaymentAsync("payment-id")).StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task Refund_UsesOriginalAmountAndPaymentId()
    {
        _handler.Responses.Enqueue(PaymentJson);
        _handler.Responses.Enqueue("{\"id\":\"refund-id\",\"status\":\"succeeded\",\"payment_id\":\"payment-id\",\"amount\":{\"value\":\"125.50\",\"currency\":\"RUB\"}}");
        (await _sut.RefundPaymentAsync("payment-id")).IsSuccess.Should().BeTrue(_logger.Error?.ToString());
        _handler.Count.Should().Be(2);
        _handler.Method.Should().Be(HttpMethod.Post);
        _handler.Path.Should().EndWith("refunds");
        var body = JsonSerializer.Deserialize<JsonElement>(_handler.Body!);
        body.GetProperty("payment_id").GetString().Should().Be("payment-id");
        body.GetProperty("amount").GetProperty("value").GetDecimal().Should().Be(125.50m);
    }

    [Fact]
    public async Task Refund_MissingPayment_DoesNotCreateRefund()
    {
        _handler.Json = "null";
        (await _sut.RefundPaymentAsync("payment-id")).StatusCode.Should().Be(StatusCode.NotFound);
        _handler.Count.Should().Be(1);
    }

    [Theory]
    [InlineData("create")] [InlineData("confirm")] [InlineData("refund")]
    public async Task GatewayException_BecomesInternalError(string operation)
    {
        _handler.Error = new HttpRequestException("offline");
        var status = operation switch
        {
            "create" => (await _sut.CreatePaymentAsync(1m, "RUB", "Course", "https://front.test")).StatusCode,
            "confirm" => (await _sut.ConfirmPaymentAsync("payment-id")).StatusCode,
            _ => (await _sut.RefundPaymentAsync("payment-id")).StatusCode,
        };
        status.Should().Be(StatusCode.InternalServerError);
    }

    [Fact]
    public async Task Fail_IsLocalAcknowledgementWithoutGatewayRequest()
    {
        (await _sut.FailPaymentAsync("payment-id", "cancelled")).IsSuccess.Should().BeTrue();
        _handler.Count.Should().Be(0);
    }

    private sealed class CaptureLogger : ILogger<YooKassaPaymentService>
    {
        public Exception? Error { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Error = exception;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public string Json { get; set; } = "{}";
        public Queue<string> Responses { get; } = new();
        public Exception? Error { get; set; }
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? Body { get; private set; }
        public int Count { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++; Method = request.Method; Path = request.RequestUri!.AbsolutePath;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Error != null) throw Error;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Responses.Count > 0 ? Responses.Dequeue() : Json, Encoding.UTF8, "application/json") };
        }
    }
}
