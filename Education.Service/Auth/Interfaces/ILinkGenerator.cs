namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для генерации ссылок
    /// </summary>
    public interface ILinkGenerator
    {
        /// <summary>
        /// Получить ссылку для сброса пароля
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="token">Токен</param>
        /// <returns>Ссылка для сброса пароля</returns>
        string GetResetPasswordLink(Guid userId, string token);

        /// <summary>
        /// Получить ссылку для подтверждения Email
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="token">Токен</param>
        /// <returns>Ссылка для подтверждения Email</returns>
        string GetConfirmEmailLink(Guid userId, string token);
    }
}
