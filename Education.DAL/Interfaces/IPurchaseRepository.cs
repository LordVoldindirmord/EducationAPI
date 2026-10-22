using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface IPurchaseRepository : IBaseRepository<PurchaseEntity>
    {
        /// <summary>
        /// Получить покупку (курса или роли) по ID транзакции с трекингом.
        /// AsNoTracking НЕ вызывается (нужно для смены статуса).
        /// </summary>
        Task<PurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId);

        /// <summary>
        /// Проверить, куплен ли конкретный курс пользователем.
        /// </summary>
        Task<bool> ExistsCompletedCourseAsync(Guid userId, Guid courseId);

        /// <summary>
        /// Проверить, куплена ли конкретная роль пользователем.
        /// </summary>
        Task<bool> ExistsCompletedRoleAsync(Guid userId, Guid roleId);
    }
}
