using System.Security.Claims;
using Education.Service.Auth.Implementations;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Auth;

public class CurrentUserServiceTests
{
    [Fact]
    public void MissingHttpContext_ReturnsAnonymousValues()
    {
        var sut = new CurrentUserService(new HttpContextAccessor());
        sut.UserId.Should().BeNull();
        sut.Role.Should().BeNull();
        sut.UserName.Should().BeNull();
        sut.Email.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void AuthenticatedPrincipal_ExposesClaims()
    {
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, "artem"),
            new Claim(ClaimTypes.Email, "a@test.com"), new Claim(ClaimTypes.Role, "course_creator"),
        }, "test")) };
        var sut = new CurrentUserService(new HttpContextAccessor { HttpContext = context });
        sut.UserId.Should().Be(id);
        sut.UserName.Should().Be("artem");
        sut.Email.Should().Be("a@test.com");
        sut.Role.Should().Be(UserRole.CourseCreator);
        sut.IsAuthenticated.Should().BeTrue();
    }

    [Theory]
    [InlineData("bad-id", "unknown")]
    [InlineData("", "")]
    public void InvalidClaims_ReturnNullInsteadOfThrowing(string id, string role)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        { new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role) })) };
        var sut = new CurrentUserService(new HttpContextAccessor { HttpContext = context });
        sut.UserId.Should().BeNull();
        sut.Role.Should().BeNull();
    }
}
