using System.Net;
using System.Text.Json;
using Education.Domain.Entity;
using Education.Domain.Enums;
using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Tests.Http;

[Collection(AuthIntegrationCollection.Name)]
public class ModuleLessonEndpointTests(PostgresFixture postgres) : EndpointTestBase(postgres)
{
    private async Task<Guid> CreateModuleAsync()
    {
        await LoginAsync("admin");
        var course = await CreateCourseAsync(await CreateCategoryAsync());
        await SendAsync(HttpMethod.Post, "/api/module/create", new { CourseId = course, Title = "Module", OrderNumber = 1 }, HttpStatusCode.Created);
        return await ReadDbAsync(db => db.Modules.Where(m => m.CourseId == course).Select(m => m.Id).SingleAsync());
    }

    private static MultipartFormDataContent LessonForm(Guid module, LessonType type, Guid? lesson = null, int order = 1)
    {
        var form = new MultipartFormDataContent();
        var prefix = lesson.HasValue ? "New" : "";
        form.Add(new StringContent(lesson.HasValue ? "Updated lesson" : "Lesson"), prefix + "Title");
        form.Add(new StringContent((lesson.HasValue ? 2 : order).ToString()), prefix + "OrderNumber");
        if (lesson.HasValue) form.Add(new StringContent(lesson.Value.ToString()), "LessonId");
        else
        {
            form.Add(new StringContent(module.ToString()), "ModuleId");
            form.Add(new StringContent(type.ToString()), "LessonType");
        }
        form.Add(new StringContent(lesson.HasValue ? "true" : "false"), "IsPublished");
        if (type == LessonType.Video)
        {
            form.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), prefix + "VideoFile", "video.mp4");
            form.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), prefix + "ThumbnailFile", "thumb.png");
            form.Add(new StringContent(lesson.HasValue ? "180" : "120"), prefix + "DurationSeconds");
        }
        if (type == LessonType.Text) form.Add(new StringContent(lesson.HasValue ? "Updated text content" : "Text content"), prefix + "TextContent");
        if (type == LessonType.Quiz)
        {
            form.Add(new StringContent(lesson.HasValue ? "80" : "70"), prefix + "PassingScore");
            form.Add(new StringContent(lesson.HasValue ? "Updated question" : "Question"), prefix + "Questions[0].QuestionText");
            form.Add(new StringContent("1"), prefix + "Questions[0].OrderNumber");
            form.Add(new StringContent("A"), prefix + "Questions[0].Options[0]");
            form.Add(new StringContent("B"), prefix + "Questions[0].Options[1]");
            form.Add(new StringContent("0"), prefix + "Questions[0].CorrectAnswers[0]");
        }
        return form;
    }

    [Theory]
    [InlineData(LessonType.Video)][InlineData(LessonType.Text)][InlineData(LessonType.Quiz)]
    public async Task Lesson_CreateUpdateDelete_RoundTripsTptAndRemovesProgress(LessonType type)
    {
        var module = await CreateModuleAsync();
        using var create = LessonForm(module, type);
        using var created = await Client.PostAsync("/api/lesson/create", create);
        await AssertApiAsync(created, HttpStatusCode.Created);
        var lesson = await ReadDbAsync(db => db.Lessons.SingleAsync(l => l.ModuleId == module));
        lesson.LessonType.Should().Be(type);
        lesson.GetType().Should().Be(type switch { LessonType.Video => typeof(VideoLessonEntity), LessonType.Text => typeof(TextLessonEntity), _ => typeof(QuizLessonEntity) });

        lesson.Title.Should().Be("Lesson");
        lesson.OrderNumber.Should().Be(1);
        lesson.IsPublished.Should().BeFalse();
        var oldPaths = lesson is VideoLessonEntity oldVideo
            ? new[] { oldVideo.VideoPath, oldVideo.ThumbnailPath } : Array.Empty<string>();
        foreach (var path in oldPaths) File.Exists(Path.Combine(App.StorageRoot, path)).Should().BeTrue();
        if (lesson is TextLessonEntity originalText) originalText.Content.Should().Be("Text content");

        using var update = LessonForm(module, type, lesson.Id);
        using var updated = await Client.PutAsync("/api/lesson/update", update);
        await AssertApiAsync(updated, HttpStatusCode.OK);
        lesson = await ReadDbAsync(db => db.Lessons.SingleAsync(l => l.Id == lesson.Id));
        lesson.Title.Should().Be("Updated lesson");
        lesson.OrderNumber.Should().Be(2);
        lesson.IsPublished.Should().BeTrue();
        if (lesson is TextLessonEntity text) text.Content.Should().Be("Updated text content");
        if (lesson is QuizLessonEntity quiz)
        {
            quiz.PassingScore.Should().Be(80);
            var question = await ReadDbAsync(db => db.TestQuestions.SingleAsync(q => q.QuizLessonId == lesson.Id));
            question.QuestionText.Should().Be("Updated question");
            question.Options.Should().Equal("A", "B");
            question.CorrectAnswers.Should().Equal(0);
        }
        if (lesson is VideoLessonEntity updatedVideo)
        {
            updatedVideo.DurationSeconds.Should().Be(180);
            oldPaths.Should().NotContain(updatedVideo.VideoPath).And.NotContain(updatedVideo.ThumbnailPath);
            foreach (var path in oldPaths) File.Exists(Path.Combine(App.StorageRoot, path)).Should().BeFalse();
            File.Exists(Path.Combine(App.StorageRoot, updatedVideo.VideoPath)).Should().BeTrue();
            File.Exists(Path.Combine(App.StorageRoot, updatedVideo.ThumbnailPath)).Should().BeTrue();
        }

        await ReadDbAsync(async db =>
        {
            var owner = await db.CourseMembers.Where(m => m.CourseId == db.Modules.Where(m => m.Id == module).Select(m => m.CourseId).Single()).Select(m => m.UserId).SingleAsync();
            db.UserProgresses.Add(new UserProgressEntity { UserId = owner, LessonId = lesson.Id });
            return await db.SaveChangesAsync();
        });
        await SendAsync(HttpMethod.Delete, "/api/lesson/delete", new { LessonId = lesson.Id });
        (await ReadDbAsync(db => db.Lessons.AnyAsync(l => l.Id == lesson.Id))).Should().BeFalse();
        (await ReadDbAsync(db => db.UserProgresses.AnyAsync(p => p.LessonId == lesson.Id))).Should().BeFalse();
        if (lesson is VideoLessonEntity video)
        {
            File.Exists(Path.Combine(App.StorageRoot, video.VideoPath)).Should().BeFalse();
            File.Exists(Path.Combine(App.StorageRoot, video.ThumbnailPath)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Module_UpdateAndDelete_RemovesVideoFilesAndProgress()
    {
        var module = await CreateModuleAsync();
        using var form = LessonForm(module, LessonType.Video);
        using var response = await Client.PostAsync("/api/lesson/create", form);
        await AssertApiAsync(response, HttpStatusCode.Created);
        var video = (VideoLessonEntity)await ReadDbAsync(db => db.Lessons.SingleAsync(l => l.ModuleId == module));
        await ReadDbAsync(async db =>
        {
            var course = await db.Modules.Where(m => m.Id == module).Select(m => m.CourseId).SingleAsync();
            var owner = await db.CourseMembers.Where(m => m.CourseId == course).Select(m => m.UserId).SingleAsync();
            db.UserProgresses.Add(new UserProgressEntity { UserId = owner, LessonId = video.Id });
            return await db.SaveChangesAsync();
        });
        await SendAsync(HttpMethod.Put, "/api/module/update", new { ModuleId = module, NewTitle = "Updated", NewOrderNumber = 2 });
        var updatedModule = await ReadDbAsync(db => db.Modules.SingleAsync(m => m.Id == module));
        updatedModule.Title.Should().Be("Updated");
        updatedModule.OrderNumber.Should().Be(2);
        await SendAsync(HttpMethod.Delete, "/api/module/delete", new { ModuleId = module });
        (await ReadDbAsync(db => db.Lessons.AnyAsync(l => l.ModuleId == module))).Should().BeFalse();
        (await ReadDbAsync(db => db.UserProgresses.AnyAsync(p => p.LessonId == video.Id))).Should().BeFalse();
        (await ReadDbAsync(db => db.Modules.AnyAsync(m => m.Id == module))).Should().BeFalse();
        File.Exists(Path.Combine(App.StorageRoot, video.VideoPath)).Should().BeFalse();
        File.Exists(Path.Combine(App.StorageRoot, video.ThumbnailPath)).Should().BeFalse();
    }

    [Fact]
    public async Task DuplicateOrder_ReturnsConflictAndRollsBackUploadedFiles()
    {
        var module = await CreateModuleAsync();
        using var first = LessonForm(module, LessonType.Video);
        using var created = await Client.PostAsync("/api/lesson/create", first);
        await AssertApiAsync(created, HttpStatusCode.Created);
        var files = Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().ToArray();
        using var duplicate = LessonForm(module, LessonType.Video);
        using var conflict = await Client.PostAsync("/api/lesson/create", duplicate);
        await AssertApiAsync(conflict, HttpStatusCode.Conflict);
        Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().Should().Equal(files);
        (await ReadDbAsync(db => db.Lessons.CountAsync(l => l.ModuleId == module))).Should().Be(1);
        var course = await ReadDbAsync(db => db.Modules.Where(m => m.Id == module).Select(m => m.CourseId).SingleAsync());
        await SendAsync(HttpMethod.Post, "/api/module/create", new { CourseId = course, Title = "Duplicate", OrderNumber = 1 }, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Lesson_RejectsUserWithoutCourseMembership()
    {
        var module = await CreateModuleAsync();
        await SendAsync(HttpMethod.Post, "/api/auth/logout", null);
        await LoginAsync();
        using var form = LessonForm(module, LessonType.Text);
        using var response = await Client.PostAsync("/api/lesson/create", form);
        await AssertApiAsync(response, HttpStatusCode.Forbidden);
    }

    public static TheoryData<string, string> ManagementEndpoints => new()
    {
        { "module", "create" }, { "module", "update" }, { "module", "delete" },
        { "lesson", "create" }, { "lesson", "update" }, { "lesson", "delete" }
    };

    public static TheoryData<string, string, string> CourseRoleEndpoints
    {
        get
        {
            var cases = new TheoryData<string, string, string>();
            foreach (var endpoint in ManagementEndpoints)
                foreach (var role in new[] { "manager", "teacher", "student", "outsider" })
                    cases.Add((string)endpoint[0], (string)endpoint[1], role);
            return cases;
        }
    }

    private async Task<(Guid Course, Guid Module, Guid Lesson)> CreateContentAsync(LessonType type = LessonType.Text)
    {
        var module = await CreateModuleAsync();
        using var form = LessonForm(module, type);
        using var response = await Client.PostAsync("/api/lesson/create", form);
        await AssertApiAsync(response, HttpStatusCode.Created);
        var course = await ReadDbAsync(db => db.Modules.Where(m => m.Id == module).Select(m => m.CourseId).SingleAsync());
        var lesson = await ReadDbAsync(db => db.Lessons.Where(l => l.ModuleId == module).Select(l => l.Id).SingleAsync());
        return (course, module, lesson);
    }

    private async Task MutateAsync(string resource, string operation, Guid course, Guid module, Guid lesson, HttpStatusCode expected)
    {
        if (resource == "module")
        {
            var method = operation switch { "create" => HttpMethod.Post, "update" => HttpMethod.Put, _ => HttpMethod.Delete };
            object request = operation switch
            {
                "create" => new { CourseId = course, Title = "Second module", OrderNumber = 2 },
                "update" => new { ModuleId = module, NewTitle = "Changed module", NewDescription = "Changed description", NewOrderNumber = 2 },
                _ => new { ModuleId = module }
            };
            await SendAsync(method, $"/api/module/{operation}", request, expected);
        }
        else if (operation == "delete")
            await SendAsync(HttpMethod.Delete, "/api/lesson/delete", new { LessonId = lesson }, expected);
        else
        {
            using var form = LessonForm(module, LessonType.Text, operation == "update" ? lesson : null, order: 2);
            using var response = operation == "create"
                ? await Client.PostAsync("/api/lesson/create", form)
                : await Client.PutAsync("/api/lesson/update", form);
            await AssertApiAsync(response, expected);
        }
    }

    private Task<string> ContentSnapshotAsync(Guid course) => ReadDbAsync(async db =>
    {
        var modules = await db.Modules.AsNoTracking().Where(m => m.CourseId == course)
            .Include(m => m.Lessons).OrderBy(m => m.Id).ToListAsync();
        return JsonSerializer.Serialize(modules.Select(m => new
        {
            m.Id, m.Title, m.Description, m.OrderNumber,
            Lessons = m.Lessons.OrderBy(l => l.Id).Select(l => new
            {
                l.Id, l.Title, l.OrderNumber, l.LessonType, l.IsPublished,
                Content = (l as TextLessonEntity)?.Content
            })
        }));
    });

    [Theory]
    [MemberData(nameof(CourseRoleEndpoints))]
    public async Task EveryEndpoint_EnforcesCourseRole(string resource, string operation, string role)
    {
        var (course, module, lesson) = await CreateContentAsync();
        await SendAsync(HttpMethod.Post, "/api/auth/logout", null);
        var actor = await LoginAsync();
        await ReadDbAsync(async db =>
        {
            var user = await db.Users.Where(u => u.Email == actor.Email).Select(u => u.Id).SingleAsync();
            if (role == "student") db.Enrollments.Add(new EnrollmentEntity { CourseId = course, UserId = user });
            if (role is "manager" or "teacher")
                db.CourseMembers.Add(new CourseMemberEntity
                {
                    CourseId = course, UserId = user,
                    Role = role == "manager" ? CourseRole.Manager : CourseRole.Teacher
                });
            return await db.SaveChangesAsync();
        });

        var before = await ContentSnapshotAsync(course);
        var expected = role == "manager"
            ? operation == "create" ? HttpStatusCode.Created : HttpStatusCode.OK
            : HttpStatusCode.Forbidden;
        await MutateAsync(resource, operation, course, module, lesson, expected);
        var after = await ContentSnapshotAsync(course);
        if (role == "manager") after.Should().NotBe(before);
        else after.Should().Be(before);
    }

    [Theory]
    [MemberData(nameof(ManagementEndpoints))]
    public async Task EveryEndpoint_MissingResource_Returns404(string resource, string operation)
    {
        var (course, module, lesson) = await CreateContentAsync();
        var before = await ContentSnapshotAsync(course);
        await MutateAsync(resource, operation,
            resource == "module" && operation == "create" ? Guid.NewGuid() : course,
            resource == "module" || operation == "create" ? Guid.NewGuid() : module,
            Guid.NewGuid(), HttpStatusCode.NotFound);
        (await ContentSnapshotAsync(course)).Should().Be(before);
    }

    [Theory]
    [MemberData(nameof(ManagementEndpoints))]
    public async Task EveryEndpoint_EmptyId_Returns400(string resource, string operation)
    {
        var (course, module, lesson) = await CreateContentAsync();
        var before = await ContentSnapshotAsync(course);
        await MutateAsync(resource, operation, Guid.Empty, Guid.Empty, Guid.Empty, HttpStatusCode.BadRequest);
        (await ContentSnapshotAsync(course)).Should().Be(before);
    }

    [Fact]
    public async Task ModuleCreate_PersistsAllFieldsInRequestedCourse()
    {
        var (course, module, _) = await CreateContentAsync();
        await SendAsync(HttpMethod.Post, "/api/module/create",
            new { CourseId = course, Title = "Second module", Description = "Description", OrderNumber = 2 }, HttpStatusCode.Created);
        var created = await ReadDbAsync(db => db.Modules.SingleAsync(m => m.CourseId == course && m.Id != module));
        created.Title.Should().Be("Second module");
        created.Description.Should().Be("Description");
        created.OrderNumber.Should().Be(2);
        created.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task ModuleUpdate_DuplicateOrder_Returns409AndKeepsExistingValues()
    {
        var (course, module, _) = await CreateContentAsync();
        await SendAsync(HttpMethod.Post, "/api/module/create",
            new { CourseId = course, Title = "Second module", OrderNumber = 2 }, HttpStatusCode.Created);
        var before = await ContentSnapshotAsync(course);
        await SendAsync(HttpMethod.Put, "/api/module/update",
            new { ModuleId = module, NewTitle = "Conflict", NewOrderNumber = 2 }, HttpStatusCode.Conflict);
        (await ContentSnapshotAsync(course)).Should().Be(before);
    }

    [Fact]
    public async Task LessonUpdate_DuplicateOrder_RollsBackNewFilesAndKeepsOldContent()
    {
        var (course, module, lesson) = await CreateContentAsync(LessonType.Video);
        using var second = LessonForm(module, LessonType.Text, order: 2);
        using var created = await Client.PostAsync("/api/lesson/create", second);
        await AssertApiAsync(created, HttpStatusCode.Created);
        var before = await ReadDbAsync(db => db.Set<VideoLessonEntity>().SingleAsync(l => l.Id == lesson));
        var files = Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().ToArray();

        using var update = LessonForm(module, LessonType.Video, lesson);
        using var response = await Client.PutAsync("/api/lesson/update", update);
        await AssertApiAsync(response, HttpStatusCode.Conflict);
        var after = await ReadDbAsync(db => db.Set<VideoLessonEntity>().SingleAsync(l => l.Id == lesson));
        after.Title.Should().Be(before.Title);
        after.OrderNumber.Should().Be(before.OrderNumber);
        after.DurationSeconds.Should().Be(before.DurationSeconds);
        after.VideoPath.Should().Be(before.VideoPath);
        after.ThumbnailPath.Should().Be(before.ThumbnailPath);
        Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().Should().Equal(files);
    }

    [Theory]
    [InlineData("missing-video")][InlineData("missing-thumbnail")][InlineData("wrong-video-extension")]
    [InlineData("empty-text")][InlineData("invalid-answer")][InlineData("duplicate-question-order")]
    public async Task LessonCreate_InvalidContent_Returns400WithoutSavingOrLeakingFiles(string issue)
    {
        var module = await CreateModuleAsync();
        var type = issue is "invalid-answer" or "duplicate-question-order" ? LessonType.Quiz
            : issue == "empty-text" ? LessonType.Text : LessonType.Video;
        using var form = LessonForm(module, type);
        var fields = issue switch
        {
            "missing-video" or "wrong-video-extension" => new[] { "VideoFile" },
            "missing-thumbnail" => new[] { "ThumbnailFile" },
            "empty-text" => new[] { "TextContent" },
            "invalid-answer" => new[] { "Questions[0].CorrectAnswers[0]" },
            _ => Array.Empty<string>()
        };
        using var invalid = new MultipartFormDataContent();
        foreach (var part in form)
            if (!fields.Contains(part.Headers.ContentDisposition!.Name!.Trim('"')))
                invalid.Add(part);
        if (issue == "wrong-video-extension") invalid.Add(new ByteArrayContent(new byte[] { 1 }), "VideoFile", "image.png");
        if (issue == "invalid-answer") invalid.Add(new StringContent("5"), "Questions[0].CorrectAnswers[0]");
        if (issue == "duplicate-question-order")
        {
            invalid.Add(new StringContent("Second question"), "Questions[1].QuestionText");
            invalid.Add(new StringContent("1"), "Questions[1].OrderNumber");
            invalid.Add(new StringContent("A"), "Questions[1].Options[0]");
            invalid.Add(new StringContent("B"), "Questions[1].Options[1]");
            invalid.Add(new StringContent("0"), "Questions[1].CorrectAnswers[0]");
        }
        var files = Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().ToArray();
        using var response = await Client.PostAsync("/api/lesson/create", invalid);
        await AssertApiAsync(response, HttpStatusCode.BadRequest);
        (await ReadDbAsync(db => db.Lessons.AnyAsync(l => l.ModuleId == module))).Should().BeFalse();
        Directory.GetFiles(App.StorageRoot, "*", SearchOption.AllDirectories).Order().Should().Equal(files);
    }

    [Fact]
    public async Task LessonUpdate_RejectsContentForDifferentType_AndKeepsText()
    {
        var (_, module, lesson) = await CreateContentAsync();
        using var form = LessonForm(module, LessonType.Video, lesson);
        using var response = await Client.PutAsync("/api/lesson/update", form);
        await AssertApiAsync(response, HttpStatusCode.BadRequest);
        var text = await ReadDbAsync(db => db.Set<TextLessonEntity>().SingleAsync(l => l.Id == lesson));
        text.Title.Should().Be("Lesson");
        text.Content.Should().Be("Text content");
    }
}
