using Education.Domain.InternalServiceModels.Payments;
using Education.Domain.ResponseModel;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// Сервис для управления покупками курсов.
    /// </summary>
    public interface ICoursePurchaseService
    {
        /// <summary>
        /// Инициировать покупку курса (создать Pending-запись и получить ссылку на оплату).
        /// </summary>
        Task<InternalResponse<PaymentResult>> InitiatePurchaseAsync(Guid userId, Guid courseId, string returnUrl);

        /// <summary>
        /// Завершить покупку (вызывается после вебхука от ЮKassa).
        /// </summary>
        Task<InternalResponse<bool>> CompletePurchaseAsync(string paymentId, string transactionId);

        /// <summary>
        /// Отменить покупку (вызывается после вебхука об отмене или ошибке).
        /// </summary>
        Task<InternalResponse<bool>> FailPurchaseAsync(string paymentId, string? reason = null);

        /// <summary>
        /// Оформить возврат средств и забрать доступ.
        /// </summary>
        Task<InternalResponse<bool>> RefundPurchaseAsync(string paymentId);
    }
}
