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
        builder
            .HasOne(e => e.Farm)
            .WithMany(f => f.Paddocks)
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.Name);
    }
}
