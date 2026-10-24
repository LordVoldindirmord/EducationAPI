using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class RolePurchaseConfiguration : IEntityTypeConfiguration<RolePurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<RolePurchaseEntity> builder)
        {
            builder.ToTable("RolePurchase");

            builder
                .HasOne(p => p.Role)
                .WithMany(r => r.Purchases)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}