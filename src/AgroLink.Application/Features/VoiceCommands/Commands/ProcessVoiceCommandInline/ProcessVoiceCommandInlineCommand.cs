using System.Diagnostics;
using System.Text.Json;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommandInline;

public record ProcessVoiceCommandInlineCommand(
    int FarmId,
    int UserId,
    Stream AudioStream,
    string ContentType,
    long Size
) : IRequest<VoiceCommandResultDto>;

public class ProcessVoiceCommandInlineHandler(
    IEntityResolutionService resolutionService,
    IExternalApiWorkerClient workerClient,
    ILogger<ProcessVoiceCommandInlineHandler> logger
) : IRequestHandler<ProcessVoiceCommandInlineCommand, VoiceCommandResultDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<VoiceCommandResultDto> Handle(
        ProcessVoiceCommandInlineCommand request,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        var jobId = Guid.NewGuid();

        logger.LogInformation(
            "[voice] START farm={FarmId} user={UserId} size={Size}B",
            request.FarmId,
            request.UserId,
            request.Size
        );

        using var ms = new MemoryStream((int)request.Size);
        await request.AudioStream.CopyToAsync(ms, cancellationToken);
        var audioBytes = ms.ToArray();

        var fileName = $"{jobId}{ExtensionForContentType(request.ContentType)}";

        var transcript = await TranscribeAsync(audioBytes, fileName, jobId, cancellationToken);
        logger.LogInformation(
            "[voice] transcript='{Transcript}' in {Ms}ms",
            transcript,
            sw.ElapsedMilliseconds
        );

        if (string.IsNullOrWhiteSpace(transcript))
        {
            logger.LogInformation("[voice] empty transcript → unknown intent");
            return new VoiceCommandResultDto("unknown", 0.0, null, string.Empty);
        }

        using var intentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        intentCts.CancelAfter(TimeSpan.FromSeconds(5));

        ParsedIntentResponse parsed;
        try
        {
            parsed = await ExtractIntentAsync(transcript, jobId, intentCts.Token);
            logger.LogInformation(
                "[voice] intent={Intent} confidence={Confidence} in {Ms}ms",
                parsed.Intent,
                parsed.Confidence,
                sw.ElapsedMilliseconds
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Intent extraction timed out.");
        }

        var resolution = await resolutionService.ResolveAsync(
            request.FarmId,
            parsed.AnimalMention,
            parsed.LotMention,
            parsed.TargetPaddockMention,
            parsed.MotherMention,
            parsed.OwnerNames,
            cancellationToken
        );
        logger.LogInformation("[voice] resolution done in {Ms}ms", sw.ElapsedMilliseconds);

        var (entities, adjustedConfidence) = BuildEntities(parsed, resolution);
        var finalIntent = Math.Round(adjustedConfidence, 4) < 0.5 ? "unknown" : parsed.Intent;
        var finalConfidence = finalIntent == "unknown" ? 0.0 : adjustedConfidence;

        logger.LogInformation("[voice] DONE total={Ms}ms", sw.ElapsedMilliseconds);

        return new VoiceCommandResultDto(finalIntent, finalConfidence, entities, transcript);
    }

    private async Task<string> TranscribeAsync(
        byte[] audioBytes,
        string fileName,
        Guid jobId,
        CancellationToken ct
    )
    {
        var mimeType = ResolveMimeType(fileName);
        var payload = new TranscribeVoiceAudioPayload(
            Convert.ToBase64String(audioBytes),
            fileName,
            mimeType
        );
        var workerRequest = new ExternalWorkerRequest(
            jobId.ToString(),
            ExternalWorkerOperations.TranscribeVoiceAudio,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await workerClient.ExecuteAsync(workerRequest, ct);

        if (!response.Success || response.Result is null)
        {
            throw new InvalidOperationException(response.Error ?? "Transcription failed.");
        }

        return response.Result.Value.TryGetProperty("text", out var textEl)
            ? textEl.GetString() ?? string.Empty
            : string.Empty;
    }

    private async Task<ParsedIntentResponse> ExtractIntentAsync(
        string transcript,
        Guid jobId,
        CancellationToken ct
    )
    {
        var payload = new ExtractVoiceIntentPayload(transcript);
        var workerRequest = new ExternalWorkerRequest(
            jobId.ToString(),
            ExternalWorkerOperations.ExtractVoiceIntent,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await workerClient.ExecuteAsync(workerRequest, ct);

        if (!response.Success || response.Result is null)
        {
            throw new InvalidOperationException(
                $"ExtractVoiceIntent failed: {response.Error ?? "no result"}"
            );
        }

        try
        {
            return response.Result.Value.Deserialize<ParsedIntentResponse>(JsonOptions)
                ?? new ParsedIntentResponse();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed intent JSON from GPT-4o, defaulting to unknown.");
            return new ParsedIntentResponse();
        }
    }

    private static (VoiceCommandEntitiesDto entities, double confidence) BuildEntities(
        ParsedIntentResponse parsed,
        EntityResolutionResult resolution
    )
    {
        var confidence = parsed.Confidence;
        if (parsed.AnimalMention != null && resolution.Animal == null)
        {
            confidence -= 0.2;
        }

        if (parsed.MotherMention != null && resolution.Mother == null)
        {
            confidence -= 0.2;
        }

        if (parsed.LotMention != null && resolution.Lot == null)
        {
            confidence -= 0.2;
        }

        if (parsed.TargetPaddockMention != null && resolution.TargetPaddock == null)
        {
            confidence -= 0.2;
        }

        var owners = resolution.Owners is { Count: > 0 }
            ? resolution.Owners.Select(o => new VoiceCommandOwnerSummary(o.Id, o.Name)).ToArray()
            : null;

        var entities = new VoiceCommandEntitiesDto(
            resolution.Animal != null
                ? new VoiceCommandAnimalSummary(
                    resolution.Animal.Id,
                    resolution.Animal.Name,
                    resolution.Animal.TagVisual,
                    resolution.Animal.Cuia,
                    resolution.Animal.Lot?.Name
                )
                : null,
            resolution.Mother != null
                ? new VoiceCommandAnimalSummary(
                    resolution.Mother.Id,
                    resolution.Mother.Name,
                    resolution.Mother.TagVisual,
                    resolution.Mother.Cuia,
                    resolution.Mother.Lot?.Name
                )
                : null,
            resolution.Lot != null
                ? new VoiceCommandLotSummary(
                    resolution.Lot.Id,
                    resolution.Lot.Name,
                    resolution.Lot.Paddock?.Name
                )
                : null,
            resolution.TargetPaddock != null
                ? new VoiceCommandPaddockSummary(
                    resolution.TargetPaddock.Id,
                    resolution.TargetPaddock.Name
                )
                : null,
            ParseSex(parsed.Sex),
            parsed.NoteText,
            parsed.AnimalName,
            parsed.EarTag,
            parsed.Color,
            ParseDate(parsed.BirthDate),
            owners
        );

        return (entities, confidence);
    }

    private static Sex? ParseSex(string? raw)
    {
        return raw?.ToLowerInvariant().Trim() switch
        {
            "male" or "macho" or "m" => Sex.Male,
            "female" or "hembra" or "f" => Sex.Female,
            _ => null,
        };
    }

    private static DateOnly? ParseDate(string? raw)
    {
        return DateOnly.TryParse(raw, out var date) ? date : null;
    }

    private static string? ResolveMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".mp4" or ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            ".ogg" => "audio/ogg",
            _ => null,
        };
    }

    private static string ExtensionForContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "audio/mpeg" => ".mp3",
            "audio/mp4" or "audio/m4a" or "audio/x-m4a" => ".m4a",
            "audio/wav" or "audio/wave" => ".wav",
            "audio/webm" => ".webm",
            "audio/ogg" => ".ogg",
            _ => ".bin",
        };
    }
}
