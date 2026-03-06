using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(255);

        builder.HasQueryFilter(e => e.IsActive);

        // Removed IsUnique from UserId since an owner can exist in multiple farms
        builder.HasIndex(e => e.UserId);

        // Composite unique index for Name and FarmId
        builder.HasIndex(e => new { e.Name, e.FarmId }).IsUnique();

        // Relationship with Farm
        builder
            .HasOne(e => e.Farm)
            .WithMany()
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
