using Education.DAL.Interfaces;
using Education.Service.Courses.Interfaces;
using Education.Service.Purchases.Implementations;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Purchases;

public class CoursePurchaseHandlerTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IPurchaseRepository> _purchases = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();
    private readonly Mock<ICourseAccessService> _access = new();
    private readonly MockTransactionContext _db = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid();
    private CoursePurchaseHandler Sut => new(_courses.Object, _purchases.Object, _enrollments.Object, _access.Object, _db.Context.Object);
    private CoursePurchaseEntity Purchase => new() { UserId = _user, CourseId = _course };

    [Theory]
    [InlineData("missing", StatusCode.NotFound)] [InlineData("draft", StatusCode.Conflict)]
    [InlineData("free", StatusCode.Conflict)] [InlineData("purchased", StatusCode.Conflict)]
    [InlineData("access", StatusCode.Conflict)] [InlineData("valid", StatusCode.OK)]
    public async Task GetCourse_RequiresPublishedPaidCourseWithoutExistingAccess(string scenario, StatusCode expected)
    {
        var course = new CourseEntity { Id = _course, IsPublished = scenario != "draft", Price = scenario == "free" ? 0m : 100m };
        _courses.Setup(c => c.GetByIdAsync(_course)).ReturnsAsync(scenario == "missing" ? null : course);
        _purchases.Setup(p => p.ExistsCompletedCourseAsync(_user, _course)).ReturnsAsync(scenario == "purchased");
        _access.Setup(a => a.GetAccessTypeAsync(_user, _course)).ReturnsAsync(scenario == "access" ? CourseAccessType.Student : CourseAccessType.None);
        var result = await Sut.GetCourseForPurchaseAsync(_user, _course);
        result.StatusCode.Should().Be(expected);
        if (expected == StatusCode.OK) result.Data.Should().BeSameAs(course);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task WrongPurchaseType_DoesNotStartTransaction(bool revoke)
    {
        var result = revoke ? await Sut.RevokeCourseAccessAsync(new RolePurchaseEntity()) : await Sut.CompleteCoursePurchaseAsync(new RolePurchaseEntity());
        result.StatusCode.Should().Be(StatusCode.InternalServerError);
        _db.Database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StatusCode.Created, true)] [InlineData(StatusCode.Conflict, true)] [InlineData(StatusCode.Forbidden, false)]
    public async Task Complete_CommitsOnlyWhenEnrollmentSucceedsOrAlreadyExists(StatusCode enrollmentStatus, bool success)
    {
        var purchase = Purchase;
        var before = DateTime.UtcNow;
        _enrollments.Setup(e => e.EnrollAsync(_user, _course)).ReturnsAsync(new InternalResponse<bool> { StatusCode = enrollmentStatus });
        (await Sut.CompleteCoursePurchaseAsync(purchase)).IsSuccess.Should().Be(success);
        _purchases.Verify(p => p.Update(purchase), Times.Once);
        _db.Context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        if (success)
        {
            purchase.Status.Should().Be(PurchaseStatus.Completed);
            purchase.CompletedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
            _db.Committed();
        }
        else _db.RolledBack();
    }

    [Theory]
    [InlineData(StatusCode.OK, true)] [InlineData(StatusCode.NotFound, true)] [InlineData(StatusCode.Forbidden, false)]
    public async Task Revoke_CommitsOnlyWhenEnrollmentRemovedOrAbsent(StatusCode status, bool success)
    {
        var purchase = Purchase;
        _enrollments.Setup(e => e.UnenrollAsync(_user, _course)).ReturnsAsync(new InternalResponse<bool> { StatusCode = status });
        (await Sut.RevokeCourseAccessAsync(purchase)).IsSuccess.Should().Be(success);
        _db.Purchases.Verify(p => p.Update(purchase), Times.Once);
        if (success) { purchase.Status.Should().Be(PurchaseStatus.Refunded); _db.Committed(); }
        else _db.RolledBack();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SaveException_RollsBackWithoutChangingEnrollment(bool revoke)
    {
        _db.Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());
        var result = revoke ? await Sut.RevokeCourseAccessAsync(Purchase) : await Sut.CompleteCoursePurchaseAsync(Purchase);
        result.StatusCode.Should().Be(StatusCode.InternalServerError);
        _db.RolledBack();
        _enrollments.VerifyNoOtherCalls();
    }
}
