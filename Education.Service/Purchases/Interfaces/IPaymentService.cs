using Education.Domain.DTO.Response.Payment;
using Education.Domain.ResponseModel;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для работы с платежами
    /// <para>
    /// Отвечает <b>только</b> за взаимодействие с платёжной системой
    /// (создание платежа, подтверждение, отклонение, возврат).
    /// Не знает, <b>что именно</b> покупается.
    /// Это зона вызывающих сервисов.
    /// </para>
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Создать платёж на указанную сумму.
        /// <para>
        /// Возвращает результат с идентификатором платежа в платёжной системе
        /// и URL для редиректа пользователя на страницу оплаты.
        /// </para>
        /// </summary>
        /// <param name="amount">Сумма платежа</param>
        /// <param name="currency">Валюта (по умолчанию RUB)</param>
        /// <param name="description">Описание платежа</param>
        /// <param name="returnUrl">URL, куда вернуть пользователя после оплаты</param>
        /// <returns>
        /// <see cref="PaymentResult"/> с идентификатором платежа и URL для редиректа.
        /// </returns>
        Task<InternalResponse<PaymentResult>> CreatePaymentAsync(
            decimal amount,
            string currency,
            string description,
            string returnUrl);

        /// <summary>
        /// Подтвердить платёж — проверить статус в платёжной системе
        /// и зафиксировать успешную оплату.
        /// </summary>
        /// <param name="paymentId">Идентификатор платежа в платёжной системе</param>
        /// <returns>Флаг успешности подтверждения</returns>
        Task<InternalResponse<bool>> ConfirmPaymentAsync(string paymentId);

        /// <summary>
        /// Отметить платёж как неудачный
        /// </summary>
        /// <param name="paymentId">Идентификатор платежа в платёжной системе</param>
        /// <param name="reason">Причина отказа (опционально, для логирования)</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> FailPaymentAsync(string paymentId, string? reason = null);

        /// <summary>
        /// Оформить возврат средств
        /// </summary>
        /// <param name="paymentId">Идентификатор платежа в платёжной системе</param>
        /// <returns>Флаг успешности возврата</returns>
        Task<InternalResponse<bool>> RefundPaymentAsync(string paymentId);
    }
}
