using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Condition).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder
            .HasOne(e => e.Checklist)
            .WithMany(c => c.ChecklistItems)
            .HasForeignKey(e => e.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.ChecklistItems)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ChecklistId, e.AnimalId }).IsUnique();

        builder.HasQueryFilter(e => e.Animal.LifeStatus != LifeStatus.Deleted);
    }
}
