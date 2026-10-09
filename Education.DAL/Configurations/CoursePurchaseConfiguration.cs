using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class CoursePurchaseConfiguration : IEntityTypeConfiguration<CoursePurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<CoursePurchaseEntity> builder)
        {
            builder.HasKey(p => p.Id);

            builder
                .Property(p => p.PricePaid)
                .HasPrecision(18, 2);

            builder
                .Property(p => p.Currency)
                .HasMaxLength(3);

            builder
                .Property(p => p.TransactionId)
                .HasMaxLength(128);

            builder
                .Property(p => p.PaymentMethod)
                .HasMaxLength(20);

            builder
                .Property(p => p.Status)
                .HasConversion(
                    s => ConvertStatusToString(s),  // enum -> БД
                    s => ConvertStringToStatus(s))  // БД -> enum
                .HasMaxLength(20);

            builder
                .Property(p => p.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .Property(p => p.CompletedAt)
                .HasColumnType("timestamp with time zone");

            builder
                .HasOne(p => p.User)
                .WithMany(u => u.CoursePurchases)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(p => p.Course)
                .WithMany(c => c.Purchases)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasIndex(p => new { p.UserId, p.CourseId })
                .IsUnique();
        }

        private static string ConvertStatusToString(PurchaseStatus status) => status switch
        {
            PurchaseStatus.Pending => "pending",
            PurchaseStatus.Completed => "completed",
            PurchaseStatus.Failed => "failed",
            PurchaseStatus.Refunded => "refunded",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Неизвестный статус покупки"),
        };

        private static PurchaseStatus ConvertStringToStatus(string value) => value switch
        {
            "pending" => PurchaseStatus.Pending,
            "completed" => PurchaseStatus.Completed,
            "failed" => PurchaseStatus.Failed,
            "refunded" => PurchaseStatus.Refunded,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестный статус покупки"),
        };
    }
}
