using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ReproductiveEventConfiguration : IEntityTypeConfiguration<ReproductiveEvent>
{
    public void Configure(EntityTypeBuilder<ReproductiveEvent> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.ReproductiveEvents)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Bull)
            .WithMany()
            .HasForeignKey(e => e.BullId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AnimalId, e.Date });
        builder.HasIndex(e => new
        {
            e.AnimalId,
            e.EventType,
            e.Date,
        });
    }
}
