using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ClinicalAlertConfiguration : IEntityTypeConfiguration<ClinicalAlert>
{
    public void Configure(EntityTypeBuilder<ClinicalAlert> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AlertType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => new { x.ClinicalCaseId, x.CreatedAt });

        builder
            .HasOne(x => x.ClinicalCase)
            .WithMany(x => x.Alerts)
            .HasForeignKey(x => x.ClinicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
