using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface ICoursePurchaseRepository : IBaseRepository<CoursePurchaseEntity>
    {
        /// <summary>
        /// Проверить, есть ли у пользователя успешная покупка курса.
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="courseId">Id курса</param>
        /// <returns>true - оплата по данному курсу уже была произведена, false - оплаты по данному курсу еще не было</returns>
        Task<bool> ExistsCompletedAsync(Guid userId,  Guid courseId);

        /// <summary>
        /// Получить покупку по ID транзакции (PaymentId из ЮKassa) с трекингом.
        /// AsNoTracking НЕ вызывается внутри метода (нужно для смены статуса).
        /// </summary>
        Task<CoursePurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId);
    }
}
