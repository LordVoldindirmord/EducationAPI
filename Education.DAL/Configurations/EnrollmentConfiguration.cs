using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class EnrollmentConfiguration : IEntityTypeConfiguration<EnrollmentEntity>
    {
        public void Configure(EntityTypeBuilder<EnrollmentEntity> builder)
        {
            builder.HasKey(e => e.Id);

            builder
                .Property(e => e.EnrolledAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();
        }
    }
}
