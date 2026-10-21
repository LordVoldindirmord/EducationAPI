using System;
using System.Collections.Generic;
using System.Text;

namespace Education.Domain.InternalServiceModels.Payments
{
    /// <summary>
    /// Результат создания платежа в платёжной системе.
    /// <para>
    /// <b>Внутренняя модель.</b> Не уходит клиенту напрямую.
    /// Используется для обмена между <c>IPaymentService</c> и сервисами покупок
    /// (<c>CoursePurchaseService</c>, <c>RolePurchaseService</c>).
    /// </para>
    /// </summary>
    public record PaymentResult
    {
        /// <summary>
        /// Идентификатор платежа в платёжной системе.
        /// Сохраняется в <c>TransactionId</c> покупки.
        /// </summary>
        public required string PaymentId { get; init; }

        /// <summary>
        /// URL для редиректа пользователя на страницу оплаты.
        /// Может быть <c>null</c>, если платёжная система не требует редиректа
        /// (например, для тестовой заглушки).
        /// </summary>
        public string? ConfirmationUrl { get; init; }
    }
}
