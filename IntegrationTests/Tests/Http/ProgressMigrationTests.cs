using Education.DAL.Context;
using Education.Domain.Entity;
using Education.Domain.Enums;
using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace IntegrationTests.Tests.Http;

public class ProgressMigrationTests
{
    [Fact]
    public async Task Upgrade_PreservesExistingCompletionAndEnforcesScoreRange()
    {
        var isolated = new PostgresFixture();
        try
        {
            await isolated.InitializeAsync();
            await using var db = new EducationDbContext(new DbContextOptionsBuilder<EducationDbContext>().UseNpgsql(isolated.ConnectionString).Options);
            var migrations = db.Database.GetMigrations().ToArray();
            var previous = migrations.TakeWhile(m => !m.EndsWith("_AddQuizProgress")).Last();
            await db.GetService<IMigrator>().MigrateAsync(previous);

            var user = new UserEntity { FirstName = "Test", LastName = "Student", UserName = "migration-user" };
            var lesson = new TextLessonEntity
            {
                Title = "Old lesson", Content = "Text", OrderNumber = 1, LessonType = LessonType.Text,
                Module = new ModuleEntity
                {
                    Title = "Old module", OrderNumber = 1,
                    Course = new CourseEntity { Title = "Old course", ImagePath = "Images/old.png", Category = new CategoryEntity { Name = "Old category" } }
                }
            };
            db.Users.Add(user);
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            var id = Guid.NewGuid();
            var completed = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"UserProgresses\" (\"Id\", \"UserId\", \"LessonId\", \"CompletedAt\") VALUES ({id}, {user.Id}, {lesson.Id}, {completed})");
            await db.Database.MigrateAsync();

            var progress = await db.UserProgresses.SingleAsync(p => p.Id == id);
            progress.IsPassed.Should().BeTrue();
            progress.Score.Should().BeNull();
            progress.CompletedAt.Should().Be(completed);
            Func<Task> invalid = async () => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"UserProgresses\" SET \"Score\" = {101} WHERE \"Id\" = {id}");
            var exception = await invalid.Should().ThrowAsync<PostgresException>();
            exception.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }
}
