using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalBcsReadingConfiguration : IEntityTypeConfiguration<AnimalBcsReading>
{
    public void Configure(EntityTypeBuilder<AnimalBcsReading> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(e => e.RawAiResponse).HasColumnType("text");

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.BcsReadings)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(e => e.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AnimalId, e.CreatedAt });
    }
}
