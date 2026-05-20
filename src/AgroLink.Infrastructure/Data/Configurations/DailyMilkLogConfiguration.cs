using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class DailyMilkLogConfiguration : IEntityTypeConfiguration<DailyMilkLog>
{
    public void Configure(EntityTypeBuilder<DailyMilkLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalLiters).HasColumnType("numeric(8,2)").IsRequired();

        builder.Property(e => e.PricePerLiter).HasColumnType("numeric(10,4)");

        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => new { e.FarmId, e.Date }).IsUnique();

        builder
            .HasOne(e => e.Farm)
            .WithMany()
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
