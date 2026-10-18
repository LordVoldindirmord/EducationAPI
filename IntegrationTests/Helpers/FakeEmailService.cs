using Education.Service.Auth.Interfaces;

namespace IntegrationTests.Helpers
{
    public sealed class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sent = [];

        public bool ThrowOnSend { get; set; }

        public IReadOnlyList<SentEmail> Sent => _sent;

        public SentEmail Last => _sent.Last();

        public Task SendEmailAsync(string to, string subject, string body)
        {
            if (ThrowOnSend)
                throw new InvalidOperationException("SMTP send failed");

            _sent.Add(new SentEmail(to, subject, body));
            return Task.CompletedTask;
        }

        public sealed record SentEmail(string To, string Subject, string Body);
    }
}
