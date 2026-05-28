using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalTagConfiguration : IEntityTypeConfiguration<AnimalTag>
{
    public void Configure(EntityTypeBuilder<AnimalTag> builder)
    {
        builder.HasKey(e => new { e.AnimalId, e.TagId });

        builder
            .HasOne(e => e.Animal)
            .WithMany(a => a.AnimalTags)
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Tag)
            .WithMany(t => t.AnimalTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
