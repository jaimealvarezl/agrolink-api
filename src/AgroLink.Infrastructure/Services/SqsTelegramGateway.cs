using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class SqsTelegramGateway(IExternalApiWorkerClient client) : ITelegramGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<TelegramSendResult> SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken ct = default
    )
    {
        var payload = new SendTelegramTextPayload(chatId, text);
        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.SendTelegramText,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);
        return response.Result?.Deserialize<TelegramSendResult>(JsonOptions)
            ?? new TelegramSendResult { Success = response.Success };
    }

    public async Task<TelegramSendResult> SendVoiceMessageAsync(
        long chatId,
        byte[] audioContent,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken ct = default
    )
    {
        var payload = new SendTelegramVoicePayload(
            chatId,
            Convert.ToBase64String(audioContent),
            fileName,
            mimeType,
            caption
        );
        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.SendTelegramVoice,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);
        return response.Result?.Deserialize<TelegramSendResult>(JsonOptions)
            ?? new TelegramSendResult { Success = response.Success };
    }

    public async Task<TelegramSendResult> SendAudioMessageAsync(
        long chatId,
        byte[] audioContent,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken ct = default
    )
    {
        return await SendVoiceMessageAsync(chatId, audioContent, fileName, mimeType, caption, ct);
    }

    public async Task<TelegramFileDownloadResult> DownloadFileAsync(
        string fileId,
        CancellationToken ct = default
    )
    {
        var payload = new DownloadTelegramFilePayload(fileId);
        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.DownloadTelegramFile,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);

        if (!response.Success)
        {
            return new TelegramFileDownloadResult
            {
                Success = false,
                ProviderResponse = response.Error ?? "DownloadTelegramFile failed",
            };
        }

        var dto = response.Result?.Deserialize<DownloadFileResultDto>(JsonOptions);
        if (dto == null)
        {
            return new TelegramFileDownloadResult { Success = false };
        }

        return new TelegramFileDownloadResult
        {
            Success = true,
            Content = Convert.FromBase64String(dto.Base64Content),
            FilePath = dto.FilePath,
            ContentType = dto.ContentType,
        };
    }

    private record DownloadFileResultDto(string Base64Content, string FilePath, string ContentType);
}
