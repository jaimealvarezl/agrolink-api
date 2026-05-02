using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FirebaseUid).HasMaxLength(128);
        builder.Property(e => e.PasswordHash).HasMaxLength(100);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(50);

        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.FirebaseUid).IsUnique();
    }
}
