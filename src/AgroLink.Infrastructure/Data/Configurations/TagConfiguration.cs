using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CanonicalName).IsRequired().HasMaxLength(24);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(24);
        builder.Property(e => e.ColorToken).HasMaxLength(32);

        builder.HasIndex(e => new { e.FarmId, e.CanonicalName }).IsUnique();

        builder
            .HasOne(e => e.Farm)
            .WithMany()
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
