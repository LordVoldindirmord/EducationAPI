using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<CourseEntity>
    {
        public void Configure(EntityTypeBuilder<CourseEntity> builder)
        {
            builder.HasKey(c => c.Id);

            builder
                .Property(c => c.Title)
                .HasMaxLength(20);

            builder
                .Property(c => c.Description)
                .HasMaxLength(100);

            builder
                .Property(c => c.ImagePath)
                .HasMaxLength(128);

            builder
                .Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            builder
                .HasOne(cour => cour.Category)
                .WithMany(cat => cat.Courses)
                .HasForeignKey(cour => cour.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
