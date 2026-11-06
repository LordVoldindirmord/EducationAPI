using Education.Service.Auth.Implementations;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace UnitTests.Auth;

public class EmailServiceTests
{
    private readonly Mock<ISmtpClient> _client = new();
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Smtp:Host"] = "smtp.test", ["Smtp:Port"] = "587", ["Smtp:Username"] = "sender@test.com", ["Smtp:Password"] = "secret",
    };
    private EmailService Sut => new(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build(), () => _client.Object);

    public EmailServiceTests()
    {
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).ReturnsAsync("sent");
    }

    [Fact]
    public async Task Send_ConstructsHtmlMessageAndUsesStartTlsThenDisconnects()
    {
        MimeMessage? sent = null;
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress>((message, _, _) => sent = message).ReturnsAsync("sent");
        await Sut.SendEmailAsync("recipient@test.com", "Subject", "<p>Body</p>");
        sent.Should().NotBeNull();
        sent!.From.Mailboxes.Single().Address.Should().Be("sender@test.com");
        sent.To.Mailboxes.Single().Address.Should().Be("recipient@test.com");
        sent.Subject.Should().Be("Subject");
        sent.HtmlBody.Should().Be("<p>Body</p>");
        _client.Verify(c => c.ConnectAsync("smtp.test", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.AuthenticateAsync("sender@test.com", "secret", It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData("Smtp:Host")] [InlineData("Smtp:Port")] [InlineData("Smtp:Username")] [InlineData("Smtp:Password")]
    public async Task MissingConfiguration_FailsBeforeConnecting(string key)
    {
        _settings.Remove(key);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut.SendEmailAsync("to@test.com", "Subject", "Body"));
        _client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("connect")] [InlineData("authenticate")] [InlineData("send")]
    public async Task TransportFailure_IsPropagatedAndClientDisconnected(string stage)
    {
        var error = new IOException(stage);
        if (stage == "connect") _client.Setup(c => c.ConnectAsync("smtp.test", 587, SecureSocketOptions.StartTls, It.IsAny<CancellationToken>())).ThrowsAsync(error);
        if (stage == "authenticate") _client.Setup(c => c.AuthenticateAsync("sender@test.com", "secret", It.IsAny<CancellationToken>())).ThrowsAsync(error);
        if (stage == "send") _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>())).ThrowsAsync(error);
        (await Assert.ThrowsAsync<IOException>(() => Sut.SendEmailAsync("to@test.com", "Subject", "Body"))).Should().BeSameAs(error);
        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.Dispose(), Times.Once);
    }
}
