using Education.Domain.InternalServiceModels.Payments;
using Education.Domain.ResponseModel;
using Education.Domain.Entity;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// Сервис для управления покупками глобальных ролей.
    /// <para>
    /// Жизненный цикл покупки:
    /// <list type="number">
    ///     <item><see cref="InitiatePurchaseAsync"/> — создать <c>Pending</c>-покупку и получить ссылку на оплату.</item>
    ///     <item><see cref="CompletePurchaseAsync"/> — вебхук подтвердил оплату → <c>Completed</c> + выдать роль.</item>
    ///     <item><see cref="FailPurchaseAsync"/> — вебхук отклонил → <c>Failed</c>.</item>
    ///     <item><see cref="RefundPurchaseAsync"/> — возврат → <c>Refunded</c> + забрать роль.</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface IRolePurchaseService
    {
        /// <summary>
        /// Инициировать покупку роли.
        /// <para>
        /// Проверяет: роль существует, платная, ещё не куплена.
        /// Создает <c>Pending</c>-запись в БД и вызывает платёжный шлюз.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя-покупателя</param>
        /// <param name="roleId">Id роли (<see cref="AppRoleEntity.Id"/>)</param>
        /// <param name="returnUrl">URL, на который ЮKassa вернёт пользователя после оплаты</param>
        /// <returns>
        /// <see cref="PaymentResult"/> с <c>PaymentId</c> (ID транзакции в ЮKassa) 
        /// и <c>ConfirmationUrl</c> (ссылка для редиректа пользователя).
        /// </returns>
        Task<InternalResponse<PaymentResult>> InitiatePurchaseAsync(Guid userId, Guid roleId, string returnUrl);

        /// <summary>
        /// Завершить покупку роли.
        /// <para>
        /// Вызывается после получения вебхука <c>payment.succeeded</c> от платёжной системы.
        /// Меняет статус на <c>Completed</c> и выдает роль пользователю через <c>UserManager</c>.
        /// </para>
        /// </summary>
        /// <param name="paymentId">ID платежа в платёжной системе (ЮKassa). Используется для поиска покупки в БД.</param>
        /// <param name="transactionId">ID транзакции для сохранения в БД (обычно совпадает с <paramref name="paymentId"/>, но может отличаться в других шлюзах).</param>
        Task<InternalResponse<bool>> CompletePurchaseAsync(string paymentId, string transactionId);

        /// <summary>
        /// Отменить покупку роли.
        /// <para>
        /// Вызывается после вебхука <c>payment.canceled</c> или при ошибке оплаты.
        /// </para>
        /// </summary>
        /// <param name="paymentId">ID платежа в платёжной системе</param>
        /// <param name="reason">Причина отмены (для логов)</param>
        Task<InternalResponse<bool>> FailPurchaseAsync(string paymentId, string? reason = null);

        /// <summary>
        /// Оформить возврат средств и забрать роль.
        /// <para>
        /// Вызывается при возврате денег (вручную или по API).
        /// </para>
        /// </summary>
        /// <param name="paymentId">ID платежа в платёжной системе</param>
        Task<InternalResponse<bool>> RefundPurchaseAsync(string paymentId);
    }
}
