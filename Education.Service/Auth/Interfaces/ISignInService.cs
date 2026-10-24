using Education.Domain.Entity;

namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для работы с сессиями, JWT, Cookie
    /// </summary>
    public interface ISignInService
    {
        /// <summary>
        /// Установить cookie для пользователя (вход)
        /// </summary>
        Task SignInAsync(UserEntity user, bool isPersistent);

        /// <summary>
        /// Удалить cookie (выход)
        /// </summary>
        Task SignOutAsync();
    }
}