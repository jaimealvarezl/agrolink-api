using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class MedicationImageConfiguration : IEntityTypeConfiguration<MedicationImage>
{
    public void Configure(EntityTypeBuilder<MedicationImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Label).HasMaxLength(150);

        builder
            .HasOne(x => x.Medication)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
