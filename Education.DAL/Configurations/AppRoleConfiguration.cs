using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class AppRoleConfiguration : IEntityTypeConfiguration<AppRoleEntity>
    {
        public void Configure(EntityTypeBuilder<AppRoleEntity> builder)
        {
            builder
                .Property(r => r.RoleDescription)
                .HasMaxLength(256);

            builder
                .Property(r => r.Price)
                .HasPrecision(18, 2);
        }
    }
}