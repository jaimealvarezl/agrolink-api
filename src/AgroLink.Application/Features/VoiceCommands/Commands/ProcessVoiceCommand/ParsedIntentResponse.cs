namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;

public record ParsedIntentResponse(
    string Intent,
    double Confidence,
    int? AnimalId,
    int? LotId,
    int? TargetPaddockId,
    int? MotherId,
    string? Sex,
    string? NewbornEarTag,
    string? NoteText
)
{
    public ParsedIntentResponse()
        : this("unknown", 0.0, null, null, null, null, null, null, null) { }
}
