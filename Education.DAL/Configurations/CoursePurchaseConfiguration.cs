using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class CoursePurchaseConfiguration : IEntityTypeConfiguration<CoursePurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<CoursePurchaseEntity> builder)
        {
            builder.ToTable("CoursePurchase");

            builder
                .HasOne(p => p.Course)
                .WithMany(c => c.Purchases)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
