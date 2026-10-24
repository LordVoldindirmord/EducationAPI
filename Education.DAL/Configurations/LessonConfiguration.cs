using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class LessonConfiguration : IEntityTypeConfiguration<LessonEntity>
    {
        public void Configure(EntityTypeBuilder<LessonEntity> builder)
        {
            builder.HasKey(l => l.Id);

            builder
                .Property(l => l.Title)
                .HasMaxLength(20);

            builder
                .Property(l => l.LessonType)
                .HasConversion(
                    l => ConvertLessonTypeToString(l),  // enum -> БД
                    l => ConvertStringToLessonType(l))  // БД -> enum
                .HasMaxLength(20);

            builder
                .Property(l => l.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .HasOne(l => l.Module)
                .WithMany(m => m.Lessons)
                .HasForeignKey(l => l.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(l => new { l.ModuleId, l.OrderNumber })
                .IsUnique();
        }

        private static string ConvertLessonTypeToString(LessonType type) => type switch
        {
            LessonType.Video => "video",
            LessonType.Text => "text",
            LessonType.Quiz => "quiz",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип урока"),
        };

        private static LessonType ConvertStringToLessonType(string value) => value switch
        {
            "video" => LessonType.Video,
            "text" => LessonType.Text,
            "quiz" => LessonType.Quiz,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестная тип урока в БД"),
        };
    }
}