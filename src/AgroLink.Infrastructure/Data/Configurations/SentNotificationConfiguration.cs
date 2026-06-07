using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.ToTable("SentNotifications");

        builder.HasKey(e => e.Id);

        builder
            .Property(e => e.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(e => e.ExpectedDueDate).IsRequired();

        builder
            .HasIndex(e => new
            {
                e.AnimalId,
                e.NotificationType,
                e.ExpectedDueDate,
            })
            .IsUnique();

        builder
            .HasOne(e => e.Animal)
            .WithMany()
            .HasForeignKey(e => e.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
