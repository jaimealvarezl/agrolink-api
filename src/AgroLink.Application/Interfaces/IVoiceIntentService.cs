using AgroLink.Application.Features.VoiceCommands.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IVoiceIntentService
{
    Task<string?> ExtractIntentAsync(
        string transcript,
        FarmRosterDto roster,
        CancellationToken ct = default
    );
}
