using Education.Domain.DTO.Response.Payment;
using Education.Domain.ResponseModel;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// Единый сервис для управления покупками
    /// <para>
    /// Это не паттерн стратегия, здесь происходит простое делегирование
    /// </para>
    /// </summary>
    public interface IPurchaseService
    {
        // Инициализация для каждой модели будет своя, остальное - общее

        /// <summary>
        /// Инициировать покупку курса (создать Pending-запись и получить ссылку на оплату)
        /// </summary>
        /// <param name="userId">Id пользователя, который будет производить оплату</param>
        /// <param name="courseId">Id курса, который хочет купить пользователь</param>
        /// <param name="returnUrl">URL, куда вернуть пользователя после оплаты</param>
        /// <returns>DTO оплаты</returns>
        Task<InternalResponse<PaymentResult>> InitiateCoursePurchaseAsync(Guid userId, Guid courseId, string returnUrl);

        /// <summary>
        /// Инициировать покупку роли
        /// </summary>
        /// <param name="userId">Id пользователя, который будет производить оплату</param>
        /// <param name="roleName">Роль, которую хочет купить пользователь</param>
        /// <param name="returnUrl">URL, куда вернуть пользователя после оплаты</param>
        /// <returns></returns>
        Task<InternalResponse<PaymentResult>> InitiateRolePurchaseAsync(Guid userId, string roleName, string returnUrl);

        /// <summary>
        /// Завершить покупку
        /// </summary>
        /// <param name="transactionId">Id транзации</param>
        /// <returns>Флаг успешности операции, true - покупка завершена успешно, false - с ошибкой</returns>
        Task<InternalResponse<bool>> CompletePurchaseAsync(string transactionId);

        /// <summary>
        /// Отменить покупку (вызывается после вебхука об отмене или ошибке)
        /// </summary>
        /// <param name="transactionId">Id транзации</param>
        /// <param name="reason">Причина отмены (для logger)</param>
        /// <returns>Фгал успешности операции, true - покупка успешно отменена, false - ошибка</returns>
        Task<InternalResponse<bool>> FailPurchaseAsync(string transactionId, string? reason = null);

        /// <summary>
        /// Оформить возврат средств и забрать доступ
        /// </summary>
        /// <param name="transactionId">Id транзакции</param>
        /// <returns>Флаг успешности операции, true - возврат оформлен успешно, false - нет</returns>
        Task<InternalResponse<bool>> RefundPurchaseAsync(string transactionId);
    }
}