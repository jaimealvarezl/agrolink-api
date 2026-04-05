using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ClinicalCaseConfiguration : IEntityTypeConfiguration<ClinicalCase>
{
    public void Configure(EntityTypeBuilder<ClinicalCase> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EarTag).HasMaxLength(50);
        builder.Property(x => x.FarmReferenceText).HasMaxLength(200);
        builder.Property(x => x.AnimalReferenceText).HasMaxLength(200);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => new
        {
            x.FarmId,
            x.EarTag,
            x.OpenedAt,
        });
        builder.HasIndex(x => new
        {
            x.FarmId,
            x.AnimalId,
            x.OpenedAt,
        });

        builder
            .HasOne(x => x.Farm)
            .WithMany()
            .HasForeignKey(x => x.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Animal)
            .WithMany()
            .HasForeignKey(x => x.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
