using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class VideoLessonConfiguration : IEntityTypeConfiguration<VideoLessonEntity>
    {
        public void Configure(EntityTypeBuilder<VideoLessonEntity> builder)
        {
            builder.HasKey(vl => vl.Id);

            builder
                .Property(vl => vl.VideoPath)
                .HasMaxLength(128);

            builder
                .Property(v => v.ThumbnailPath)
                .HasMaxLength(128);

            builder
                .HasOne(vl => vl.Lesson)
                .WithOne(l => l.VideoLesson)
                .HasForeignKey<VideoLessonEntity>(vl => vl.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ограничение целостности CHECK на продожительность (она должна быть больше 0)
            builder
                .ToTable(vl => vl.HasCheckConstraint(
                    "CK_VideoLessons_DurationSeconds",  // Это имя ограничения (CHECK имеет свое имя)
                    "\"DurationSeconds\" > 0")          // Условие ограничения
                );
        }
    }
}