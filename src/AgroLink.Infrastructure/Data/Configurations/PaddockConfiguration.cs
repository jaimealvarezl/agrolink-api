using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class PaddockConfiguration : IEntityTypeConfiguration<Paddock>
{
    public void Configure(EntityTypeBuilder<Paddock> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Area).HasPrecision(18, 2);
        builder.Property(e => e.AreaType).HasMaxLength(50);
        builder
            .HasOne(e => e.Farm)
            .WithMany(f => f.Paddocks)
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.Name);
        builder.HasQueryFilter(e => e.Farm.IsActive);
    }
}
