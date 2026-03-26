using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalRetirementConfiguration : IEntityTypeConfiguration<AnimalRetirement>
{
    public void Configure(EntityTypeBuilder<AnimalRetirement> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder
            .HasOne(e => e.Animal)
            .WithOne(a => a.Retirement)
            .HasForeignKey<AnimalRetirement>(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.AnimalId).IsUnique();
        builder.HasIndex(e => e.UserId);
    }
}
