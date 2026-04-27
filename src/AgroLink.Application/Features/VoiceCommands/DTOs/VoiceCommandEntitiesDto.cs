namespace AgroLink.Application.Features.VoiceCommands.DTOs;

public record VoiceCommandEntitiesDto(
    VoiceCommandAnimalSummary? Animal,
    VoiceCommandAnimalSummary? Mother,
    VoiceCommandLotSummary? Lot,
    VoiceCommandPaddockSummary? TargetPaddock,
    string? Sex,
    string? NoteText,
    string? AnimalName,
    string? EarTag,
    string? Color,
    string? BirthDate,
    string[]? OwnerNames
);

public record VoiceCommandAnimalSummary(
    int Id,
    string Name,
    string? EarTag,
    string? Cuia,
    string? LotName
);

public record VoiceCommandLotSummary(int Id, string Name, string? PaddockName);

public record VoiceCommandPaddockSummary(int Id, string Name);
