namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;

// Shape returned by GPT-4o: raw text mentions, no IDs
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

// Shape after server-side entity resolution: IDs + original mentions for display names
public record ResolvedIntentResponse(
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
    string[]? OwnerNames = null,
    string? AnimalMention = null,
    string? LotMention = null,
    string? TargetPaddockMention = null,
    string? MotherMention = null
);
