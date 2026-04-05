using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ClinicalCaseEventConfiguration : IEntityTypeConfiguration<ClinicalCaseEvent>
{
    public void Configure(EntityTypeBuilder<ClinicalCaseEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.RawPayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.StructuredDataJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ClinicalCaseId, x.CreatedAt });

        builder
            .HasOne(x => x.ClinicalCase)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.ClinicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
