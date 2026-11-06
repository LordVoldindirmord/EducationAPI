using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Lessons;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace UnitTests.Courses;

public class LessonServiceTests
{
    private readonly Mock<ILessonRepository> _lessons = new();
    private readonly Mock<IModuleRepository> _modules = new();
    private readonly Mock<ICourseAccessService> _access = new();
    private readonly Mock<IFileManager> _files = new();
    private readonly MockTransactionContext _db = new();
    private readonly Guid _user = Guid.NewGuid(), _course = Guid.NewGuid(), _module = Guid.NewGuid(), _lesson = Guid.NewGuid();
    private LessonService Sut => new(_lessons.Object, _modules.Object, _access.Object, _files.Object, _db.Context.Object, NullLogger<LessonService>.Instance);
    private static IFormFile File(string name) => new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "file", name);
    private static TestQuestionRequest Question(int order = 1) => new() { QuestionText = "Question", OrderNumber = order, Options = new() { "A", "B" }, CorrectAnswers = new() { 0 } };
    private CreateLessonRequest Create => new() { ModuleId = _module, Title = "Lesson", OrderNumber = 1, LessonType = LessonType.Video, VideoFile = File("v.mp4"), ThumbnailFile = File("t.png"), DurationSeconds = 60 };
    private UpdateLessonRequest Update => new() { LessonId = _lesson, NewTitle = "Updated", NewOrderNumber = 2 };

    public LessonServiceTests()
    {
        _modules.Setup(r => r.GetByIdAsync(_module)).ReturnsAsync(new ModuleEntity { Id = _module, CourseId = _course, OrderNumber = 1 });
        _access.Setup(a => a.CanManageAsync(_user, _course)).ReturnsAsync(true);
        _files.Setup(f => f.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string name, CancellationToken _) => CreatorResponse.Created(name.EndsWith(".mp4") ? "Videos/new.mp4" : "Images/new.png"));
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.Ok(true));
        Existing(new VideoLessonEntity { Id = _lesson, ModuleId = _module, OrderNumber = 1, LessonType = LessonType.Video, VideoPath = "Videos/old.mp4", ThumbnailPath = "Images/old.png", DurationSeconds = 30 });
    }

    private void Existing(LessonEntity lesson)
    {
        _lessons.Setup(r => r.GetByIdAsync(_lesson)).ReturnsAsync(lesson);
        _lessons.Setup(r => r.GetWithContentAsync(_lesson)).ReturnsAsync(lesson);
    }

    [Theory]
    [InlineData(LessonType.Video)][InlineData(LessonType.Text)][InlineData(LessonType.Quiz)]
    public async Task Create_PersistsDerivedEntityAndExplicitType_InTransaction(LessonType type)
    {
        var request = type == LessonType.Video ? Create : Create with
        {
            LessonType = type, VideoFile = null, ThumbnailFile = null, DurationSeconds = null,
            TextContent = type == LessonType.Text ? "Text" : null,
            Questions = type == LessonType.Quiz ? new() { Question() } : null
        };
        LessonEntity? captured = null;
        _lessons.Setup(r => r.CreateAsync(It.IsAny<LessonEntity>())).Callback<LessonEntity>(l => captured = l).Returns(Task.CompletedTask);
        (await Sut.CreateLessonAsync(request, _user)).StatusCode.Should().Be(StatusCode.Created);
        captured!.LessonType.Should().Be(type);
        captured.ModuleId.Should().Be(_module);
        captured.GetType().Should().Be(type switch { LessonType.Video => typeof(VideoLessonEntity), LessonType.Text => typeof(TextLessonEntity), _ => typeof(QuizLessonEntity) });
        if (captured is QuizLessonEntity quiz) quiz.Questions.Should().ContainSingle(q => q.QuestionText == "Question");
        _db.Committed();
        _files.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("create")][InlineData("update")][InlineData("delete")]
    public async Task Mutations_RequireAccessThroughModuleCourse(string operation)
    {
        _access.Setup(a => a.CanManageAsync(_user, _course)).ReturnsAsync(false);
        var response = operation switch
        {
            "create" => await Sut.CreateLessonAsync(Create, _user),
            "update" => await Sut.UpdateLessonAsync(Update, _user),
            _ => await Sut.DeleteLessonAsync(new() { LessonId = _lesson }, _user)
        };
        response.StatusCode.Should().Be(StatusCode.Forbidden);
        _files.VerifyNoOtherCalls();
        _lessons.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("23505", StatusCode.Conflict)][InlineData("XX000", StatusCode.InternalServerError)]
    public async Task Create_DatabaseFailureRemovesBothUploads(string code, StatusCode status)
    {
        _lessons.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException("db", new PostgresException("failure", "ERROR", "ERROR", code)));
        (await Sut.CreateLessonAsync(Create, _user)).StatusCode.Should().Be(status);
        _files.Verify(f => f.DeleteAsync("Videos/new.mp4", It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync("Images/new.png", It.IsAny<CancellationToken>()), Times.Once);
        _db.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _db.Transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_SecondUploadFailureRemovesFirst()
    {
        _files.Setup(f => f.UploadAsync(It.IsAny<Stream>(), "t.png", It.IsAny<CancellationToken>())).ReturnsAsync(CreatorResponse.InternalError<string>("disk"));
        (await Sut.CreateLessonAsync(Create, _user)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _files.Verify(f => f.DeleteAsync("Videos/new.mp4", It.IsAny<CancellationToken>()), Times.Once);
        _lessons.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("missing")][InlineData("duration")][InlineData("file")][InlineData("type")][InlineData("content")]
    public async Task Create_RejectsInvalidContent(string issue)
    {
        var request = issue switch
        {
            "missing" => Create with { VideoFile = null },
            "duration" => Create with { DurationSeconds = 0 },
            "file" => Create with { VideoFile = File("image.png") },
            "type" => Create with { LessonType = (LessonType)99 },
            _ => Create with { TextContent = "Foreign content" }
        };
        (await Sut.CreateLessonAsync(request, _user)).StatusCode.Should().Be(StatusCode.BadRequest);
        _lessons.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Create_RejectsInvalidQuestionAnswerAndDuplicateOrder()
    {
        foreach (var questions in new[] { new List<TestQuestionRequest> { Question() with { CorrectAnswers = new() { 2 } } }, new() { Question(), Question() } })
        {
            var request = Create with { LessonType = LessonType.Quiz, VideoFile = null, ThumbnailFile = null, DurationSeconds = null, Questions = questions };
            (await Sut.CreateLessonAsync(request, _user)).StatusCode.Should().Be(StatusCode.BadRequest);
        }
        _lessons.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Update_ReplacesFilesAndCleansCorrectVersion(bool databaseFails)
    {
        bool saved = false;
        if (databaseFails) _lessons.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        else _lessons.Setup(r => r.SaveChangesAsync()).Callback(() => saved = true).Returns(Task.CompletedTask);
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(() => saved.Should().Be(!databaseFails)).ReturnsAsync(CreatorResponse.Ok(true));
        var response = await Sut.UpdateLessonAsync(Update with { NewVideoFile = File("v.mp4"), NewThumbnailFile = File("t.png") }, _user);
        response.IsSuccess.Should().Be(!databaseFails);
        _files.Verify(f => f.DeleteAsync(databaseFails ? "Videos/new.mp4" : "Videos/old.mp4", It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync(databaseFails ? "Images/new.png" : "Images/old.png", It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteAsync(databaseFails ? "Videos/old.mp4" : "Videos/new.mp4", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_QuizReusesQuestionWithSameOrder()
    {
        var question = new TestQuestionEntity { Id = Guid.NewGuid(), OrderNumber = 1, QuestionText = "Old" };
        var quiz = new QuizLessonEntity { Id = _lesson, ModuleId = _module, OrderNumber = 1, LessonType = LessonType.Quiz, TimeLimitMinutes = 10, Questions = new List<TestQuestionEntity> { question } };
        Existing(quiz);
        (await Sut.UpdateLessonAsync(Update with { NewQuestions = new() { Question() }, RemoveTimeLimit = true }, _user)).IsSuccess.Should().BeTrue();
        quiz.Questions.Single().Should().BeSameAs(question);
        question.QuestionText.Should().Be("Question");
        quiz.TimeLimitMinutes.Should().BeNull();
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Delete_DatabaseFailureKeepsFiles_AndCleanupFailureKeepsSuccess(bool databaseFails)
    {
        bool saved = false;
        if (databaseFails) _lessons.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException());
        else _lessons.Setup(r => r.SaveChangesAsync()).Callback(() => saved = true).Returns(Task.CompletedTask);
        _files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(() => saved.Should().BeTrue()).ThrowsAsync(new IOException("disk"));
        (await Sut.DeleteLessonAsync(new() { LessonId = _lesson }, _user)).IsSuccess.Should().Be(!databaseFails);
        _files.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), databaseFails ? Times.Never() : Times.Exactly(2));
    }
}
