using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgroLink.Application.Features.VoiceCommands.DTOs;

public record VoiceCommandJobStatusDto(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        VoiceCommandResultDto? Result,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error
);

public record VoiceCommandResultDto(
    string Intent,
    double Confidence,
    JsonElement Entities,
    string RawTranscription
);
