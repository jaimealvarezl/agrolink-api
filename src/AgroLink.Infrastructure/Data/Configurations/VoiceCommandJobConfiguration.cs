using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class VoiceCommandJobConfiguration : IEntityTypeConfiguration<VoiceCommandJob>
{
    public void Configure(EntityTypeBuilder<VoiceCommandJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.S3Key).HasMaxLength(500).IsRequired();
        builder.Property(j => j.Status).HasMaxLength(20).IsRequired();
        builder.Property(j => j.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.CreatedAt);

        builder
            .HasOne(j => j.Farm)
            .WithMany()
            .HasForeignKey(j => j.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
