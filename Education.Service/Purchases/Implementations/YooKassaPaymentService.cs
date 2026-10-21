using Education.Domain.InternalServiceModels.Payments;
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

            // 1. Создаем клиент с тестовыми или боевыми ключами.
            // SDK поддерживает как синхронный Client, так и AsyncClient для async/await [citation:1][citation:2].
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
                // 2. Формируем запрос на создание платежа.
                // ConfirmationType.Redirect означает, что мы перенаправим пользователя на страницу ЮKassa [citation:1].
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

                // 3. Отправляем запрос. Idempotence-Key генерируется автоматически внутри SDK.
                var payment = await _client.CreatePaymentAsync(newPayment);

                _logger.LogInformation("Создан платеж ЮKassa. Id: {PaymentId}, Статус: {Status}",
                    payment.Id, payment.Status);

                // 4. Возвращаем результат для нашего сервиса покупок.
                var result = new PaymentResult
                {
                    PaymentId = payment.Id,
                    ConfirmationUrl = payment.Confirmation?.ConfirmationUrl
                };

                return CreatorResponse.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании платежа в ЮKassa на сумму {Amount} {Currency}", amount, currency);
                return CreatorResponse.InternalError<PaymentResult>("Не удалось создать платеж. Попробуйте позже.");
            }
        }

        public async Task<InternalResponse<bool>> ConfirmPaymentAsync(string paymentId)
        {
            try
            {
                // 5. Запрашиваем актуальную информацию о платеже.
                // Это важно, так как вебхук может быть поддельным. ЮKassa рекомендует перепроверять статус [citation:18].
                var payment = await _client.GetPaymentAsync(paymentId);

                if (payment == null)
                    return CreatorResponse.NotFound<bool>("Платеж не найден в системе ЮKassa.");

                // 6. Проверяем, что платеж действительно успешен.
                if (payment.Status == PaymentStatus.Succeeded && payment.Paid)
                {
                    _logger.LogInformation("Платеж {PaymentId} успешно подтвержден.", paymentId);
                    return CreatorResponse.Ok(true, "Платеж подтвержден.");
                }

                _logger.LogWarning("Платеж {PaymentId} не может быть подтвержден. Статус: {Status}, Paid: {Paid}",
                    paymentId, payment.Status, payment.Paid);

                return CreatorResponse.BadRequest<bool>($"Платеж не подтвержден. Текущий статус: {payment.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке статуса платежа {PaymentId}", paymentId);
                return CreatorResponse.InternalError<bool>("Не удалось проверить статус платежа.");
            }
        }

        public async Task<InternalResponse<bool>> FailPaymentAsync(string paymentId, string? reason = null)
        {
            // В реальной жизни "отклонить" платеж вручную через API нельзя.
            // Платеж сам переходит в статус canceled, если пользователь отказался или произошла ошибка.
            // Этот метод нужен скорее для внутренней логики (например, если мы получили вебхук payment.canceled).
            _logger.LogWarning("Платеж {PaymentId} помечен как неудачный. Причина: {Reason}", paymentId, reason);

            // Здесь можно просто вернуть Ok, так как фактическое состояние уже в ЮKassa.
            return CreatorResponse.Ok(true, "Платеж помечен как неудачный.");
        }

        public async Task<InternalResponse<bool>> RefundPaymentAsync(string paymentId)
        {
            try
            {
                // 7. Создаем возврат. SDK требует передать Id платежа и сумму.
                // Для полного возврата нужно знать исходную сумму. 
                // В реальном сценарии её можно получить из БД (CoursePurchase.PricePaid) или запросить платеж.
                var payment = await _client.GetPaymentAsync(paymentId);
                if (payment == null)
                    return CreatorResponse.NotFound<bool>("Платеж не найден.");

                var refundRequest = new NewRefund
                {
                    PaymentId = paymentId,
                    Amount = payment.Amount
                };

                var refund = await _client.CreateRefundAsync(refundRequest);

                _logger.LogInformation("Создан возврат {RefundId} по платежу {PaymentId}. Статус: {Status}",
                    refund.Id, paymentId, refund.Status);

                return CreatorResponse.Ok(true, "Возврат успешно инициирован.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании возврата по платежу {PaymentId}", paymentId);
                return CreatorResponse.InternalError<bool>("Не удалось создать возврат.");
            }
        }
    }

    /// <summary>
    /// Настройки для подключения к ЮKassa.
    /// </summary>
    public class YooKassaOptions
    {
        public string ShopId { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
    }
}
