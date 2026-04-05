using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class MedicationRuleConfiguration : IEntityTypeConfiguration<MedicationRule>
{
    public void Configure(EntityTypeBuilder<MedicationRule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Species).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SymptomTags).HasMaxLength(500);
        builder.Property(x => x.DoseFormula).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Contraindications).HasMaxLength(2000);
        builder.Property(x => x.WeightMin).HasPrecision(10, 2);
        builder.Property(x => x.WeightMax).HasPrecision(10, 2);

        builder.HasIndex(x => new { x.Species, x.Active });

        builder
            .HasOne(x => x.Medication)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
