namespace AgroLink.Application.Features.VoiceCommands.DTOs;

public record VoiceCommandResultDto(
    string Intent,
    double Confidence,
    VoiceCommandEntitiesDto? Entities,
    string RawTranscription
);
