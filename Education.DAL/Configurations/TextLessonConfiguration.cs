using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class TextLessonConfiguration : IEntityTypeConfiguration<TextLessonEntity>
    {
        public void Configure(EntityTypeBuilder<TextLessonEntity> builder)
        {
            builder.HasKey(t => t.Id);

            builder
                .Property(t => t.Content)
                .HasMaxLength(32192);

            builder
                .HasOne(t => t.Lesson)
                .WithOne(l => l.TextLesson)
                .HasForeignKey<TextLessonEntity>(t => t.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}