using Education.DAL.Interfaces;
using Education.Service.Courses.Implementations;

namespace UnitTests.Courses;

public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _repo = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid();
    private EnrollmentService Sut => new(_repo.Object);

    [Fact]
    public async Task Enroll_CreatesEnrollmentAndSaves()
    {
        (await Sut.EnrollAsync(_user, _course)).StatusCode.Should().Be(StatusCode.Created);
        _repo.Verify(r => r.CreateAsync(It.Is<EnrollmentEntity>(e => e.UserId == _user && e.CourseId == _course)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Enroll_AlreadyEnrolled_DoesNotWrite()
    {
        _repo.Setup(r => r.ExistsAsync(_user, _course)).ReturnsAsync(true);
        (await Sut.EnrollAsync(_user, _course)).StatusCode.Should().Be(StatusCode.Conflict);
        _repo.Verify(r => r.CreateAsync(It.IsAny<EnrollmentEntity>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsEnrolled_UsesBothIdentifiers(bool exists)
    {
        _repo.Setup(r => r.ExistsAsync(_user, _course)).ReturnsAsync(exists);
        (await Sut.IsEnrolledAsync(_user, _course)).Should().Be(exists);
        _repo.Verify(r => r.ExistsAsync(_user, _course), Times.Once);
    }

    [Fact]
    public async Task Unenroll_DeletesExistingEnrollment()
    {
        var enrollment = new EnrollmentEntity { UserId = _user, CourseId = _course };
        _repo.Setup(r => r.GetByUserAndCourseTrackedAsync(_user, _course)).ReturnsAsync(enrollment);
        (await Sut.UnenrollAsync(_user, _course)).StatusCode.Should().Be(StatusCode.OK);
        _repo.Verify(r => r.Delete(enrollment), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Unenroll_Missing_DoesNotWrite()
    {
        (await Sut.UnenrollAsync(_user, _course)).StatusCode.Should().Be(StatusCode.NotFound);
        _repo.Verify(r => r.Delete(It.IsAny<EnrollmentEntity>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
