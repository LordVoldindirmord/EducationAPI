using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Courses;
using Education.Service.Auth.Interfaces;
using Education.Service.Common.Interfaces;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace UnitTests.Courses;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IPermissionService> _permissions = new();
    private readonly Mock<ICourseAccessService> _access = new();
    private readonly Mock<IFileManager> _files = new();
    private readonly Guid _user = Guid.NewGuid(), _category = Guid.NewGuid(), _course = Guid.NewGuid();
    private const string OldImage = "Images/old.png", NewImage = "Images/new.png";
    private CourseService Sut => new(_courses.Object, _categories.Object, _permissions.Object, _access.Object, _files.Object, NullLogger<CourseService>.Instance, Mock.Of<ICurrentUserService>());
    private static IFormFile Image(string name = "cover.png", byte[]? bytes = null) => new FormFile(new MemoryStream(bytes ?? new byte[] { 1, 2, 3 }), 0, bytes?.Length ?? 3, "Image", name);
    private CreateCourseRequest CreateRequest => new() { Title = "Title", Description = "Description", Image = Image(), CategoryId = _category, Price = 125.50m, IsPublished = true };
    private UpdateCourseRequest UpdateRequest => new() { CourseId = _course, NewCategoryId = _category, NewTitle = "Updated", NewDescription = "Updated description", NewPrice = 99.99m };
    private CourseEntity Existing => new() { Id = _course, CategoryId = _category, Title = "Old title", ImagePath = OldImage };

    public CourseServiceTests()
    {
        _permissions.Setup(p => p.IsActiveAsync(_user)).ReturnsAsync(true);
        _permissions.Setup(p => p.HasAnyRoleAsync(_user, It.IsAny<IEnumerable<UserRole>>())).ReturnsAsync(true);
        _access.Setup(a => a.GetAccessTypeAsync(_user, _course)).ReturnsAsync(CourseAccessType.Owner);
        _categories.Setup(c => c.GetByIdAsync(_category)).ReturnsAsync(new CategoryEntity { Id = _category });
        _courses.Setup(c => c.GetByIdAsync(_course)).ReturnsAsync(Existing);
        _courses.Setup(c => c.GetWithContentAsync(_course)).ReturnsAsync(Existing);
        _files.Setup(f => f.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.Created(NewImage));
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.Ok(true));
    }

    [Fact]
    public async Task Create_SavesCourseAndOwnerAtomically()
    {
        var request = CreateRequest;
        (await Sut.CreateCourseAsync(request, _user)).StatusCode.Should().Be(StatusCode.Created);
        _courses.Verify(c => c.CreateAsync(It.Is<CourseEntity>(x => x.Title == request.Title && x.Description == request.Description
            && x.Price == request.Price && x.CategoryId == _category && x.IsPublished && x.ImagePath == NewImage
            && x.Members.Count == 1 && x.Members.Single().UserId == _user && x.Members.Single().Role == CourseRole.Owner)), Times.Once);
        _courses.Verify(c => c.SaveChangesAsync(), Times.Once);
        _files.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Create_RequiresActiveAccountAndCreationRole(bool active)
    {
        _permissions.Setup(p => p.IsActiveAsync(_user)).ReturnsAsync(active);
        _permissions.Setup(p => p.HasAnyRoleAsync(_user, It.IsAny<IEnumerable<UserRole>>())).ReturnsAsync(false);
        (await Sut.CreateCourseAsync(CreateRequest, _user)).StatusCode.Should().Be(StatusCode.Forbidden);
        _courses.VerifyNoOtherCalls();
        _files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("category", StatusCode.NotFound)]
    [InlineData("duplicate", StatusCode.Conflict)]
    [InlineData("precision", StatusCode.BadRequest)]
    public async Task Create_InvalidBusinessInput_DoesNotUpload(string issue, StatusCode status)
    {
        if (issue == "category") _categories.Setup(c => c.GetByIdAsync(_category)).ReturnsAsync((CategoryEntity?)null);
        if (issue == "duplicate") _courses.Setup(c => c.ExistsByTitleAsync(_category, "Title", null)).ReturnsAsync(true);
        var request = issue == "precision" ? CreateRequest with { Price = 1.123m } : CreateRequest;
        (await Sut.CreateCourseAsync(request, _user)).StatusCode.Should().Be(status);
        _files.VerifyNoOtherCalls();
        _courses.Verify(c => c.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("script.exe", false)] [InlineData("cover.png", true)]
    public async Task Create_RejectsInvalidCoverBeforeFileStorage(string name, bool empty)
    {
        var request = CreateRequest with { Image = Image(name, empty ? Array.Empty<byte>() : null) };
        (await Sut.CreateCourseAsync(request, _user)).StatusCode.Should().Be(StatusCode.BadRequest);
        _files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_UploadFailure_DoesNotSaveCourse()
    {
        _files.Setup(f => f.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.InternalError<string>("disk full"));
        (await Sut.CreateCourseAsync(CreateRequest, _user)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _courses.Verify(c => c.CreateAsync(It.IsAny<CourseEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, StatusCode.Conflict)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, StatusCode.Conflict)]
    [InlineData("XX000", StatusCode.InternalServerError)]
    public async Task Create_DatabaseFailure_MapsErrorAndRemovesUploadedCover(string sqlState, StatusCode status)
    {
        _courses.Setup(c => c.SaveChangesAsync()).ThrowsAsync(new DbUpdateException("write", new PostgresException("failure", "ERROR", "ERROR", sqlState)));
        (await Sut.CreateCourseAsync(CreateRequest, _user)).StatusCode.Should().Be(status);
        _files.Verify(f => f.DeleteAsync(NewImage, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("update", CourseAccessType.Teacher)]
    [InlineData("publish", CourseAccessType.Manager)]
    [InlineData("delete", CourseAccessType.Manager)]
    public async Task Mutations_RejectInsufficientCourseAccess(string operation, CourseAccessType access)
    {
        _access.Setup(a => a.GetAccessTypeAsync(_user, _course)).ReturnsAsync(access);
        var result = operation switch
        {
            "update" => await Sut.UpdateCourseAsync(UpdateRequest, _user),
            "publish" => await Sut.PublishCourseAsync(new() { CourseId = _course, IsPublished = true }, _user),
            _ => await Sut.DeleteCourseAsync(new() { CourseId = _course }, _user),
        };
        result.StatusCode.Should().Be(StatusCode.Forbidden);
        _courses.VerifyNoOtherCalls();
        _files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Update_ManagerCanChangeMetadataAndOptionallyReplaceCover(bool replace)
    {
        var course = Existing;
        _courses.Setup(c => c.GetByIdAsync(_course)).ReturnsAsync(course);
        _access.Setup(a => a.GetAccessTypeAsync(_user, _course)).ReturnsAsync(CourseAccessType.Manager);
        var request = UpdateRequest with { NewImage = replace ? Image() : null };
        (await Sut.UpdateCourseAsync(request, _user)).StatusCode.Should().Be(StatusCode.OK);
        course.Title.Should().Be(request.NewTitle);
        course.Description.Should().Be(request.NewDescription);
        course.Price.Should().Be(request.NewPrice);
        course.ImagePath.Should().Be(replace ? NewImage : OldImage);
        _courses.Verify(c => c.ExistsByTitleAsync(_category, request.NewTitle, _course), Times.Once);
        _courses.Verify(c => c.SaveChangesAsync(), Times.Once);
        _files.Verify(f => f.DeleteAsync(OldImage, It.IsAny<CancellationToken>()), replace ? Times.Once : Times.Never);
    }

    [Theory]
    [InlineData("course", StatusCode.NotFound)] [InlineData("category", StatusCode.NotFound)]
    [InlineData("duplicate", StatusCode.Conflict)] [InlineData("precision", StatusCode.BadRequest)]
    public async Task Update_InvalidInput_DoesNotUploadOrSave(string issue, StatusCode status)
    {
        if (issue == "course") _courses.Setup(c => c.GetByIdAsync(_course)).ReturnsAsync((CourseEntity?)null);
        if (issue == "category") _categories.Setup(c => c.GetByIdAsync(_category)).ReturnsAsync((CategoryEntity?)null);
        if (issue == "duplicate") _courses.Setup(c => c.ExistsByTitleAsync(_category, "Updated", _course)).ReturnsAsync(true);
        var request = UpdateRequest with { NewImage = Image(), NewPrice = issue == "precision" ? 1.123m : 10m };
        (await Sut.UpdateCourseAsync(request, _user)).StatusCode.Should().Be(status);
        _courses.Verify(c => c.SaveChangesAsync(), Times.Never);
        _files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_SaveFailure_DeletesNewCoverAndKeepsOldFile()
    {
        _courses.Setup(c => c.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        (await Sut.UpdateCourseAsync(UpdateRequest with { NewImage = Image() }, _user)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _files.Verify(f => f.DeleteAsync(NewImage, It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync(OldImage, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_OldCoverCleanupFailure_DoesNotUndoSavedResult()
    {
        _files.Setup(f => f.DeleteAsync(OldImage, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk"));
        var result = await Sut.UpdateCourseAsync(UpdateRequest with { NewImage = Image() }, _user);
        result.StatusCode.Should().Be(StatusCode.OK);
        result.Description.Should().Contain("старую обложку");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Publish_ChangesPublicationOnlyWhenNecessary(bool alreadyPublished)
    {
        var course = Existing;
        course.IsPublished = alreadyPublished;
        _courses.Setup(c => c.GetByIdAsync(_course)).ReturnsAsync(course);
        (await Sut.PublishCourseAsync(new() { CourseId = _course, IsPublished = true }, _user)).StatusCode.Should().Be(StatusCode.OK);
        course.IsPublished.Should().BeTrue();
        _courses.Verify(c => c.SaveChangesAsync(), alreadyPublished ? Times.Never : Times.Once);
    }

    [Fact]
    public async Task Publish_SaveFailure_ReturnsInternalError()
    {
        _courses.Setup(c => c.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        (await Sut.PublishCourseAsync(new() { CourseId = _course, IsPublished = true }, _user)).StatusCode.Should().Be(StatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(PurchaseStatus.Pending)] [InlineData(PurchaseStatus.Completed)] [InlineData(PurchaseStatus.Refunded)]
    public async Task Delete_RejectsPendingOrHistoricallySuccessfulPurchases(PurchaseStatus status)
    {
        var course = Existing;
        course.Purchases.Add(new CoursePurchaseEntity { Status = status });
        _courses.Setup(c => c.GetWithContentAsync(_course)).ReturnsAsync(course);
        (await Sut.DeleteCourseAsync(new() { CourseId = _course }, _user)).StatusCode.Should().Be(StatusCode.Conflict);
        _courses.Verify(c => c.DeleteWithContent(It.IsAny<CourseEntity>()), Times.Never);
        _files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_SaveFailure_LeavesFilesUntouched()
    {
        _courses.Setup(c => c.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        (await Sut.DeleteCourseAsync(new() { CourseId = _course }, _user)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _files.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Delete_RemovesContentThenFiles_ReportsCleanupFailureWithoutFailingDeletion(bool cleanupFails)
    {
        var course = Existing;
        course.Purchases.Add(new CoursePurchaseEntity { Status = PurchaseStatus.Failed });
        course.Modules.Add(new ModuleEntity { OrderNumber = 1, Lessons = new List<LessonEntity> { new VideoLessonEntity { OrderNumber = 1, LessonType = LessonType.Video, VideoPath = "Videos/video.mp4", ThumbnailPath = OldImage } } });
        _courses.Setup(c => c.GetWithContentAsync(_course)).ReturnsAsync(course);
        var saved = false;
        _courses.Setup(c => c.SaveChangesAsync()).Callback(() => saved = true).Returns(Task.CompletedTask);
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(() => saved.Should().BeTrue())
            .ReturnsAsync(cleanupFails ? CreatorResponse.InternalError<bool>("disk") : CreatorResponse.Ok(true));
        (await Sut.DeleteCourseAsync(new() { CourseId = _course }, _user)).StatusCode.Should().Be(StatusCode.OK);
        _courses.Verify(c => c.DeleteWithContent(course), Times.Once);
        _files.Verify(f => f.DeleteAsync(OldImage, It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync("Videos/video.mp4", It.IsAny<CancellationToken>()), Times.Once);
    }
}
