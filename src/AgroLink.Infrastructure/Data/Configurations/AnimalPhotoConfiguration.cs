using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalPhotoConfiguration : IEntityTypeConfiguration<AnimalPhoto>
{
    public void Configure(EntityTypeBuilder<AnimalPhoto> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UriRemote).IsRequired().HasMaxLength(500);

        builder.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);

        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Description).HasMaxLength(200);

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.Photos)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.AnimalId);
        builder.HasIndex(e => e.AnimalId).IsUnique().HasFilter("\"IsProfile\" = true");
    }
}
