using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalOwnerConfiguration : IEntityTypeConfiguration<AnimalOwner>
{
    public void Configure(EntityTypeBuilder<AnimalOwner> builder)
    {
        builder.HasKey(e => new { e.AnimalId, e.OwnerId });
        builder.Property(e => e.SharePercent).HasPrecision(5, 2);

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.AnimalOwners)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Owner)
            .WithMany(o => o.AnimalOwners)
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.Animal.LifeStatus != LifeStatus.Deleted);
    }
}
