using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class QuizLessonConfiguration : IEntityTypeConfiguration<QuizLessonEntity>
    {
        public void Configure(EntityTypeBuilder<QuizLessonEntity> builder)
        {
            builder.HasKey(q => q.Id);

            builder
                .HasOne(q => q.Lesson)
                .WithOne(l => l.QuizLesson)
                .HasForeignKey<QuizLessonEntity>(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable(q =>
            {
                // Проходной балл в процентах
                q.HasCheckConstraint(
                    "CK_QuizLessons_PassingScore",
                    "\"PassingScore\" >= 0 AND \"PassingScore\" <= 100");

                // Ограничение теста по времени в минутах
                q.HasCheckConstraint(
                    "CK_QuizLessons_TimeLimitMinutes",
                    "\"TimeLimitMinutes\" IS NULL OR \"TimeLimitMinutes\" > 0");
            });
        }
    }
}
