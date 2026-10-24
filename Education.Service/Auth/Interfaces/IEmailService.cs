namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// <b>ВНУТРЕННИЙ</b> сервис для отправки Email писем
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Отправить email-письмо
        /// </summary>
        /// <param name="to">Кому</param>
        /// <param name="subject">Тема письма</param>
        /// <param name="body">HTML-содержимое письма</param>
        /// <exception cref="InvalidOperationException">Ошибка при считывании данных из JSON для настройки SMTP</exception>
        Task SendEmailAsync(string to, string subject, string body);
    }
}