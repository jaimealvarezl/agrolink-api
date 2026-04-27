using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;

public record ProcessVoiceCommandCommand(Guid JobId, int FarmId, int UserId) : IRequest;

public class ProcessVoiceCommandHandler(
    IVoiceCommandJobRepository jobRepository,
    IStorageService storageService,
    IFarmRosterService rosterService,
    IEntityResolutionService resolutionService,
    IExternalApiWorkerClient workerClient,
    IUnitOfWork unitOfWork,
    ILogger<ProcessVoiceCommandHandler> logger
) : IRequestHandler<ProcessVoiceCommandCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions EntitiesJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task Handle(
        ProcessVoiceCommandCommand request,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation(
            "[voice-process] START job={JobId} farm={FarmId}",
            request.JobId,
            request.FarmId
        );

        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            logger.LogWarning("Voice command job {JobId} not found, skipping.", request.JobId);
            return;
        }

        if (job.Status is "completed" or "failed")
        {
            logger.LogInformation(
                "Voice command job {JobId} already in terminal state '{Status}', skipping.",
                request.JobId,
                job.Status
            );
            return;
        }

        job.Status = "processing";
        jobRepository.Update(job);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "[voice-process] status=processing saved in {ElapsedMs}ms job={JobId}",
            sw.ElapsedMilliseconds,
            request.JobId
        );

        // Step 1: Download audio from S3
        var audioBytes = await storageService.GetFileBytesAsync(job.S3Key, cancellationToken);
        if (audioBytes == null || audioBytes.Length == 0)
        {
            logger.LogError(
                "Failed to download audio from S3 for job {JobId}, key {S3Key}.",
                request.JobId,
                job.S3Key
            );
            await FailJobAsync(job, "Failed to download audio from S3.", cancellationToken);
            return;
        }

        logger.LogInformation(
            "[voice-process] S3 download done ({Bytes}B) in {ElapsedMs}ms job={JobId}",
            audioBytes.Length,
            sw.ElapsedMilliseconds,
            request.JobId
        );

        // Step 2: Transcribe audio via Whisper
        var transcriptResult = await TranscribeAudioAsync(
            audioBytes,
            job.S3Key,
            request.JobId,
            cancellationToken
        );
        logger.LogInformation(
            "[voice-process] transcription done in {ElapsedMs}ms job={JobId} failed={Failed}",
            sw.ElapsedMilliseconds,
            request.JobId,
            transcriptResult.Failed
        );
        if (transcriptResult.Failed)
        {
            await FailJobAsync(job, transcriptResult.Error!, cancellationToken);
            return;
        }

        var transcript = transcriptResult.Value ?? string.Empty;
        logger.LogInformation(
            "[voice-process] transcript='{Transcript}' job={JobId}",
            transcript,
            request.JobId
        );

        // Step 3: Empty transcript → unknown intent, no GPT-4o call
        if (string.IsNullOrWhiteSpace(transcript))
        {
            logger.LogInformation(
                "Empty transcript for job {JobId}, marking as unknown.",
                request.JobId
            );
            await CompleteJobAsync(job, "unknown", 0.0, transcript, null, cancellationToken);
            await TryDeleteAudioAsync(job.S3Key);
            return;
        }

        // Step 4: Extract raw intent via GPT-4o (5-second timeout, no roster)
        using var intentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        intentCts.CancelAfter(TimeSpan.FromSeconds(5));

        ParsedIntentResponse parsed;
        try
        {
            parsed = await ExtractIntentAsync(transcript, request.JobId, intentCts.Token);
            logger.LogInformation(
                "[voice-process] intent extracted in {ElapsedMs}ms job={JobId} intent={Intent} confidence={Confidence}",
                sw.ElapsedMilliseconds,
                request.JobId,
                parsed.Intent,
                parsed.Confidence
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("GPT-4o intent extraction timed out for job {JobId}.", request.JobId);
            await FailJobAsync(job, "Intent extraction timed out.", cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Intent extraction failed for job {JobId}.", request.JobId);
            await FailJobAsync(job, "Intent extraction failed.", cancellationToken);
            return;
        }

        // Step 5: Server-side entity resolution (mentions → IDs) + roster load
        // Sequential to avoid concurrent DbContext operations on the same scoped instance
        var resolution = await resolutionService.ResolveAsync(
            request.FarmId,
            parsed.AnimalMention,
            parsed.LotMention,
            parsed.TargetPaddockMention,
            parsed.MotherMention,
            cancellationToken
        );
        var roster = await rosterService.GetRosterAsync(request.FarmId, cancellationToken);
        logger.LogInformation(
            "[voice-process] resolution+roster done in {ElapsedMs}ms job={JobId}",
            sw.ElapsedMilliseconds,
            request.JobId
        );

        var resolved = BuildResolvedIntent(parsed, resolution);

        // Step 6: Server-side entity validation against roster
        var validated = VoiceIntentValidator.Validate(resolved, roster);
        logger.LogInformation(
            "[voice-process] validation done intent={Intent} confidence={Confidence} job={JobId}",
            validated.Intent,
            validated.Confidence,
            request.JobId
        );

        // Step 7: Persist result
        await CompleteJobAsync(
            job,
            validated.Intent,
            validated.Confidence,
            transcript,
            validated,
            cancellationToken
        );
        await TryDeleteAudioAsync(job.S3Key);
        logger.LogInformation(
            "[voice-process] DONE total={ElapsedMs}ms job={JobId}",
            sw.ElapsedMilliseconds,
            request.JobId
        );
    }

    private async Task<Result<string>> TranscribeAudioAsync(
        byte[] audioBytes,
        string s3Key,
        Guid jobId,
        CancellationToken ct
    )
    {
        var fileName = Path.GetFileName(s3Key);
        var mimeType = ResolveMimeType(fileName);
        var base64 = Convert.ToBase64String(audioBytes);

        var payload = new TranscribeVoiceAudioPayload(base64, fileName, mimeType);
        var request = new ExternalWorkerRequest(
            jobId.ToString(),
            ExternalWorkerOperations.TranscribeVoiceAudio,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        ExternalWorkerResponse response;
        try
        {
            response = await workerClient.ExecuteAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TranscribeVoiceAudio Lambda call failed for job {JobId}.", jobId);
            return Result<string>.Fail("Transcription service unavailable.");
        }

        if (!response.Success || response.Result is null)
        {
            logger.LogWarning(
                "TranscribeVoiceAudio returned failure for job {JobId}: {Error}",
                jobId,
                response.Error
            );
            return Result<string>.Fail(response.Error ?? "Transcription failed.");
        }

        var text = response.Result.Value.TryGetProperty("text", out var textEl)
            ? textEl.GetString() ?? string.Empty
            : string.Empty;

        return Result<string>.Ok(text);
    }

    private async Task<ParsedIntentResponse> ExtractIntentAsync(
        string transcript,
        Guid jobId,
        CancellationToken ct
    )
    {
        var payload = new ExtractVoiceIntentPayload(transcript);
        var request = new ExternalWorkerRequest(
            jobId.ToString(),
            ExternalWorkerOperations.ExtractVoiceIntent,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await workerClient.ExecuteAsync(request, ct);

        if (!response.Success || response.Result is null)
        {
            throw new InvalidOperationException(
                $"ExtractVoiceIntent failed for job {jobId}: {response.Error ?? "no result"}"
            );
        }

        try
        {
            return response.Result.Value.Deserialize<ParsedIntentResponse>(JsonOptions)
                ?? new ParsedIntentResponse();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Malformed intent JSON from GPT-4o for job {JobId}. Defaulting to unknown.",
                jobId
            );
            return new ParsedIntentResponse();
        }
    }

    private static ResolvedIntentResponse BuildResolvedIntent(
        ParsedIntentResponse parsed,
        EntityResolutionResult resolution
    )
    {
        return new ResolvedIntentResponse(
            parsed.Intent,
            parsed.Confidence,
            resolution.AnimalId,
            resolution.LotId,
            resolution.TargetPaddockId,
            resolution.MotherId,
            parsed.Sex,
            parsed.NoteText,
            parsed.AnimalName,
            parsed.EarTag,
            parsed.Color,
            parsed.BirthDate,
            parsed.OwnerNames
        );
    }

    private async Task CompleteJobAsync(
        VoiceCommandJob job,
        string intent,
        double confidence,
        string transcript,
        ResolvedIntentResponse? resolved,
        CancellationToken ct
    )
    {
        var entities = BuildEntitiesElement(resolved);
        var result = new VoiceCommandResultDto(intent, confidence, entities, transcript);

        job.Status = "completed";
        job.ResultJson = JsonSerializer.Serialize(result, JsonOptions);
        job.CompletedAt = DateTime.UtcNow;

        jobRepository.Update(job);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Voice command job {JobId} completed with intent '{Intent}', confidence {Confidence}.",
            job.Id,
            intent,
            confidence
        );
    }

    private async Task FailJobAsync(VoiceCommandJob job, string error, CancellationToken ct)
    {
        job.Status = "failed";
        job.ErrorMessage = error.Length > 500 ? error[..500] : error;
        job.CompletedAt = DateTime.UtcNow;

        jobRepository.Update(job);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogWarning("Voice command job {JobId} failed: {Error}", job.Id, error);
    }

    private async Task TryDeleteAudioAsync(string s3Key)
    {
        try
        {
            await storageService.DeleteFileAsync(s3Key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete audio from S3 at key {S3Key}.", s3Key);
        }
    }

    private static JsonElement BuildEntitiesElement(ResolvedIntentResponse? resolved)
    {
        if (resolved == null)
        {
            return JsonSerializer.SerializeToElement(new { }, EntitiesJsonOptions);
        }

        return JsonSerializer.SerializeToElement(
            new
            {
                resolved.AnimalId,
                resolved.LotId,
                resolved.TargetPaddockId,
                resolved.MotherId,
                resolved.Sex,
                resolved.NoteText,
                resolved.AnimalName,
                resolved.EarTag,
                resolved.Color,
                resolved.BirthDate,
                OwnerNames = resolved.OwnerNames is { Length: > 0 } ? resolved.OwnerNames : null,
            },
            EntitiesJsonOptions
        );
    }

    private static string? ResolveMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => "audio/mpeg",
            ".mp4" => "audio/mp4",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            ".ogg" => "audio/ogg",
            _ => null,
        };
    }

    private record Result<T>
    {
        public bool Failed { get; private init; }
        public T? Value { get; private init; }
        public string? Error { get; private init; }

        public static Result<T> Ok(T value)
        {
            return new Result<T> { Value = value };
        }

        public static Result<T> Fail(string error)
        {
            return new Result<T> { Failed = true, Error = error };
        }
    }
}
