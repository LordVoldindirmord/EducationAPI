using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class ModuleConfiguration : IEntityTypeConfiguration<ModuleEntity>
    {
        public void Configure(EntityTypeBuilder<ModuleEntity> builder)
        {
            builder.HasKey(m => m.Id);

            builder
                .Property(m => m.Title)
                .HasMaxLength(20);

            builder
                .Property(m => m.Description)
                .HasMaxLength(128);

            builder
                .Property(m => m.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder
                .HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индекс для выборки модулей курса по порядку
            // Уникальная пара: курс + порядковый номер модуля
            builder
                .HasIndex(m => new { m.CourseId, m.OrderNumber })
                .IsUnique();
        }
    }
}