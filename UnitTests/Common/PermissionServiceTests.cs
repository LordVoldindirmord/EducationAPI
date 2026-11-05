using Education.Service.Common.Implementations;
using Microsoft.AspNetCore.Identity;

namespace UnitTests.Common;

public class PermissionServiceTests
{
    private readonly Mock<UserManager<UserEntity>> _users = MockUserManager.Create();
    private readonly Guid _id = Guid.NewGuid();
    private PermissionService Sut => new(_users.Object);

    [Theory]
    [InlineData("admin", UserRole.Admin)]
    [InlineData("moderator", UserRole.Moderator)]
    [InlineData("course_creator", UserRole.CourseCreator)]
    [InlineData("user", UserRole.User)]
    [InlineData("unknown", null)]
    [InlineData("", null)]
    public async Task GetUserRole_MapsStoredRole(string role, UserRole? expected)
    {
        var user = UserFactory.Create(id: _id);
        _users.Setup(u => u.FindByIdAsync(_id.ToString())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new[] { role });
        (await Sut.GetUserRoleAsync(_id)).Should().Be(expected);
    }

    [Fact]
    public async Task MissingUser_HasNoRoleOrPermissions()
    {
        (await Sut.GetUserRoleAsync(_id)).Should().BeNull();
        (await Sut.HasRoleAsync(_id, UserRole.Admin)).Should().BeFalse();
        (await Sut.HasAnyRoleAsync(_id, new[] { UserRole.Admin })).Should().BeFalse();
        (await Sut.IsActiveAsync(_id)).Should().BeFalse();
    }

    [Fact]
    public async Task UserWithoutRoles_HasNoRole()
    {
        var user = UserFactory.Create();
        _users.Setup(u => u.FindByIdAsync(_id.ToString())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(Array.Empty<string>());
        (await Sut.GetUserRoleAsync(_id)).Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActive_RejectsLockedAccount(bool locked)
    {
        var user = UserFactory.Create();
        _users.Setup(u => u.FindByIdAsync(_id.ToString())).ReturnsAsync(user);
        _users.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(locked);
        (await Sut.IsActiveAsync(_id)).Should().Be(!locked);
    }

    [Fact]
    public async Task RoleChecks_MatchOnlyAllowedRoles()
    {
        var user = UserFactory.Create();
        _users.Setup(u => u.FindByIdAsync(_id.ToString())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new[] { "moderator" });
        (await Sut.HasRoleAsync(_id, UserRole.Moderator)).Should().BeTrue();
        (await Sut.HasRoleAsync(_id, UserRole.Admin)).Should().BeFalse();
        (await Sut.HasAnyRoleAsync(_id, new[] { UserRole.Admin, UserRole.Moderator })).Should().BeTrue();
        (await Sut.HasAnyRoleAsync(_id, Array.Empty<UserRole>())).Should().BeFalse();
    }
}
