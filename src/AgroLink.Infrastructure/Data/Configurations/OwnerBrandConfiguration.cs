using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class OwnerBrandConfiguration : IEntityTypeConfiguration<OwnerBrand>
{
    public void Configure(EntityTypeBuilder<OwnerBrand> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RegistrationNumber).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.PhotoUrl).HasMaxLength(1000);

        builder.HasQueryFilter(e => e.IsActive && e.Owner.IsActive);

        builder
            .HasOne(e => e.Owner)
            .WithMany(o => o.OwnerBrands)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => new { e.OwnerId, e.RegistrationNumber }).IsUnique();
    }
}
