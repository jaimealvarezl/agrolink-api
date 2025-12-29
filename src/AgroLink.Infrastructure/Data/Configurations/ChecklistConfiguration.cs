using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ScopeType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.Checklists)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new
        {
            e.ScopeType,
            e.ScopeId,
            e.Date,
        });
    }
}
