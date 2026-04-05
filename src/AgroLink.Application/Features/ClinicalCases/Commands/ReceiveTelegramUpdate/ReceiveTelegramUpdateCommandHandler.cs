using System.Text.Json;
using System.Text.RegularExpressions;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;

public class ReceiveTelegramUpdateCommandHandler(
    ITelegramInboundEventLogRepository inboundEventLogRepository,
    ITelegramOutboundMessageRepository outboundMessageRepository,
    IClinicalCaseRepository clinicalCaseRepository,
    IClinicalCaseEventRepository clinicalCaseEventRepository,
    IClinicalRecommendationRepository clinicalRecommendationRepository,
    IClinicalExtractionService clinicalExtractionService,
    IFarmAnimalResolver farmAnimalResolver,
    IClinicalMedicationAdvisorService clinicalMedicationAdvisorService,
    IClinicalAudioTranscriptionService clinicalAudioTranscriptionService,
    IClinicalTextToSpeechService clinicalTextToSpeechService,
    ITelegramGateway telegramGateway,
    IUnitOfWork unitOfWork,
    ILogger<ReceiveTelegramUpdateCommandHandler> logger
) : IRequestHandler<ReceiveTelegramUpdateCommand, ReceiveTelegramUpdateResult>
{
    public async Task<ReceiveTelegramUpdateResult> Handle(
        ReceiveTelegramUpdateCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.RawUpdateJson))
        {
            return new ReceiveTelegramUpdateResult
            {
                Processed = false,
                Status = "Ignored",
                Message = "Payload is empty.",
            };
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(request.RawUpdateJson);
        }
        catch (JsonException)
        {
            return new ReceiveTelegramUpdateResult
            {
                Processed = false,
                Status = "InvalidPayload",
                Message = "Payload is not valid JSON.",
            };
        }

        using (document)
        {
            var root = document.RootElement;

            if (
                !root.TryGetProperty("update_id", out var updateIdElement)
                || !updateIdElement.TryGetInt64(out var updateId)
            )
            {
                return new ReceiveTelegramUpdateResult
                {
                    Processed = false,
                    Status = "InvalidPayload",
                    Message = "Missing update_id.",
                };
            }

            var existingInbound = await inboundEventLogRepository.GetByTelegramUpdateIdAsync(
                updateId,
                cancellationToken
            );
            if (existingInbound != null)
            {
                return new ReceiveTelegramUpdateResult
                {
                    Processed = false,
                    Status = "Duplicate",
                    Message = "Update already processed.",
                };
            }

            TryGetMessageElement(root, out var messageElement);
            var chatId = TryGetChatId(messageElement);
            var messageId = TryGetMessageId(messageElement);
            var incomingText = await ExtractIncomingTextAsync(messageElement, cancellationToken);

            var inboundLog = new TelegramInboundEventLog
            {
                TelegramUpdateId = updateId,
                ChatId = chatId,
                MessageId = messageId,
                RawPayloadJson = request.RawUpdateJson,
                Processed = false,
                ProcessingStatus = "Received",
            };
            await inboundEventLogRepository.AddAsync(inboundLog, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (messageElement.ValueKind == JsonValueKind.Undefined)
            {
                inboundLog.Processed = true;
                inboundLog.ProcessedAt = DateTime.UtcNow;
                inboundLog.ProcessingStatus = "IgnoredNoMessage";
                inboundEventLogRepository.Update(inboundLog);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ReceiveTelegramUpdateResult
                {
                    Processed = false,
                    Status = "IgnoredNoMessage",
                    Message = "No message payload to process.",
                };
            }

            if (IsStartCommand(incomingText))
            {
                inboundLog.Processed = true;
                inboundLog.ProcessedAt = DateTime.UtcNow;
                inboundLog.ProcessingStatus = "IgnoredStartCommand";
                inboundEventLogRepository.Update(inboundLog);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ReceiveTelegramUpdateResult
                {
                    Processed = false,
                    Status = "IgnoredStartCommand",
                    Message = "Telegram /start command ignored.",
                };
            }

            var extraction = await clinicalExtractionService.ExtractAsync(
                incomingText,
                cancellationToken
            );
            var resolution = await farmAnimalResolver.ResolveAsync(
                extraction.FarmReference,
                extraction.AnimalReference,
                extraction.EarTag,
                cancellationToken
            );

            if (resolution.Farm == null)
            {
                var farmMissingTemporaryCaseCode = GenerateTemporaryCaseCode(updateId, "FARM");
                var adviceWithoutFarm = await clinicalMedicationAdvisorService.GetAdviceAsync(
                    new ClinicalMedicationAdviceRequest
                    {
                        Country = "Nicaragua",
                        Species = "Ganado bovino",
                        FarmName = extraction.FarmReference ?? "granja no identificada",
                        AnimalReference = extraction.AnimalReference ?? "animal no identificado",
                        EarTag = extraction.EarTag ?? string.Empty,
                        SymptomsSummary = extraction.SymptomsSummary,
                        TranscriptText = incomingText,
                    },
                    cancellationToken
                );

                var recommendationMessageWithoutFarm = BuildRecommendationMessage(
                    extraction.AnimalReference ?? "animal no identificado",
                    extraction.EarTag,
                    adviceWithoutFarm,
                    true,
                    true,
                    farmMissingTemporaryCaseCode
                );

                await SendAndLogOutboundAsync(
                    chatId,
                    recommendationMessageWithoutFarm,
                    null,
                    $"update-{updateId}-recommendation-no-farm",
                    cancellationToken
                );
                await SendVoiceAndLogOutboundAsync(
                    chatId,
                    recommendationMessageWithoutFarm,
                    null,
                    $"update-{updateId}-recommendation-no-farm-voice",
                    cancellationToken
                );

                inboundLog.Processed = true;
                inboundLog.ProcessedAt = DateTime.UtcNow;
                inboundLog.ProcessingStatus = "RecommendationDeliveredWithoutFarm";
                inboundEventLogRepository.Update(inboundLog);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ReceiveTelegramUpdateResult
                {
                    Processed = true,
                    Status = "RecommendationDeliveredWithoutFarm",
                    Message =
                        $"Recommendation sent with temporary code {farmMissingTemporaryCaseCode}.",
                };
            }

            var farm = resolution.Farm!;
            var animal = resolution.Animal;
            var animalDisplay =
                animal?.Name ?? extraction.AnimalReference ?? "animal no identificado";
            var earTag = extraction.EarTag ?? animal?.TagVisual ?? animal?.Cuia;
            var animalMissingInDb = animal == null;
            var temporaryCaseCode = animalMissingInDb
                ? GenerateTemporaryCaseCode(updateId, "ANIMAL")
                : null;

            if (extraction.Intent == ClinicalMessageIntent.AnimalStatusRequest)
            {
                var latestCase =
                    animal != null
                        ? await clinicalCaseRepository.GetLatestByFarmAndAnimalAsync(
                            farm.Id,
                            animal.Id,
                            cancellationToken
                        )
                        : await clinicalCaseRepository.GetLatestByFarmAndReferenceAsync(
                            farm.Id,
                            earTag,
                            extraction.AnimalReference,
                            cancellationToken
                        );

                var statusMessage = BuildStatusMessage(farm.Name, animalDisplay, latestCase);
                await SendAndLogOutboundAsync(
                    chatId,
                    statusMessage,
                    latestCase?.Id,
                    $"update-{updateId}-status",
                    cancellationToken
                );
                await SendVoiceAndLogOutboundAsync(
                    chatId,
                    statusMessage,
                    latestCase?.Id,
                    $"update-{updateId}-status-voice",
                    cancellationToken
                );

                inboundLog.Processed = true;
                inboundLog.ProcessedAt = DateTime.UtcNow;
                inboundLog.ProcessingStatus = "StatusDelivered";
                inboundEventLogRepository.Update(inboundLog);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ReceiveTelegramUpdateResult
                {
                    Processed = true,
                    Status = "StatusDelivered",
                    Message = "Status response delivered.",
                };
            }

            var currentCase =
                animal != null
                    ? await clinicalCaseRepository.GetOpenCaseByFarmAndAnimalWithinDaysAsync(
                        farm.Id,
                        animal.Id,
                        7,
                        cancellationToken
                    )
                    : await clinicalCaseRepository.GetOpenCaseByFarmAndReferenceWithinDaysAsync(
                        farm.Id,
                        earTag,
                        extraction.AnimalReference,
                        7,
                        cancellationToken
                    );

            if (currentCase == null)
            {
                currentCase = new ClinicalCase
                {
                    FarmId = farm.Id,
                    AnimalId = animal?.Id,
                    EarTag = earTag,
                    FarmReferenceText = extraction.FarmReference,
                    AnimalReferenceText = BuildAnimalReferenceText(
                        extraction.AnimalReference ?? animal?.Name,
                        temporaryCaseCode
                    ),
                    State = ClinicalCaseState.NewCase,
                    RiskLevel = ClinicalRiskLevel.Low,
                    OpenedAt = DateTime.UtcNow,
                };
                await clinicalCaseRepository.AddAsync(currentCase, cancellationToken);
            }

            var caseEvent = new ClinicalCaseEvent
            {
                ClinicalCase = currentCase,
                EventType = currentCase.Events.Count == 0 ? "new_case_report" : "follow_up_report",
                RawPayloadJson = request.RawUpdateJson,
                Transcript = incomingText,
                StructuredDataJson = JsonSerializer.Serialize(extraction),
                Confidence = extraction.ConfidenceScore,
            };
            await clinicalCaseEventRepository.AddAsync(caseEvent, cancellationToken);

            var advice = await clinicalMedicationAdvisorService.GetAdviceAsync(
                new ClinicalMedicationAdviceRequest
                {
                    Country = "Nicaragua",
                    Species = "Ganado bovino",
                    FarmName = farm.Name,
                    AnimalReference = animalDisplay,
                    EarTag = earTag ?? string.Empty,
                    SymptomsSummary = extraction.SymptomsSummary,
                    TranscriptText = incomingText,
                },
                cancellationToken
            );

            var recommendation = new ClinicalRecommendation
            {
                ClinicalCase = currentCase,
                RecommendationSource = RecommendationSource.AiExploratory,
                AdviceText = advice.AdviceText,
                Disclaimer = advice.Disclaimer,
                RawModelResponse = advice.RawModelResponse,
            };
            await clinicalRecommendationRepository.AddAsync(recommendation, cancellationToken);

            currentCase.State = ClinicalCaseState.Recommended;
            currentCase.RiskLevel = advice.RiskLevel;
            currentCase.UpdatedAt = DateTime.UtcNow;
            clinicalCaseRepository.Update(currentCase);

            // Persist case graph first so outbound logs can reference a stable case id.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var recommendationMessage = BuildRecommendationMessage(
                animalDisplay,
                earTag,
                advice,
                animalMissingInDb,
                false,
                temporaryCaseCode
            );
            await SendAndLogOutboundAsync(
                chatId,
                recommendationMessage,
                currentCase.Id,
                $"update-{updateId}-recommendation",
                cancellationToken
            );
            await SendVoiceAndLogOutboundAsync(
                chatId,
                recommendationMessage,
                currentCase.Id,
                $"update-{updateId}-recommendation-voice",
                cancellationToken
            );

            inboundLog.Processed = true;
            inboundLog.ProcessedAt = DateTime.UtcNow;
            inboundLog.ProcessingStatus = "RecommendationDelivered";
            inboundEventLogRepository.Update(inboundLog);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ReceiveTelegramUpdateResult
            {
                Processed = true,
                Status = "RecommendationDelivered",
                Message = "Recommendation sent.",
            };
        }
    }

    private async Task SendAndLogOutboundAsync(
        long? chatId,
        string message,
        int? clinicalCaseId,
        string idempotencyKey,
        CancellationToken ct
    )
    {
        if (!chatId.HasValue)
        {
            logger.LogWarning("Telegram chat id is missing, outbound message was not sent.");
            return;
        }

        var existing = await outboundMessageRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing != null)
        {
            return;
        }

        var sendResult = await telegramGateway.SendTextMessageAsync(chatId.Value, message, ct);
        var payloadJson = JsonSerializer.Serialize(new { message, sendResult.ProviderResponse });

        var outbound = new TelegramOutboundMessage
        {
            ClinicalCaseId = clinicalCaseId,
            ChatId = chatId.Value,
            MessageType = "Text",
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey,
            TelegramMessageId = sendResult.TelegramMessageId,
            DeliveryStatus = sendResult.Success ? "Delivered" : "Failed",
        };

        await outboundMessageRepository.AddAsync(outbound, ct);
    }

    private async Task SendVoiceAndLogOutboundAsync(
        long? chatId,
        string sourceText,
        int? clinicalCaseId,
        string idempotencyKey,
        CancellationToken ct
    )
    {
        if (!chatId.HasValue)
        {
            logger.LogWarning("Telegram chat id is missing, outbound voice message was not sent.");
            return;
        }

        var existing = await outboundMessageRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing != null)
        {
            return;
        }

        var ttsInput = BuildTextToSpeechInput(sourceText);
        var ttsResult = await clinicalTextToSpeechService.SynthesizeAsync(
            new ClinicalTextToSpeechRequest(ttsInput, Format: "opus"),
            ct
        );

        if (ttsResult == null || !ttsResult.Success || ttsResult.AudioContent.Length == 0)
        {
            logger.LogWarning(
                "Text-to-speech generation failed. Voice response will be skipped. Reason: {Reason}",
                ttsResult?.ProviderResponse ?? "unknown"
            );
            return;
        }

        var sendResult = await telegramGateway.SendVoiceMessageAsync(
            chatId.Value,
            ttsResult.AudioContent,
            ttsResult.FileName,
            ttsResult.MimeType,
            "Respuesta en audio",
            ct
        );

        var payloadJson = JsonSerializer.Serialize(
            new
            {
                sourceText = ttsInput,
                ttsProviderResponse = ttsResult.ProviderResponse,
                sendResult.ProviderResponse,
            }
        );

        var outbound = new TelegramOutboundMessage
        {
            ClinicalCaseId = clinicalCaseId,
            ChatId = chatId.Value,
            MessageType = "Voice",
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey,
            TelegramMessageId = sendResult.TelegramMessageId,
            DeliveryStatus = sendResult.Success ? "Delivered" : "Failed",
        };

        await outboundMessageRepository.AddAsync(outbound, ct);
    }

    private static bool TryGetMessageElement(JsonElement root, out JsonElement messageElement)
    {
        if (root.TryGetProperty("message", out messageElement))
        {
            return true;
        }

        if (root.TryGetProperty("edited_message", out messageElement))
        {
            return true;
        }

        messageElement = default;
        return false;
    }

    private static long? TryGetChatId(JsonElement messageElement)
    {
        if (
            messageElement.ValueKind != JsonValueKind.Undefined
            && messageElement.TryGetProperty("chat", out var chatElement)
            && chatElement.TryGetProperty("id", out var chatIdElement)
            && chatIdElement.TryGetInt64(out var chatId)
        )
        {
            return chatId;
        }

        return null;
    }

    private static long? TryGetMessageId(JsonElement messageElement)
    {
        if (
            messageElement.ValueKind != JsonValueKind.Undefined
            && messageElement.TryGetProperty("message_id", out var messageIdElement)
            && messageIdElement.TryGetInt64(out var messageId)
        )
        {
            return messageId;
        }

        return null;
    }

    private async Task<string> ExtractIncomingTextAsync(
        JsonElement messageElement,
        CancellationToken cancellationToken
    )
    {
        if (messageElement.ValueKind == JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (messageElement.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString() ?? string.Empty;
        }

        var caption = string.Empty;
        if (messageElement.TryGetProperty("caption", out var captionElement))
        {
            caption = captionElement.GetString() ?? string.Empty;
        }

        if (
            !TryExtractAudioAttachment(
                messageElement,
                out var fileId,
                out var fileName,
                out var mimeType
            )
        )
        {
            return caption;
        }

        var fileDownload = await telegramGateway.DownloadFileAsync(fileId, cancellationToken);
        if (!fileDownload.Success || fileDownload.Content.Length == 0)
        {
            logger.LogWarning(
                "Telegram audio file could not be downloaded for transcription. FileId: {FileId}",
                fileId
            );

            return caption;
        }

        var effectiveMimeType = string.IsNullOrWhiteSpace(mimeType)
            ? fileDownload.ContentType
            : mimeType;
        var effectiveFileName = BuildAudioFileName(
            fileName,
            fileDownload.FilePath,
            effectiveMimeType
        );

        var transcribedText = await clinicalAudioTranscriptionService.TranscribeAsync(
            new ClinicalAudioTranscriptionRequest(
                fileDownload.Content,
                effectiveFileName,
                effectiveMimeType
            ),
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(transcribedText))
        {
            logger.LogWarning(
                "Audio transcription returned empty text. FileId: {FileId}, FilePath: {FilePath}",
                fileId,
                fileDownload.FilePath
            );
            return caption;
        }

        return string.IsNullOrWhiteSpace(caption)
            ? transcribedText
            : $"{caption}\n\nTranscripcion de audio: {transcribedText}";
    }

    private static bool TryExtractAudioAttachment(
        JsonElement messageElement,
        out string fileId,
        out string? fileName,
        out string? mimeType
    )
    {
        fileId = string.Empty;
        fileName = null;
        mimeType = null;

        if (messageElement.TryGetProperty("voice", out var voiceElement))
        {
            fileId = TryGetStringProperty(voiceElement, "file_id") ?? string.Empty;
            mimeType = TryGetStringProperty(voiceElement, "mime_type");
            fileName = "voice.ogg";
            return !string.IsNullOrWhiteSpace(fileId);
        }

        if (messageElement.TryGetProperty("audio", out var audioElement))
        {
            fileId = TryGetStringProperty(audioElement, "file_id") ?? string.Empty;
            fileName = TryGetStringProperty(audioElement, "file_name");
            mimeType = TryGetStringProperty(audioElement, "mime_type");
            return !string.IsNullOrWhiteSpace(fileId);
        }

        return false;
    }

    private static string BuildAudioFileName(string? fileName, string? filePath, string? mimeType)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fileNameFromPath = Path.GetFileName(filePath);
            if (!string.IsNullOrWhiteSpace(fileNameFromPath))
            {
                return fileNameFromPath;
            }
        }

        return mimeType switch
        {
            "audio/ogg" => "voice.ogg",
            "audio/mpeg" => "audio.mp3",
            "audio/wav" => "audio.wav",
            "audio/mp4" => "audio.m4a",
            _ => "audio.bin",
        };
    }

    private static string? TryGetStringProperty(JsonElement element, string propertyName)
    {
        if (
            element.TryGetProperty(propertyName, out var valueElement)
            && valueElement.ValueKind == JsonValueKind.String
        )
        {
            return valueElement.GetString();
        }

        return null;
    }

    private static string BuildStatusMessage(
        string farmName,
        string animalName,
        ClinicalCase? clinicalCase
    )
    {
        if (clinicalCase == null)
        {
            return $"No hay historial clinico para {animalName} en la granja {farmName}.";
        }

        var latestRecommendation = clinicalCase
            .Recommendations.OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latestRecommendation == null)
        {
            return $"Caso clinico #{clinicalCase.Id} para {animalName} sin recomendacion registrada aun.";
        }

        return $"Estado de {animalName} (caso #{clinicalCase.Id}): {clinicalCase.State}.\n\n"
            + latestRecommendation.AdviceText;
    }

    private static string BuildRecommendationMessage(
        string animalDisplay,
        string? earTag,
        ClinicalMedicationAdviceResult advice,
        bool animalMissingInDb,
        bool farmMissingInDb,
        string? temporaryCaseCode
    )
    {
        var prefixLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(temporaryCaseCode))
        {
            prefixLines.Add($"Codigo temporal de seguimiento: {temporaryCaseCode}");
        }

        if (farmMissingInDb)
        {
            prefixLines.Add(
                "No encontre la granja en AgroLink, pero igual te comparto una guia orientativa de prueba."
            );
        }
        else if (animalMissingInDb)
        {
            prefixLines.Add(
                "No encontre ese animal en AgroLink, pero igual te comparto una guia orientativa de prueba."
            );
        }

        var prefix =
            prefixLines.Count == 0 ? string.Empty : string.Join("\n", prefixLines) + "\n\n";

        return prefix + advice.AdviceText;
    }

    private static string GenerateTemporaryCaseCode(long updateId, string scope)
    {
        return $"TMP-{scope}-{updateId}";
    }

    private static string BuildAnimalReferenceText(
        string? animalReference,
        string? temporaryCaseCode
    )
    {
        if (string.IsNullOrWhiteSpace(temporaryCaseCode))
        {
            return animalReference ?? string.Empty;
        }

        var safeReference = string.IsNullOrWhiteSpace(animalReference)
            ? "animal no identificado"
            : animalReference;
        return $"{safeReference} [TEMP:{temporaryCaseCode}]";
    }

    private static string BuildTextToSpeechInput(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var normalized = sourceText.Replace("\r", string.Empty).Trim();
        var symptoms = ExtractSectionText(
            normalized,
            ["Sintomas a monitorear:", "Síntomas a monitorear:"],
            [
                "Tratamiento recomendado:",
                "Consejos:",
                "Recomendacion orientativa",
                "Recomendación orientativa",
            ]
        );
        var treatment = ExtractSectionText(
            normalized,
            ["Tratamiento recomendado:"],
            ["Consejos:", "Recomendacion orientativa", "Recomendación orientativa"]
        );
        var tips = ExtractSectionText(
            normalized,
            ["Consejos:"],
            ["Sintomas a monitorear:", "Síntomas a monitorear:", "Tratamiento recomendado:"]
        );

        var audioParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(symptoms))
        {
            audioParts.Add($"Sintomas a monitorear: {symptoms}");
        }

        if (!string.IsNullOrWhiteSpace(treatment))
        {
            audioParts.Add($"Tratamiento recomendado: {treatment}");
        }

        if (!string.IsNullOrWhiteSpace(tips))
        {
            audioParts.Add($"Consejos: {tips}");
        }

        if (audioParts.Count > 0)
        {
            return string.Join(". ", audioParts);
        }

        return SanitizeSectionText(normalized);
    }

    private static string ExtractSectionText(
        string sourceText,
        string[] sectionHeaders,
        string[] stopHeaders
    )
    {
        var start = IndexOfAny(sourceText, sectionHeaders);
        if (start < 0)
        {
            return string.Empty;
        }

        var selectedHeader =
            sectionHeaders.FirstOrDefault(h =>
                sourceText.AsSpan(start).StartsWith(h, StringComparison.OrdinalIgnoreCase)
            ) ?? sectionHeaders[0];

        var contentStart = start + selectedHeader.Length;
        var remainingText = sourceText.Substring(contentStart);
        var stop = IndexOfAny(remainingText, stopHeaders);
        var rawSection = stop >= 0 ? remainingText[..stop] : remainingText;

        return SanitizeSectionText(rawSection);
    }

    private static int IndexOfAny(string sourceText, string[] candidates)
    {
        var indexes = candidates
            .Select(candidate => sourceText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0)
            .ToList();

        return indexes.Count == 0 ? -1 : indexes.Min();
    }

    private static string SanitizeSectionText(string text)
    {
        var sanitized = text;
        sanitized = Regex.Replace(sanitized, @"[#*_`>\[\]\(\)]", " ");
        sanitized = Regex.Replace(sanitized, @"https?://\S+", " ");

        var lines = sanitized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => Regex.Replace(line, @"^\d+\.\s*", string.Empty))
            .Select(line => Regex.Replace(line, @"^[-•]+\s*", string.Empty))
            .Select(line => Regex.Replace(line, @"\s{2,}", " ").Trim())
            .Select(line => line.TrimEnd('.', ';', ':'))
            .Where(line =>
                !line.StartsWith("Codigo temporal", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("Código temporal", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("No encontre la granja", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("No encontré la granja", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("No encontre ese animal", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("No encontré ese animal", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("Recomendacion orientativa", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("Recomendación orientativa", StringComparison.OrdinalIgnoreCase)
            )
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(". ", lines);
    }

    private static bool IsStartCommand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        return Regex.IsMatch(
            trimmed,
            @"^\/start(@[A-Za-z0-9_]+)?(\s+.*)?$",
            RegexOptions.IgnoreCase
        );
    }
}
