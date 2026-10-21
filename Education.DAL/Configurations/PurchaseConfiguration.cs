using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class PurchaseConfiguration : IEntityTypeConfiguration<PurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<PurchaseEntity> builder)
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
                .Property(p => p.PurchaseType)
                .HasConversion(
                    s => ConvertPurchasesTypeToString(s),  // enum -> БД
                    s => ConvertStringtoPurchasesType(s))  // БД -> enum
                .HasMaxLength(20);

            builder
                .Property(p => p.CompletedAt)
                .HasColumnType("timestamp with time zone");

            builder
                .HasOne(p => p.User)
                .WithMany(u => u.Purchases)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.UseTptMappingStrategy();
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

        private static string ConvertPurchasesTypeToString(PurchaseType type) => type switch
        {
            PurchaseType.Course => "course",
            PurchaseType.Role => "role",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип покупки"),
        };

        private static PurchaseType ConvertStringtoPurchasesType(string value) => value switch
        {
            "course" => PurchaseType.Course,
            "role" => PurchaseType.Role,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестный тип покупки"),
        };
    }
}
