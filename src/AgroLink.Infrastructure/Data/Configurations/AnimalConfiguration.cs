using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Tag).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Color).HasMaxLength(100);
        builder.Property(e => e.Breed).HasMaxLength(100);
        builder.Property(e => e.Sex).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50);

        builder
            .HasOne(e => e.Lot)
            .WithMany(l => l.Animals)
            .HasForeignKey(e => e.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Mother)
            .WithMany(a => a.Children)
            .HasForeignKey(e => e.MotherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.Father)
            .WithMany()
            .HasForeignKey(e => e.FatherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Tag).IsUnique();
        builder.HasIndex(e => e.Name);
    }
}
