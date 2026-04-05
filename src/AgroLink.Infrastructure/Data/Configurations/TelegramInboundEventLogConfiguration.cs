using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroLink.Infrastructure.Data.Configurations;

public class TelegramInboundEventLogConfiguration
    : IEntityTypeConfiguration<TelegramInboundEventLog>
{
    public void Configure(EntityTypeBuilder<TelegramInboundEventLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RawPayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.ProcessingStatus).IsRequired().HasMaxLength(50);

        builder.HasIndex(x => x.TelegramUpdateId).IsUnique();
        builder.HasIndex(x => new { x.ChatId, x.CreatedAt });
    }
}
