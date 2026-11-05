using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using Education.Service.Auth.Interfaces;

namespace Education.Service.Auth.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly Func<ISmtpClient> _createClient;

        /// <summary>
        /// Конструктор только для Unit тестов
        /// </summary>
        public EmailService(IConfiguration configuration) : this(configuration, () => new SmtpClient()) { }

        public EmailService(IConfiguration configuration, Func<ISmtpClient> createClient)
        {
            _configuration = configuration;
            _createClient = createClient;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"]
                ?? throw new InvalidOperationException("SMTP Host не настроен");

            var port = int.Parse(_configuration["Smtp:Port"]
                ?? throw new InvalidOperationException("SMTP Port не настроен"));

            var username = _configuration["Smtp:Username"]
                ?? throw new InvalidOperationException("SMTP Username не настроен");

            var password = _configuration["Smtp:Password"]
                ?? throw new InvalidOperationException("SMTP Password не настроен");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Онлайн платформа обучения", username));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = body
            };

            using var client = _createClient();

            // Потом удалить, это отключение проверки SSL сертификата
            client.CheckCertificateRevocation = false;

            try
            {
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(email);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
