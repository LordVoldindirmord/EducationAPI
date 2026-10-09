using Education.Service.Auth.Interfaces;

namespace Education.Service.Auth.Implementations
{
    public class EmailTemplateBuilder : IEmailTemplateBuilder
    {
        public string BuildConfirmEmail(string firstName, string link)
        {
            return $@"
<h2>Подтверждение Email</h2>
<p>Здравствуйте, {firstName}!</p>
<p>Для подтверждения Email перейдите по ссылке:</p>
<p><a href='{link}'>{link}</a></p>
<p>Если вы не регистрировались, просто проигнорируйте это письмо.</p>";
        }

        public string BuildResetPasswordEmail(string firstName, string link)
        {
            return $@"
<h2>Сброс пароля</h2>
<p>Здравствуйте, {firstName}!</p>
<p>Для сброса пароля перейдите по ссылке:</p>
<p><a href='{link}'>{link}</a></p>
<p>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>";
        }
    }
}
