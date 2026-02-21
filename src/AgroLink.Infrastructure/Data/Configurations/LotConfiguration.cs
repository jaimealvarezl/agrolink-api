using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);
        builder
            .HasOne(e => e.Paddock)
            .WithMany(p => p.Lots)
            .HasForeignKey(e => e.PaddockId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.Name);
        builder.HasQueryFilter(e => e.Paddock.Farm.IsActive);
    }
}
