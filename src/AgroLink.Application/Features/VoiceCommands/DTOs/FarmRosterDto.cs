namespace AgroLink.Application.Features.VoiceCommands.DTOs;

public record FarmRosterDto(
    IReadOnlyList<AnimalRosterEntry> Animals,
    IReadOnlyList<LotRosterEntry> Lots
);

public record AnimalRosterEntry(
    int Id,
    string Name,
    string? EarTag,
    string? Cuia,
    int LotId,
    string LotName
);

public record LotRosterEntry(int Id, string Name, int PaddockId, string PaddockName);
