using AgroLink.Application.Features.VoiceCommands.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IFarmRosterService
{
    Task<FarmRosterDto> GetRosterAsync(int farmId, CancellationToken ct = default);
}
