using Education.Domain.Enums;

namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// Сервис для получения данных текущего пользователя из ClaimsPrincipal
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Id текущего пользователя (из ClaimTypes.NameIdentifier)
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Логин текущего пользователя (из ClaimTypes.Name)
        /// </summary>
        string? UserName { get; }

        /// <summary>
        /// Email текущего пользователя (из ClaimTypes.Email)
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Роль текущего пользователя (из ClaimTypes.Role)
        /// </summary>
        UserRole? Role { get; }

        /// <summary>
        /// Авторизован ли пользователь
        /// </summary>
        bool IsAuthenticated { get; }
    }
}