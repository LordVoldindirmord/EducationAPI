using Education.DAL.Interfaces;
using Education.Service.Courses.Implementations;

namespace UnitTests.Courses;

public class CourseMembershipServiceTests
{
    private readonly Mock<ICourseMemberRepository> _repo = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid();
    private CourseMembershipService Sut => new(_repo.Object);

    [Theory]
    [InlineData(CourseRole.Owner)]
    [InlineData(CourseRole.Manager)]
    [InlineData(CourseRole.Teacher)]
    [InlineData(null)]
    public async Task GetRole_ReturnsMembershipRoleOrNull(CourseRole? role)
    {
        _repo.Setup(r => r.GetByUserAndCourseAsync(_user, _course))
            .ReturnsAsync(role.HasValue ? new CourseMemberEntity { Role = role.Value } : null);
        (await Sut.GetRoleAsync(_user, _course)).Should().Be(role);
    }

    [Theory]
    [InlineData(CourseRole.Owner, true)]
    [InlineData(CourseRole.Teacher, false)]
    [InlineData(null, false)]
    public async Task HasAnyRole_RequiresMembershipAndMatchingRole(CourseRole? role, bool expected)
    {
        _repo.Setup(r => r.GetByUserAndCourseAsync(_user, _course))
            .ReturnsAsync(role.HasValue ? new CourseMemberEntity { Role = role.Value } : null);
        (await Sut.HasAnyRoleAsync(_user, _course, new[] { CourseRole.Owner, CourseRole.Manager })).Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsMember_DelegatesWithBothIdentifiers(bool exists)
    {
        _repo.Setup(r => r.ExistsAsync(_user, _course)).ReturnsAsync(exists);
        (await Sut.IsMemberAsync(_user, _course)).Should().Be(exists);
        _repo.Verify(r => r.ExistsAsync(_user, _course), Times.Once);
    }
}
