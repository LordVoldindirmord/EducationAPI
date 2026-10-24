namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для генерации писем для отправки на Email
    /// </summary>
    public interface IEmailTemplateBuilder
    {
        /// <summary>
        /// Сгенерировать письмо для подтверждения Email
        /// </summary>
        /// <param name="firstName">Имя пользователя</param>
        /// <param name="link">Ссылка</param>
        /// <returns>Письмо для отправки на Email</returns>
        string BuildResetPasswordEmail(string firstName, string link);

        /// <summary>
        /// Сгенерировать письмо для сброса пароля
        /// </summary>
        /// <param name="firstName">Имя пользователя</param>
        /// <param name="link">Ссылка</param>
        /// <returns>Письмо для отправки на Email</returns>
        string BuildConfirmEmail(string firstName, string link);
    }
}