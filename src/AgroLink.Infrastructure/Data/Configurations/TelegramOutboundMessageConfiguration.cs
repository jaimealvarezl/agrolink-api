using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class TelegramOutboundMessageConfiguration
    : IEntityTypeConfiguration<TelegramOutboundMessage>
{
    public void Configure(EntityTypeBuilder<TelegramOutboundMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DeliveryStatus).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.ChatId, x.CreatedAt });

        builder
            .HasOne(x => x.ClinicalCase)
            .WithMany()
            .HasForeignKey(x => x.ClinicalCaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
