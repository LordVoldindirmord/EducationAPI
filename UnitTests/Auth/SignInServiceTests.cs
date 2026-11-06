using Education.Service.Auth.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnitTests.Auth;

public class SignInServiceTests
{
    private readonly Mock<SignInManager<UserEntity>> _manager = new(MockUserManager.Create().Object,
        Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<UserEntity>>(),
        Options.Create(new IdentityOptions()), Mock.Of<ILogger<SignInManager<UserEntity>>>(),
        Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<UserEntity>>());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SignIn_ForwardsUserAndPersistence(bool persistent)
    {
        var user = UserFactory.Create();
        await new SignInService(_manager.Object).SignInAsync(user, persistent);
        _manager.Verify(m => m.SignInAsync(user, persistent, null), Times.Once);
    }

    [Fact]
    public async Task SignOut_DelegatesToIdentity()
    {
        await new SignInService(_manager.Object).SignOutAsync();
        _manager.Verify(m => m.SignOutAsync(), Times.Once);
    }
}
