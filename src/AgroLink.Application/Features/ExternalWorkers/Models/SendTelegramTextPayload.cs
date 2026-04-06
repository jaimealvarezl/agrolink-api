namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record SendTelegramTextPayload(long ChatId, string Text);
