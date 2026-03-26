using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Reason).HasMaxLength(500);

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.Movements)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.Movements)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.FromLot)
            .WithMany()
            .HasForeignKey(e => e.FromLotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(e => e.ToLot)
            .WithMany()
            .HasForeignKey(e => e.ToLotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.AnimalId);
    }
}
