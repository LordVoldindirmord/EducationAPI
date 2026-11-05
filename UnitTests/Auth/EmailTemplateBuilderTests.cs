using Education.Service.Auth.Implementations;

namespace UnitTests.Auth;

public class EmailTemplateBuilderTests
{
    [Theory]
    [InlineData(false, "Подтверждение Email")]
    [InlineData(true, "Сброс пароля")]
    public void Template_IncludesCorrectPurposeGreetingAndActionLink(bool reset, string heading)
    {
        var sut = new EmailTemplateBuilder();
        const string link = "https://front.test/action?token=abc";
        var html = reset ? sut.BuildResetPasswordEmail("Artem", link) : sut.BuildConfirmEmail("Artem", link);
        html.Should().Contain($"<h2>{heading}</h2>").And.Contain("Artem").And.Contain($"href='{link}'");
    }
}
