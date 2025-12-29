using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class FarmMemberConfiguration : IEntityTypeConfiguration<FarmMember>
{
    public void Configure(EntityTypeBuilder<FarmMember> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(50);

        builder
            .HasOne(e => e.Farm)
            .WithMany(f => f.Members)
            .HasForeignKey(e => e.FarmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.FarmMembers)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.FarmId, e.UserId }).IsUnique();
    }
}
