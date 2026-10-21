using Education.Domain.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Context
{
    public class EducationDbContext(DbContextOptions options)
        : IdentityDbContext<UserEntity, AppRoleEntity, Guid>(options)
    {
        public DbSet<CategoryEntity> Categories { get; set; }

        public DbSet<CourseEntity> Courses { get; set; }

        public DbSet<ModuleEntity> Modules { get; set; }

        public DbSet<LessonEntity> Lessons { get; set; }

        public DbSet<VideoLessonEntity> VideoLessons { get; set; }

        public DbSet<TextLessonEntity> TextLessons { get; set; }

        public DbSet<QuizLessonEntity> QuizLessons { get; set; }

        public DbSet<TestQuestionEntity> TestQuestions { get; set; }

        public DbSet<CourseMemberEntity> CourseMembers { get; set; }

        public DbSet<EnrollmentEntity> Enrollments { get; set; }

        public DbSet<UserProgressEntity> UserProgresses { get; set; }

        public DbSet<PurchaseEntity> Purchase { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Это для Identity
            base.OnModelCreating(builder);

            // Конфигурации из сборки
            builder.ApplyConfigurationsFromAssembly(typeof(EducationDbContext).Assembly);
        }
    }
}
