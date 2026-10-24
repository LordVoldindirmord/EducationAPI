using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class UserProgressConfiguration : IEntityTypeConfiguration<UserProgressEntity>
    {
        public void Configure(EntityTypeBuilder<UserProgressEntity> builder)
        {
            builder.HasKey(up => up.Id);

            builder
                .Property(up => up.CompletedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .HasOne(up => up.Lesson)
                .WithMany(l => l.Progresses)
                .HasForeignKey(up => up.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(up => up.User)
                .WithMany(u => u.UserProgresses)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(up => new { up.UserId, up.LessonId })
                .IsUnique();
        }
    }
}