using Education.Domain.Entity;

namespace Education.DAL.Interfaces
{
    public interface IRolePurchaseRepository : IBaseRepository<RolePurchaseEntity>
    {
        /// <summary>
        /// Проверить, есть ли у пользователя успешная покупка указанной роли.
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="roleId">Id роли</param>
        /// <returns>true - роль уже куплена и оплачена, false - покупки еще не было</returns>
        Task<bool> ExistsCompletedAsync(Guid userId, Guid roleId);

        /// <summary>
        /// Получить покупку роли по ID транзакции (PaymentId из ЮKassa) с трекингом.
        /// AsNoTracking НЕ вызывается внутри метода (нужно для смены статуса).
        /// </summary>
        /// <param name="transactionId">ID транзакции из платежной системы</param>
        /// <returns>Сущность покупки роли или null, если не найдена</returns>
        Task<RolePurchaseEntity?> GetByTransactionIdTrackedAsync(string transactionId);
    }
}
