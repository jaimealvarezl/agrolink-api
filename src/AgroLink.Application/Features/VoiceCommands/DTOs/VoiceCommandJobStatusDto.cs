using System.Text.Json;

namespace AgroLink.Application.Features.VoiceCommands.DTOs;

public record VoiceCommandJobStatusDto(string Status, VoiceCommandResultDto? Result, string? Error);

public record VoiceCommandResultDto(
    string Intent,
    double Confidence,
    JsonElement Entities,
    string RawTranscription
);
