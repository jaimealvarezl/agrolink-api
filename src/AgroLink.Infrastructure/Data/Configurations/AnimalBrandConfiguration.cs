using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalBrandConfiguration : IEntityTypeConfiguration<AnimalBrand>
{
    public void Configure(EntityTypeBuilder<AnimalBrand> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.Brands)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.OwnerBrand)
            .WithMany(ob => ob.AnimalBrands)
            .HasForeignKey(e => e.OwnerBrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.AnimalId);
        builder.HasIndex(e => e.OwnerBrandId);
    }
}
