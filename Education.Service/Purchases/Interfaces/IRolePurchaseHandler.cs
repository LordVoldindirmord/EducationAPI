using Education.Domain.Entity;
using Education.Domain.ResponseModel;

namespace Education.Service.Purchases.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для обработки покупок роли
    /// <para>
    /// Отвечает за валидацию перед покупкой и за выдачу/отзыв глобальной роли пользователю
    /// </para>
    /// </summary>
    public interface IRolePurchaseHandler
    {
        /// <summary>
        /// Получить роль для ее покупки. Если купить роль нельзя, то вернется InternalResponse с ошибкой
        /// <para>
        /// Если где-то по ходу с ролью была ошибка, то вернется в InternalResponse, что именно пошло не так
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="roleName">Название роли, которую будет покупать пользователь</param>
        /// <returns>Объект AppRoleEntity из БД или сообщение об ошибке</returns>
        Task<InternalResponse<AppRoleEntity>> GetRoleForPurchaseAsync(Guid userId, string roleName);

        /// <summary>
        /// Подтвердить покупку в БД
        /// </summary>
        /// <param name="purchase">Сущность покупки</param>
        /// <returns>Флаг успешности операции, true - все хорошо, false - что-то пошло не так</returns>
        Task<InternalResponse<bool>> CompleteRolePurchaseAsync(PurchaseEntity purchase);

        /// <summary>
        /// Отозвать роль пользователя
        /// </summary>
        /// <param name="purchase">Сущность покупки</param>
        /// <returns>Флаг успешности операции, true - все хорошо, false - что-то пошло не так</returns>
        Task<InternalResponse<bool>> RevokeRoleAccessAsync(PurchaseEntity purchase);
    }
}