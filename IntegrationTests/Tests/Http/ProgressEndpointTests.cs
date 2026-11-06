using System.Net;
using Education.Domain.DTO.Request.Courses.Progress;
using Education.Domain.Entity;
using Education.Domain.Enums;
using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Tests.Http;

[Collection(AuthIntegrationCollection.Name)]
public class ProgressEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    private async Task<(Guid Lesson, Guid User, Guid Course)> CreateLessonAsync(
        LessonType type, bool enrolled = true, CourseRole? role = null, int questionCount = 2, int passingScore = 50)
    {
        var actor = await LoginAsync();
        return await ReadDbAsync(async db =>
        {
            var user = await db.Users.Where(u => u.Email == actor.Email).Select(u => u.Id).SingleAsync();
            var course = new CourseEntity
            {
                Title = "Progress course", ImagePath = "Images/progress.png", IsPublished = true,
                Category = new CategoryEntity { Name = $"pr_{Guid.NewGuid():N}"[..20] }
            };
            var module = new ModuleEntity { Title = "Progress module", OrderNumber = 1, Course = course };
            LessonEntity lesson = type switch
            {
                LessonType.Video => new VideoLessonEntity { OrderNumber = 1, LessonType = type, VideoPath = "Videos/progress.mp4", ThumbnailPath = "Images/preview.png", DurationSeconds = 120 },
                LessonType.Text => new TextLessonEntity { OrderNumber = 1, LessonType = type, Content = "Text" },
                _ => new QuizLessonEntity
                {
                    OrderNumber = 1, LessonType = type, PassingScore = passingScore,
                    Questions = Enumerable.Range(1, questionCount).Select(i => new TestQuestionEntity
                    {
                        OrderNumber = i * 10, QuestionText = "Question", Options = new() { "A", "B", "C" },
                        CorrectAnswers = i == 1 ? new() { 0, 2 } : new() { 1 }
                    }).ToList()
                }
            };
            lesson.Title = "Progress lesson";
            lesson.IsPublished = true;
            lesson.Module = module;
            db.Lessons.Add(lesson);
            if (enrolled) course.Enrollments.Add(new EnrollmentEntity { UserId = user });
            if (role.HasValue) course.Members.Add(new CourseMemberEntity { UserId = user, Role = role.Value });
            await db.SaveChangesAsync();
            return (lesson.Id, user, course.Id);
        });
    }

    private static List<UserAnswer> Answers(int correct) => new()
    {
        new() { QuestionOrderNumber = 20, SelectedOptions = correct >= 2 ? new() { 1 } : new() },
        new() { QuestionOrderNumber = 10, SelectedOptions = correct >= 1 ? new() { 2, 0 } : new() { 0 } }
    };

    private Task<int> ProgressCountAsync(Guid lesson) => ReadDbAsync(db => db.UserProgresses.CountAsync(p => p.LessonId == lesson));

    [Theory]
    [InlineData(LessonType.Video)][InlineData(LessonType.Text)]
    public async Task Complete_RecordsCompletionAndIsIdempotent(LessonType type)
    {
        var (lesson, user, _) = await CreateLessonAsync(type);
        var started = DateTime.UtcNow;
        await SendAsync(HttpMethod.Post, $"/api/progress/complete/{lesson}", null);
        var first = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson && p.UserId == user));
        first.Score.Should().BeNull();
        first.IsPassed.Should().BeTrue();
        first.CompletedAt.Should().BeOnOrAfter(started).And.BeOnOrBefore(DateTime.UtcNow);
        first.CompletedAt.Kind.Should().Be(DateTimeKind.Utc);

        var response = await SendAsync(HttpMethod.Post, $"/api/progress/complete/{lesson}", null);
        response.GetProperty("description").GetString().Should().Be("Уже пройден");
        var repeated = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson && p.UserId == user));
        repeated.Id.Should().Be(first.Id);
        repeated.CompletedAt.Should().Be(first.CompletedAt);
        (await ProgressCountAsync(lesson)).Should().Be(1);
    }

    [Theory]
    [InlineData(0, 0, false)][InlineData(1, 50, true)][InlineData(2, 100, true)]
    public async Task SubmitQuiz_GradesOnServerAndReturnsOnlyResult(int correct, int expectedScore, bool passed)
    {
        var (lesson, user, _) = await CreateLessonAsync(LessonType.Quiz);
        var response = await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new
        {
            LessonId = lesson, Answers = Answers(correct), Score = 100, IsPassed = true, UserId = Guid.NewGuid()
        });
        var data = response.GetProperty("data");
        data.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("score", "isPassed", "passingScore");
        data.GetProperty("score").GetInt32().Should().Be(expectedScore);
        data.GetProperty("isPassed").GetBoolean().Should().Be(passed);
        data.GetProperty("passingScore").GetInt32().Should().Be(50);
        var progress = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson));
        progress.UserId.Should().Be(user);
        progress.Score.Should().Be(expectedScore);
        progress.IsPassed.Should().Be(passed);
    }

    [Fact]
    public async Task SubmitQuiz_RetakesReplaceResultInSameRecord()
    {
        var (lesson, _, _) = await CreateLessonAsync(LessonType.Quiz);
        await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = Answers(0) });
        var first = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson));
        foreach (var correct in new[] { 2, 0 })
        {
            await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = Answers(correct) });
            var progress = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson));
            progress.Id.Should().Be(first.Id);
            progress.Score.Should().Be(correct == 2 ? 100 : 0);
            progress.IsPassed.Should().Be(correct == 2);
            progress.CompletedAt.Should().BeOnOrAfter(first.CompletedAt);
        }
        (await ProgressCountAsync(lesson)).Should().Be(1);
    }

    [Fact]
    public async Task SubmitQuiz_TruncatesFractionalScoreBeforeThresholdComparison()
    {
        var (lesson, _, _) = await CreateLessonAsync(LessonType.Quiz, questionCount: 3, passingScore: 67);
        var answers = Answers(2);
        answers.Add(new() { QuestionOrderNumber = 30, SelectedOptions = new() });
        var response = await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = answers });
        response.GetProperty("data").GetProperty("score").GetInt32().Should().Be(66);
        response.GetProperty("data").GetProperty("isPassed").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData("count")][InlineData("duplicate-question")][InlineData("unknown-question")]
    [InlineData("negative-option")][InlineData("outside-options")][InlineData("duplicate-option")]
    [InlineData("null-options")][InlineData("null-answer")][InlineData("null-answers")]
    public async Task SubmitQuiz_InvalidAnswersReturn400AndDoNotChangeSavedAttempt(string issue)
    {
        var (lesson, _, _) = await CreateLessonAsync(LessonType.Quiz);
        await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = Answers(2) });
        var before = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson));
        var answers = Answers(2);
        switch (issue)
        {
            case "count": answers.RemoveAt(1); break;
            case "duplicate-question": answers[1] = answers[0]; break;
            case "unknown-question": answers[0] = answers[0] with { QuestionOrderNumber = 30 }; break;
            case "negative-option": answers[0] = answers[0] with { SelectedOptions = new() { -1 } }; break;
            case "outside-options": answers[0] = answers[0] with { SelectedOptions = new() { 3 } }; break;
            case "duplicate-option": answers[1] = answers[1] with { SelectedOptions = new() { 0, 0, 2 } }; break;
            case "null-options": answers[0] = answers[0] with { SelectedOptions = null! }; break;
            case "null-answer": answers[0] = null!; break;
            case "null-answers": answers = null!; break;
        }
        await SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = answers }, HttpStatusCode.BadRequest);
        var after = await ReadDbAsync(db => db.UserProgresses.SingleAsync(p => p.LessonId == lesson));
        after.Id.Should().Be(before.Id);
        after.Score.Should().Be(before.Score);
        after.IsPassed.Should().Be(before.IsPassed);
        after.CompletedAt.Should().Be(before.CompletedAt);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothEndpoints_DenyUnenrolledUserEvenWithEnrollmentInAnotherCourse(bool quiz)
    {
        var (lesson, user, _) = await CreateLessonAsync(quiz ? LessonType.Quiz : LessonType.Text, enrolled: false);
        await ReadDbAsync(async db =>
        {
            db.Courses.Add(new CourseEntity
            {
                Title = "Another course", ImagePath = "Images/another.png",
                Category = new CategoryEntity { Name = $"pr_{Guid.NewGuid():N}"[..20] },
                Enrollments = new List<EnrollmentEntity> { new() { UserId = user } }
            });
            return await db.SaveChangesAsync();
        });
        await SubmitAsync(quiz, lesson, HttpStatusCode.Forbidden);
        (await ProgressCountAsync(lesson)).Should().Be(0);
    }

    private Task SubmitAsync(bool quiz, Guid lesson, HttpStatusCode expected) => quiz
        ? SendAsync(HttpMethod.Post, "/api/progress/submit-quiz", new { LessonId = lesson, Answers = Answers(2) }, expected)
        : SendAsync(HttpMethod.Post, $"/api/progress/complete/{lesson}", null, expected);

    [Theory]
    [InlineData(false, CourseRole.Owner)][InlineData(true, CourseRole.Owner)]
    [InlineData(false, CourseRole.Manager)][InlineData(true, CourseRole.Manager)]
    [InlineData(false, CourseRole.Teacher)][InlineData(true, CourseRole.Teacher)]
    public async Task BothEndpoints_AllowCourseAccessWithoutEnrollment(bool quiz, CourseRole role)
    {
        var (lesson, _, _) = await CreateLessonAsync(quiz ? LessonType.Quiz : LessonType.Text, enrolled: false, role: role);
        await SubmitAsync(quiz, lesson, HttpStatusCode.OK);
        (await ProgressCountAsync(lesson)).Should().Be(1);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothEndpoints_MissingLessonReturns404(bool quiz)
    {
        await LoginAsync();
        await SubmitAsync(quiz, Guid.NewGuid(), HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothEndpoints_EmptyLessonIdReturns400(bool quiz)
    {
        await LoginAsync();
        await SubmitAsync(quiz, Guid.Empty, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(LessonType.Quiz)][InlineData(LessonType.Video)][InlineData(LessonType.Text)]
    public async Task BothEndpoints_RejectWrongLessonType(LessonType type)
    {
        var (lesson, _, _) = await CreateLessonAsync(type);
        await SubmitAsync(type != LessonType.Quiz, lesson, HttpStatusCode.BadRequest);
        (await ProgressCountAsync(lesson)).Should().Be(0);
    }

    [Fact]
    public async Task SubmitQuiz_EmptyQuizReturns409()
    {
        var (lesson, _, _) = await CreateLessonAsync(LessonType.Quiz, questionCount: 0);
        await SubmitAsync(true, lesson, HttpStatusCode.Conflict);
        (await ProgressCountAsync(lesson)).Should().Be(0);
    }

    [Fact]
    public async Task Complete_ConcurrentRequestsDoNotCreateDuplicateProgress()
    {
        var (lesson, _, _) = await CreateLessonAsync(LessonType.Text);
        var responses = await Task.WhenAll(
            Client.PostAsync($"/api/progress/complete/{lesson}", null),
            Client.PostAsync($"/api/progress/complete/{lesson}", null));
        try
        {
            responses.Should().Contain(r => r.StatusCode == HttpStatusCode.OK);
            foreach (var response in responses)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
                await AssertApiAsync(response, response.StatusCode);
            }
            (await ProgressCountAsync(lesson)).Should().Be(1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }
}
