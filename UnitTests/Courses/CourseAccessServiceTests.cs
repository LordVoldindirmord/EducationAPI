using Education.DAL.Interfaces;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;

namespace UnitTests.Courses;

public class CourseAccessServiceTests
{
    private readonly Mock<ICourseMembershipService> _members = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid();
    private CourseAccessService Sut => new(_members.Object, _enrollments.Object, _courses.Object);

    [Theory]
    [InlineData(CourseRole.Owner, CourseAccessType.Owner, true)]
    [InlineData(CourseRole.Manager, CourseAccessType.Manager, true)]
    [InlineData(CourseRole.Teacher, CourseAccessType.Teacher, false)]
    public async Task Membership_TakesPriorityOverEnrollment(CourseRole role, CourseAccessType access, bool manages)
    {
        _members.Setup(m => m.GetRoleAsync(_user, _course)).ReturnsAsync(role);
        (await Sut.GetAccessTypeAsync(_user, _course)).Should().Be(access);
        (await Sut.CanManageAsync(_user, _course)).Should().Be(manages);
        (await Sut.CanViewAsync(_user, _course)).Should().BeTrue();
        (await Sut.HasAccessAsync(_user, _course)).Should().BeTrue();
        _enrollments.Verify(e => e.IsEnrolledAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _courses.Verify(c => c.IsPublishedAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Student_CanViewOnlyPublishedCourse(bool published)
    {
        _enrollments.Setup(e => e.IsEnrolledAsync(_user, _course)).ReturnsAsync(true);
        _courses.Setup(c => c.IsPublishedAsync(_course)).ReturnsAsync(published);
        (await Sut.GetAccessTypeAsync(_user, _course)).Should().Be(CourseAccessType.Student);
        (await Sut.CanViewAsync(_user, _course)).Should().Be(published);
        (await Sut.CanManageAsync(_user, _course)).Should().BeFalse();
        (await Sut.HasAccessAsync(_user, _course)).Should().BeTrue();
    }

    [Fact]
    public async Task UnrelatedUser_HasNoAccess()
    {
        (await Sut.GetAccessTypeAsync(_user, _course)).Should().Be(CourseAccessType.None);
        (await Sut.HasAccessAsync(_user, _course)).Should().BeFalse();
        (await Sut.CanViewAsync(_user, _course)).Should().BeFalse();
        (await Sut.CanManageAsync(_user, _course)).Should().BeFalse();
        _courses.Verify(c => c.IsPublishedAsync(It.IsAny<Guid>()), Times.Never);
    }
}
