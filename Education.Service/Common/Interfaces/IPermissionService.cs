using Education.Domain.Enums;
using Education.Domain.ResponseModel;

namespace Education.Service.Common.Interfaces
{
    /// <summary>
    /// Сервис для проверки ролей и прав пользователя
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Получить актуальную роль пользователя из БД.
        /// Если роль не найдена, вернется null
        /// </summary>
        Task<UserRole?> GetUserRoleAsync(Guid userId);

        /// <summary>
        /// Проверить, что пользователь имеет указанную роль
        /// </summary>
        Task<bool> HasRoleAsync(Guid userId, UserRole role);

        /// <summary>
        /// Проверить, что пользователь не заблокирован
        /// </summary>
        Task<bool> IsActiveAsync(Guid userId);

        /// <summary>
        /// Проверить, есть ли у пользователя подходящая роль
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="roles">Список подходящих ролей</param>
        /// <returns>true - роль у пользователя есть, false - нет</returns>
        Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<UserRole> roles);
    }
}
