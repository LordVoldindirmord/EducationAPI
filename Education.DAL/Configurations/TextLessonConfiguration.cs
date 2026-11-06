using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class TextLessonConfiguration : IEntityTypeConfiguration<TextLessonEntity>
    {
        public void Configure(EntityTypeBuilder<TextLessonEntity> builder)
        {
            builder.ToTable("TextLesson");

            builder
                .Property(t => t.Content)
                .HasMaxLength(32192);
        }
    }
}