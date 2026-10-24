using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder
                .Property(u => u.FirstName)
                .HasMaxLength(50);

            builder
                .Property(u => u.LastName)
                .HasMaxLength(50);

            builder
                .Property(u => u.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        }
    }
}