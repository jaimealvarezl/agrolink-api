using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ClinicalRecommendationConfiguration : IEntityTypeConfiguration<ClinicalRecommendation>
{
    public void Configure(EntityTypeBuilder<ClinicalRecommendation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecommendationSource).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.AdviceText).IsRequired().HasMaxLength(5000);
        builder.Property(x => x.Disclaimer).IsRequired().HasMaxLength(500);
        builder.Property(x => x.RawModelResponse).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ClinicalCaseId, x.CreatedAt });

        builder
            .HasOne(x => x.ClinicalCase)
            .WithMany(x => x.Recommendations)
            .HasForeignKey(x => x.ClinicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
