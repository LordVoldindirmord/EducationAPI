using Education.DAL.Interfaces;
using Education.Service.Common.Interfaces;
using Education.Service.Purchases.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UnitTests.Purchases;

public class RolePurchaseHandlerTests
{
    private readonly Mock<UserManager<UserEntity>> _users = MockUserManager.Create();
    private readonly Mock<RoleManager<AppRoleEntity>> _roles = new(Mock.Of<IRoleStore<AppRoleEntity>>(),
        Array.Empty<IRoleValidator<AppRoleEntity>>(), Mock.Of<ILookupNormalizer>(), new IdentityErrorDescriber(), Mock.Of<ILogger<RoleManager<AppRoleEntity>>>());
    private readonly Mock<IPermissionService> _permissions = new();
    private readonly Mock<IPurchaseRepository> _purchases = new();
    private readonly MockTransactionContext _db = new();
    private readonly UserEntity _user = UserFactory.Create();
    private readonly AppRoleEntity _role = new() { Id = Guid.NewGuid(), Name = "course_creator", Price = 100m };
    private RolePurchaseHandler Sut => new(_users.Object, _roles.Object, _permissions.Object, _purchases.Object, _db.Context.Object);
    private RolePurchaseEntity Purchase => new() { UserId = _user.Id, RoleId = _role.Id, PurchaseType = PurchaseType.Role };
    private static IdentityResult Error => IdentityResult.Failed(new IdentityError { Description = "role write failed" });

    public RolePurchaseHandlerTests()
    {
        _permissions.Setup(p => p.GetUserRoleAsync(_user.Id)).ReturnsAsync(UserRole.User);
        _roles.Setup(r => r.FindByNameAsync("course_creator")).ReturnsAsync(_role);
        _roles.Setup(r => r.FindByIdAsync(_role.Id.ToString())).ReturnsAsync(_role);
        _roles.Setup(r => r.FindByNameAsync("user")).ReturnsAsync(new AppRoleEntity { Name = "user" });
        _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(_user);
        _users.Setup(u => u.GetRolesAsync(_user)).ReturnsAsync(new[] { "user" });
        _users.Setup(u => u.RemoveFromRolesAsync(_user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.RemoveFromRoleAsync(_user, "course_creator")).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.AddToRoleAsync(_user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
    }

    [Theory]
    [InlineData(null)] [InlineData(UserRole.Admin)] [InlineData(UserRole.Moderator)] [InlineData(UserRole.CourseCreator)]
    public async Task GetRole_RejectsMissingPrivilegedOrUnchangedRole(UserRole? current)
    {
        _permissions.Setup(p => p.GetUserRoleAsync(_user.Id)).ReturnsAsync(current);
        (await Sut.GetRoleForPurchaseAsync(_user.Id, "course_creator")).StatusCode.Should().Be(StatusCode.Conflict);
        _purchases.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("invalid", StatusCode.BadRequest)] [InlineData("missing", StatusCode.NotFound)]
    [InlineData("free", StatusCode.Conflict)] [InlineData("purchased", StatusCode.Conflict)] [InlineData("valid", StatusCode.OK)]
    public async Task GetRole_ValidatesTargetPriceAndPreviousPurchases(string scenario, StatusCode expected)
    {
        if (scenario == "missing") _roles.Setup(r => r.FindByNameAsync("course_creator")).ReturnsAsync((AppRoleEntity?)null);
        if (scenario == "free") _role.Price = 0;
        _purchases.Setup(p => p.ExistsCompletedRoleAsync(_user.Id, _role.Id)).ReturnsAsync(scenario == "purchased");
        var result = await Sut.GetRoleForPurchaseAsync(_user.Id, scenario == "invalid" ? "invalid" : "course_creator");
        result.StatusCode.Should().Be(expected);
        if (expected == StatusCode.OK) result.Data.Should().BeSameAs(_role);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task WrongPurchaseType_DoesNotStartTransaction(bool revoke)
    {
        var result = revoke ? await Sut.RevokeRoleAccessAsync(new CoursePurchaseEntity()) : await Sut.CompleteRolePurchaseAsync(new CoursePurchaseEntity());
        result.StatusCode.Should().Be(StatusCode.InternalServerError);
        _db.Database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Complete_ReplacesExistingRoleAndCommitsCompletedPurchase()
    {
        var purchase = Purchase;
        var before = DateTime.UtcNow;
        (await Sut.CompleteRolePurchaseAsync(purchase)).IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PurchaseStatus.Completed);
        purchase.CompletedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        _users.Verify(u => u.RemoveFromRolesAsync(_user, It.Is<IEnumerable<string>>(roles => roles.Single() == "user")), Times.Once);
        _users.Verify(u => u.AddToRoleAsync(_user, "course_creator"), Times.Once);
        _purchases.Verify(p => p.Update(purchase), Times.Once);
        _db.Committed();
    }

    [Theory]
    [InlineData("user")] [InlineData("role")] [InlineData("assign")]
    public async Task Complete_MissingIdentityOrAssignmentFailure_RollsBack(string failure)
    {
        if (failure == "user") _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync((UserEntity?)null);
        if (failure == "role") _roles.Setup(r => r.FindByIdAsync(_role.Id.ToString())).ReturnsAsync((AppRoleEntity?)null);
        if (failure == "assign") _users.Setup(u => u.AddToRoleAsync(_user, "course_creator")).ReturnsAsync(Error);
        (await Sut.CompleteRolePurchaseAsync(Purchase)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _db.RolledBack();
    }

    [Fact]
    public async Task Complete_PreviousRoleRemovalFailure_DoesNotAssignSecondRole()
    {
        _users.Setup(u => u.RemoveFromRolesAsync(_user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(Error);
        (await Sut.CompleteRolePurchaseAsync(Purchase)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _users.Verify(u => u.AddToRoleAsync(_user, It.IsAny<string>()), Times.Never);
        _db.RolledBack();
    }

    [Fact]
    public async Task Revoke_RemovesPaidRoleRestoresDefaultAndCommits()
    {
        var purchase = Purchase;
        (await Sut.RevokeRoleAccessAsync(purchase)).IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PurchaseStatus.Refunded);
        _users.Verify(u => u.RemoveFromRoleAsync(_user, "course_creator"), Times.Once);
        _users.Verify(u => u.AddToRoleAsync(_user, "user"), Times.Once);
        _db.Purchases.Verify(p => p.Update(purchase), Times.Once);
        _db.Committed();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Revoke_IdentityFailure_DoesNotCommitRefund(bool addFails)
    {
        if (addFails) _users.Setup(u => u.AddToRoleAsync(_user, "user")).ReturnsAsync(Error);
        else _users.Setup(u => u.RemoveFromRoleAsync(_user, "course_creator")).ReturnsAsync(Error);
        (await Sut.RevokeRoleAccessAsync(Purchase)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _db.RolledBack();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DatabaseFailure_RollsBackBeforeChangingUserRoles(bool revoke)
    {
        _db.Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());
        var result = revoke ? await Sut.RevokeRoleAccessAsync(Purchase) : await Sut.CompleteRolePurchaseAsync(Purchase);
        result.StatusCode.Should().Be(StatusCode.InternalServerError);
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>()), Times.Never);
        _db.RolledBack();
    }
}
