using Education.Domain.DTO.Response.Payment;
using Education.Domain.ResponseModel;
using Education.Service.Purchases.Interfaces;

namespace IntegrationTests.Helpers;

// Only the external gateway is substituted; purchase and access logic stays real.
public sealed class FakePaymentService : IPaymentService
{
    public bool RejectConfirmation { get; set; }
    public List<(decimal Amount, string Currency, string ReturnUrl)> Created { get; } = [];
    public List<string> Refunded { get; } = [];
    public Task<InternalResponse<PaymentResult>> CreatePaymentAsync(decimal amount, string currency, string description, string returnUrl)
    {
        Created.Add((amount, currency, returnUrl));
        return Task.FromResult(CreatorResponse.Ok(new PaymentResult
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            ConfirmationUrl = "https://payments.test/checkout",
        }));
    }
    public Task<InternalResponse<bool>> ConfirmPaymentAsync(string paymentId) => Task.FromResult(
        RejectConfirmation ? CreatorResponse.InternalError<bool>("Gateway unavailable") : CreatorResponse.Ok(true));
    public Task<InternalResponse<bool>> FailPaymentAsync(string paymentId, string? reason = null) => Task.FromResult(CreatorResponse.Ok(true));
    public Task<InternalResponse<bool>> RefundPaymentAsync(string paymentId)
    {
        Refunded.Add(paymentId);
        return Task.FromResult(CreatorResponse.Ok(true));
    }
}
