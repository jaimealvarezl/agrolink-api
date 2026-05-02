namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommandInline;

public record ParsedIntentResponse(
    string Intent = "unknown",
    double Confidence = 0.0,
    string? AnimalMention = null,
    string? LotMention = null,
    string? TargetPaddockMention = null,
    string? MotherMention = null,
    string? Sex = null,
    string? NoteText = null,
    string? AnimalName = null,
    string? EarTag = null,
    string? Color = null,
    string? BirthDate = null,
    string[]? OwnerNames = null
);
