using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalNoteConfiguration : IEntityTypeConfiguration<AnimalNote>
{
    public void Configure(EntityTypeBuilder<AnimalNote> builder)
    {
        builder.HasQueryFilter(e =>
            e.Animal.LifeStatus != LifeStatus.Deleted && e.Animal.Lot.Paddock.Farm.IsActive
        );

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Content).HasMaxLength(2000).IsRequired();

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.Notes)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.AnimalId);
    }
}
