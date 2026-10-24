namespace Education.Domain.DTO.Response.Payment
{
    /// <summary>
    /// Результат создания платежа в платёжной системе.
    /// </summary>
    public record PaymentResult
    {
        /// <summary>
        /// Идентификатор платежа в платёжной системе.
        /// Сохраняется в покупки.
        /// </summary>
        public required string TransactionId { get; init; }

        /// <summary>
        /// URL для редиректа пользователя на страницу оплаты.
        /// Может быть <c>null</c>, если платёжная система не требует редиректа
        /// (например, для тестовой заглушки).
        /// </summary>
        public string? ConfirmationUrl { get; init; }
    }
}