using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class CourseMemberConfiguration : IEntityTypeConfiguration<CourseMemberEntity>
    {
        public void Configure(EntityTypeBuilder<CourseMemberEntity> builder)
        {
            builder
                .HasKey(m => m.Id);

            builder
                .Property(m => m.Role)
                .HasConversion(
                    m => ConvertCourseRoleToString(m),  // enum -> БД
                    m => ConvertStringToCourseRole(m))  // БД -> enum
                .HasMaxLength(20);

            builder
                .Property(m => m.AddedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .HasOne(m => m.Course)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(m => m.User)
                .WithMany(u => u.CourseMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(m => new { m.CourseId, m.UserId })
                .IsUnique();
        }

        private static string ConvertCourseRoleToString(CourseRole role) => role switch
        {
            CourseRole.Owner => "owner",
            CourseRole.Manager => "manager",
            CourseRole.Teacher => "teacher",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Неизвестный тип роли"),
        };

        private static CourseRole ConvertStringToCourseRole(string value) => value switch
        {
            "owner" => CourseRole.Owner,
            "manager" => CourseRole.Manager,
            "teacher" => CourseRole.Teacher,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестная роль в БД"),
        };
    }
}