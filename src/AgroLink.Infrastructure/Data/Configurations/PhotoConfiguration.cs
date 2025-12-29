using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.UriLocal).IsRequired().HasMaxLength(500);
        builder.Property(e => e.UriRemote).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(200);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
