using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
    {
        public void Configure(EntityTypeBuilder<CategoryEntity> builder)
        {
            builder.HasKey(c => c.Id);

            builder
                .Property(c => c.Name)
                .HasMaxLength(20);

            builder
                .Property(c => c.Description)
                .HasMaxLength(100);

            // Нужно подумать насчет дефолта
            builder
                .Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        }
    }
}
