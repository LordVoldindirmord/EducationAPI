using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Progress;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace UnitTests.Courses;

public class ProgressServiceTests
{
    private readonly Mock<IUserProgressRepository> _progress = new();
    private readonly Mock<ILessonRepository> _lessons = new();
    private readonly Mock<IEnrollmentService> _enrollment = new();
    private readonly Mock<ICourseAccessService> _access = new();
    private readonly Guid _user = Guid.NewGuid(), _lesson = Guid.NewGuid(), _course = Guid.NewGuid();
    private ProgressService Sut => new(_progress.Object, _lessons.Object, _enrollment.Object, _access.Object, NullLogger<ProgressService>.Instance);
    private ModuleEntity Module => new() { CourseId = _course, OrderNumber = 1 };

    public ProgressServiceTests()
    {
        _enrollment.Setup(e => e.IsEnrolledAsync(_user, _course)).ReturnsAsync(true);
        SetLesson(new TextLessonEntity { Id = _lesson, Module = Module, OrderNumber = 1, LessonType = LessonType.Text, Content = "Text" });
    }

    private void SetLesson(LessonEntity lesson)
    {
        _lessons.Setup(r => r.GetByIdAsync(_lesson)).ReturnsAsync(lesson);
        _lessons.Setup(r => r.GetWithContentAsync(_lesson)).ReturnsAsync(lesson);
    }

    private QuizLessonEntity Quiz(int count = 2, int passingScore = 50)
    {
        var quiz = new QuizLessonEntity
        {
            Id = _lesson, Module = Module, OrderNumber = 1, LessonType = LessonType.Quiz, PassingScore = passingScore,
            Questions = Enumerable.Range(1, count).Select(i => new TestQuestionEntity
            {
                OrderNumber = i * 10, QuestionText = "Question", Options = new() { "A", "B", "C" }, CorrectAnswers = new() { 0, 2 }
            }).ToList()
        };
        SetLesson(quiz);
        return quiz;
    }

    private SubmitQuizRequest Request(params UserAnswer[] answers) => new() { LessonId = _lesson, Answers = answers.ToList() };
    private static UserAnswer Answer(int order, params int[] selected) => new() { QuestionOrderNumber = order, SelectedOptions = selected.ToList() };

    [Theory]
    [InlineData(LessonType.Video)][InlineData(LessonType.Text)]
    public async Task Complete_SavesCompletionWithoutScore(LessonType type)
    {
        SetLesson(new LessonEntity { Id = _lesson, Module = Module, OrderNumber = 1, LessonType = type });
        var started = DateTime.UtcNow;
        (await Sut.CompleteLessonAsync(_user, _lesson)).StatusCode.Should().Be(StatusCode.OK);
        _progress.Verify(r => r.CreateAsync(It.Is<UserProgressEntity>(p => p.UserId == _user && p.LessonId == _lesson
            && p.IsPassed && p.Score == null && p.CompletedAt >= started && p.CompletedAt.Kind == DateTimeKind.Utc)), Times.Once);
        _progress.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Complete_RepeatedRequestDoesNotWriteAgain()
    {
        _progress.Setup(r => r.GetByUserAndLessonTrackedAsync(_user, _lesson)).ReturnsAsync(new UserProgressEntity());
        var response = await Sut.CompleteLessonAsync(_user, _lesson);
        response.Description.Should().Be("Уже пройден");
        response.Data.Should().BeTrue();
        _progress.Verify(r => r.CreateAsync(It.IsAny<UserProgressEntity>()), Times.Never);
        _progress.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothOperations_RequireEnrollmentOrCourseAccess(bool quiz)
    {
        if (quiz) Quiz();
        _enrollment.Setup(e => e.IsEnrolledAsync(_user, _course)).ReturnsAsync(false);
        var status = quiz ? (await Sut.SubmitQuizAsync(_user, Request(Answer(10, 0, 2), Answer(20, 0, 2)))).StatusCode
            : (await Sut.CompleteLessonAsync(_user, _lesson)).StatusCode;
        status.Should().Be(StatusCode.Forbidden);
        _progress.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_CourseAccessAllowsCompletionWithoutEnrollment()
    {
        _enrollment.Setup(e => e.IsEnrolledAsync(_user, _course)).ReturnsAsync(false);
        _access.Setup(a => a.HasAccessAsync(_user, _course)).ReturnsAsync(true);
        (await Sut.CompleteLessonAsync(_user, _lesson)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Complete_DoesNotBypassQuizGrading()
    {
        Quiz();
        (await Sut.CompleteLessonAsync(_user, _lesson)).StatusCode.Should().Be(StatusCode.BadRequest);
        _progress.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothOperations_MissingLessonReturnsNotFound(bool quiz)
    {
        _lessons.Setup(r => r.GetByIdAsync(_lesson)).ReturnsAsync((LessonEntity?)null);
        _lessons.Setup(r => r.GetWithContentAsync(_lesson)).ReturnsAsync((LessonEntity?)null);
        var status = quiz ? (await Sut.SubmitQuizAsync(_user, Request())).StatusCode : (await Sut.CompleteLessonAsync(_user, _lesson)).StatusCode;
        status.Should().Be(StatusCode.NotFound);
        _enrollment.VerifyNoOtherCalls();
        _access.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Submit_RequiresQuizWithQuestions()
    {
        (await Sut.SubmitQuizAsync(_user, Request())).StatusCode.Should().Be(StatusCode.BadRequest);
        Quiz(count: 0);
        (await Sut.SubmitQuizAsync(_user, Request())).StatusCode.Should().Be(StatusCode.Conflict);
        _progress.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1, 3, 34, 33, false)][InlineData(2, 3, 66, 66, true)]
    [InlineData(1, 2, 50, 50, true)][InlineData(0, 2, 0, 0, true)]
    [InlineData(0, 2, 50, 0, false)][InlineData(2, 2, 100, 100, true)]
    public async Task Submit_GradesExactSetsRegardlessOfOrder(int correct, int count, int passing, int expected, bool passed)
    {
        Quiz(count, passing);
        var answers = Enumerable.Range(1, count).Reverse().Select(i => i <= correct ? Answer(i * 10, 2, 0) : Answer(i * 10, 0)).ToArray();
        var response = await Sut.SubmitQuizAsync(_user, Request(answers));
        response.StatusCode.Should().Be(StatusCode.OK);
        response.Data!.Score.Should().Be(expected);
        response.Data.IsPassed.Should().Be(passed);
        response.Data.PassingScore.Should().Be(passing);
        _progress.Verify(r => r.CreateAsync(It.Is<UserProgressEntity>(p => p.UserId == _user && p.LessonId == _lesson && p.Score == expected && p.IsPassed == passed)), Times.Once);
    }

    [Theory]
    [InlineData("count")][InlineData("duplicate-question")][InlineData("unknown-question")]
    [InlineData("negative-option")][InlineData("outside-options")][InlineData("duplicate-option")]
    [InlineData("null-options")][InlineData("null-answer")][InlineData("null-answers")]
    public async Task Submit_RejectsMalformedAnswersWithoutWriting(string issue)
    {
        Quiz();
        var answers = new List<UserAnswer> { Answer(10, 0, 2), Answer(20, 0, 2) };
        switch (issue)
        {
            case "count": answers.RemoveAt(1); break;
            case "duplicate-question": answers[1] = Answer(10, 0); break;
            case "unknown-question": answers[1] = Answer(30, 0); break;
            case "negative-option": answers[0] = Answer(10, -1); break;
            case "outside-options": answers[0] = Answer(10, 3); break;
            case "duplicate-option": answers[0] = Answer(10, 0, 0, 2); break;
            case "null-options": answers[0] = Answer(10) with { SelectedOptions = null! }; break;
            case "null-answer": answers[0] = null!; break;
            case "null-answers": answers = null!; break;
        }
        (await Sut.SubmitQuizAsync(_user, new() { LessonId = _lesson, Answers = answers })).StatusCode.Should().Be(StatusCode.BadRequest);
        _progress.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Submit_EmptySelectionCountsAsIncorrect()
    {
        Quiz();
        (await Sut.SubmitQuizAsync(_user, Request(Answer(10), Answer(20)))).Data!.Score.Should().Be(0);
    }

    [Fact]
    public async Task Submit_RetakeUpdatesSameRecordAndReplacesPreviousResult()
    {
        Quiz();
        var previousTime = DateTime.UtcNow.AddDays(-1);
        var existing = new UserProgressEntity { Id = Guid.NewGuid(), UserId = _user, LessonId = _lesson, CompletedAt = previousTime, Score = 100, IsPassed = true };
        _progress.Setup(r => r.GetByUserAndLessonTrackedAsync(_user, _lesson)).ReturnsAsync(existing);
        var response = await Sut.SubmitQuizAsync(_user, Request(Answer(10), Answer(20)));
        response.IsSuccess.Should().BeTrue();
        existing.Score.Should().Be(0);
        existing.IsPassed.Should().BeFalse();
        existing.CompletedAt.Should().BeAfter(previousTime);
        _progress.Verify(r => r.CreateAsync(It.IsAny<UserProgressEntity>()), Times.Never);
        _progress.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(false, "23505", StatusCode.Conflict)][InlineData(true, "23505", StatusCode.Conflict)]
    [InlineData(false, "23503", StatusCode.Conflict)][InlineData(true, "23503", StatusCode.Conflict)]
    [InlineData(false, "XX000", StatusCode.InternalServerError)][InlineData(true, "XX000", StatusCode.InternalServerError)]
    public async Task BothOperations_MapDatabaseErrors(bool quiz, string code, StatusCode expected)
    {
        if (quiz) Quiz();
        _progress.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new DbUpdateException("db", new PostgresException("failure", "ERROR", "ERROR", code)));
        var status = quiz ? (await Sut.SubmitQuizAsync(_user, Request(Answer(10, 0, 2), Answer(20, 0, 2)))).StatusCode
            : (await Sut.CompleteLessonAsync(_user, _lesson)).StatusCode;
        status.Should().Be(expected);
    }
}
