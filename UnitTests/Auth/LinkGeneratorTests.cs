using Education.Service.Auth.Implementations;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Auth;

public class LinkGeneratorTests
{
    [Theory]
    [InlineData(false, "/confirm")]
    [InlineData(true, "/reset")]
    public void Links_UseConfiguredPathAndEncodeToken(bool reset, string path)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frontend:BaseUrl"] = "https://frontend.test", ["Frontend:ConfirmEmailPath"] = "/confirm", ["Frontend:ResetPasswordPath"] = "/reset",
        }).Build();
        var sut = new LinkGenerator(config);
        var id = Guid.NewGuid();
        const string token = "a+b/c=&? token";
        var link = reset ? sut.GetResetPasswordLink(id, token) : sut.GetConfirmEmailLink(id, token);
        link.Should().Be($"https://frontend.test{path}?userId={id}&token={Uri.EscapeDataString(token)}");
    }

    [Theory]
    [InlineData("Frontend:BaseUrl", false)]
    [InlineData("Frontend:ConfirmEmailPath", false)]
    [InlineData("Frontend:ResetPasswordPath", true)]
    public void MissingConfiguration_ReportsRequiredKey(string key, bool reset)
    {
        var settings = new Dictionary<string, string?> { ["Frontend:BaseUrl"] = "https://front.test", ["Frontend:ConfirmEmailPath"] = "/confirm", ["Frontend:ResetPasswordPath"] = "/reset" };
        settings.Remove(key);
        var sut = new LinkGenerator(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        Action act = () => { if (reset) sut.GetResetPasswordLink(Guid.NewGuid(), "token"); else sut.GetConfirmEmailLink(Guid.NewGuid(), "token"); };
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }
}
