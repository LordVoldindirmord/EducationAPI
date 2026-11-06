using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Modules;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace UnitTests.Courses;

public class ModuleServiceTests
{
    private readonly Mock<IModuleRepository> _modules = new();
    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<ICourseAccessService> _access = new();
    private readonly Mock<IFileManager> _files = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid(), _module = Guid.NewGuid();
    private ModuleService Sut => new(_modules.Object, _courses.Object, _access.Object, _files.Object, NullLogger<ModuleService>.Instance);
    private CreateModuleRequest Create => new() { CourseId = _course, Title = "Module", OrderNumber = 1 };
    private UpdateModuleRequest Update => new() { ModuleId = _module, NewTitle = "Updated", NewOrderNumber = 2 };

    public ModuleServiceTests()
    {
        var module = new ModuleEntity { Id = _module, CourseId = _course, OrderNumber = 1 };
        module.Lessons.Add(new VideoLessonEntity { OrderNumber = 1, LessonType = LessonType.Video, VideoPath = "Videos/v.mp4", ThumbnailPath = "Images/t.png" });
        _courses.Setup(r => r.GetByIdAsync(_course)).ReturnsAsync(new CourseEntity { Id = _course });
        _modules.Setup(r => r.GetByIdAsync(_module)).ReturnsAsync(module);
        _modules.Setup(r => r.GetWithContentAsync(_module)).ReturnsAsync(module);
        _access.Setup(a => a.CanManageAsync(_user, _course)).ReturnsAsync(true);
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.Ok(true));
    }

    [Theory]
    [InlineData("create")][InlineData("update")][InlineData("delete")]
    public async Task Mutations_RequireManagementAccess(string operation)
    {
        _access.Setup(a => a.CanManageAsync(_user, _course)).ReturnsAsync(false);
        var response = operation switch
        {
            "create" => await Sut.CreateModuleAsync(Create, _user),
            "update" => await Sut.UpdateModuleAsync(Update, _user),
            _ => await Sut.DeleteModuleAsync(new() { ModuleId = _module }, _user)
        };
        response.StatusCode.Should().Be(StatusCode.Forbidden);
        _modules.Verify(r => r.SaveChangesAsync(), Times.Never);
        _files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_PersistsModule()
    {
        (await Sut.CreateModuleAsync(Create, _user)).StatusCode.Should().Be(StatusCode.Created);
        _modules.Verify(r => r.CreateAsync(It.Is<ModuleEntity>(m => m.CourseId == _course && m.OrderNumber == 1 && m.Title == "Module")), Times.Once);
    }

    [Fact]
    public async Task Update_PersistsTrackedEntity()
    {
        (await Sut.UpdateModuleAsync(Update, _user)).IsSuccess.Should().BeTrue();
        var module = await _modules.Object.GetByIdAsync(_module);
        module!.Title.Should().Be("Updated");
        module.OrderNumber.Should().Be(2);
        _modules.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("23505", StatusCode.Conflict)][InlineData("XX000", StatusCode.InternalServerError)]
    public async Task Create_MapsDatabaseErrors(string code, StatusCode expected)
    {
        _modules.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException("db", new PostgresException("failure", "ERROR", "ERROR", code)));
        (await Sut.CreateModuleAsync(Create, _user)).StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Delete_CleansFilesOnlyAfterCommit_AndToleratesCleanupFailure(bool fails)
    {
        bool saved = false;
        _modules.Setup(r => r.SaveChangesAsync()).Callback(() => saved = true).Returns(Task.CompletedTask);
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => saved.Should().BeTrue()).ReturnsAsync(fails ? CreatorResponse.InternalError<bool>() : CreatorResponse.Ok(true));
        (await Sut.DeleteModuleAsync(new() { ModuleId = _module }, _user)).StatusCode.Should().Be(StatusCode.OK);
        _files.Verify(f => f.DeleteAsync("Videos/v.mp4", It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync("Images/t.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_DatabaseFailureKeepsFiles()
    {
        _modules.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        (await Sut.DeleteModuleAsync(new() { ModuleId = _module }, _user)).IsSuccess.Should().BeFalse();
        _files.VerifyNoOtherCalls();
    }
}
