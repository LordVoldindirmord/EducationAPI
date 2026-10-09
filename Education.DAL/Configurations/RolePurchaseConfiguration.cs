using Education.Domain.Entity;
using Education.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class RolePurchaseConfiguration : IEntityTypeConfiguration<RolePurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<RolePurchaseEntity> builder)
        {
            builder.HasKey(r => r.Id);

            builder
                .Property(r => r.PricePaid)
                .HasPrecision(18, 2);

            builder
                .Property(r => r.Currency)
                .HasMaxLength(3);

            builder
                .Property(r => r.TransactionId)
                .HasMaxLength(128);

            builder
                .Property(r => r.PaymentMethod)
                .HasMaxLength(20);

            builder
                .Property(r => r.Status)
                .HasConversion(
                    r => ConvertPurchaseStatusToString(r),  // enum -> БД
                    r => ConvertStringToPurchaseStatus(r))  // БД -> enum
                .HasMaxLength(20);

            builder
                .Property(r => r.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .Property(r => r.CompletedAt)
                .HasColumnType("timestamp with time zone");

            builder
                .HasOne(r => r.User)
                .WithMany(u => u.RolePurchases)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(rp => rp.Role)
                .WithMany(r => r.Purchases)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static string ConvertPurchaseStatusToString(PurchaseStatus type) => type switch
        {
            PurchaseStatus.Pending => "pending",
            PurchaseStatus.Completed => "completed",
            PurchaseStatus.Failed => "failed",
            PurchaseStatus.Refunded => "refunded",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип статуса оплаты"),
        };

        private static PurchaseStatus ConvertStringToPurchaseStatus(string value) => value switch
        {
            "pending" => PurchaseStatus.Pending,
            "completed" => PurchaseStatus.Completed,
            "failed" => PurchaseStatus.Failed,
            "refunded" => PurchaseStatus.Refunded,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестная тип оплаты в БД"),
        };
    }
}
