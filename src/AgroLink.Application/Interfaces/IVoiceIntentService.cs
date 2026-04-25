namespace AgroLink.Application.Interfaces;

public interface IVoiceIntentService
{
    Task<string?> ExtractIntentAsync(string transcript, CancellationToken ct = default);
}
