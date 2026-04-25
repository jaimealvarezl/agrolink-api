namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;

public record ParsedIntentResponse(
    string Intent = "unknown",
    double Confidence = 0.0,
    int? AnimalId = null,
    int? LotId = null,
    int? TargetPaddockId = null,
    int? MotherId = null,
    string? Sex = null,
    string? NoteText = null,
    string? AnimalName = null,
    string? EarTag = null,
    string? Color = null,
    string? BirthDate = null,
    string[]? OwnerNames = null
);
