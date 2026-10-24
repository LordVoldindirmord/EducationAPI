using Education.Domain.DTO.Response.Payment;
using Education.Domain.InternalServiceModels.Purchases;
using Education.Domain.ResponseModel;
using Education.Service.Purchases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yandex.Checkout.V3;

namespace Education.Service.Purchases.Implementations
{
    /// <summary>
    /// Реализация <see cref="IPaymentService"/> через SDK Yandex.Checkout.V3 (ЮKassa).
    /// </summary>
    public class YooKassaPaymentService : IPaymentService
    {
        private readonly AsyncClient _client;
        private readonly ILogger<YooKassaPaymentService> _logger;

        public YooKassaPaymentService(
            IOptions<YooKassaOptions> options,
            ILogger<YooKassaPaymentService> logger)
        {
            _logger = logger;

            // Создаем клиент с ключами
            var client = new Client(
                shopId: options.Value.ShopId,
                secretKey: options.Value.SecretKey);

            _client = client.MakeAsync();
        }

        public async Task<InternalResponse<PaymentResult>> CreatePaymentAsync(
            decimal amount,
            string currency,
            string description,
            string returnUrl)
        {
            try
            {
                // Формируем запрос на создание платежа
                // ConfirmationType.Redirect - перенаправим пользователя на страницу ЮKassa
                var newPayment = new NewPayment
                {
                    Amount = new Amount { Value = amount, Currency = currency },
                    Confirmation = new Confirmation
                    {
                        Type = ConfirmationType.Redirect,
                        ReturnUrl = returnUrl
                    },
                    Description = description,
                    Capture = true // Автоматическое списание после оплаты (одностадийный платеж)
                };

                // Отправляем запрос. Idempotence-Key генерируется автоматически внутри SDK
                var payment = await _client.CreatePaymentAsync(newPayment);

                _logger.LogInformation($"Создан платеж ЮKassa. Id: {payment.Id}, Статус: {payment.Status}");

                // Возвращаем результат для нашего сервиса покупок
                var result = new PaymentResult
                {
                    TransactionId = payment.Id,
                    ConfirmationUrl = payment.Confirmation?.ConfirmationUrl
                };

                return CreatorResponse.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании платежа в ЮKassa на сумму {Amount} {Currency}", amount, currency);

                return CreatorResponse.InternalError<PaymentResult>("Не удалось создать платеж. Попробуйте позже");
            }
        }

        public async Task<InternalResponse<bool>> ConfirmPaymentAsync(string paymentId)
        {
            try
            {
                // Запрашиваем актуальную информацию о платеже
                // Это важно, так как вебхук может быть поддельным
                var payment = await _client.GetPaymentAsync(paymentId);

                if (payment == null)
                    return CreatorResponse.NotFound<bool>("Платеж не найден в системе ЮKassa");

                // Проверяем, что платеж действительно успешен.
                if (payment.Status == PaymentStatus.Succeeded && payment.Paid)
                {
                    _logger.LogInformation($"Платеж {paymentId} успешно подтвержден");

                    return CreatorResponse.Ok(true, "Платеж подтвержден");
                }

                _logger.LogWarning(
                    $"Платеж {paymentId} не может быть подтвержден. " +
                    $"Статус: {payment.Status}, Paid: {payment.Paid}"
                );

                return CreatorResponse.BadRequest<bool>($"Платеж не подтвержден. Текущий статус: {payment.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке статуса платежа {paymentId}");

                return CreatorResponse.InternalError<bool>("Не удалось проверить статус платежа");
            }
        }

        public async Task<InternalResponse<bool>> FailPaymentAsync(string paymentId, string? reason = null)
        {
            _logger.LogWarning($"Платеж {paymentId} помечен как неудачный. Причина: {reason}");

            // Здесь можно просто вернуть Ok, так как фактическое состояние уже в ЮKassa
            return CreatorResponse.Ok(true, "Платеж помечен как неудачный");
        }

        public async Task<InternalResponse<bool>> RefundPaymentAsync(string paymentId)
        {
            try
            {
                // Создаем возврат. SDK требует передать Id платежа и сумму.
                // Для полного возврата нужно знать исходную сумму
                Payment payment = await _client.GetPaymentAsync(paymentId);

                if (payment == null)
                    return CreatorResponse.NotFound<bool>("Платеж не найден");

                var refundRequest = new NewRefund
                {
                    PaymentId = paymentId,
                    Amount = payment.Amount
                };

                var refund = await _client.CreateRefundAsync(refundRequest);

                _logger.LogInformation($"Создан возврат {refund.Id} по платежу {paymentId}. Статус: {refund.Status}");

                return CreatorResponse.Ok(true, "Возврат успешно инициирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при создании возврата по платежу {paymentId}");

                return CreatorResponse.InternalError<bool>("Не удалось создать возврат");
            }
        }
    }
}