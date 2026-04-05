using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class TelegramGateway(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TelegramGateway> logger
) : ITelegramGateway
{
    private readonly string _botToken = configuration["Telegram:BotToken"] ?? string.Empty;

    public async Task<TelegramSendResult> SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            logger.LogWarning("Telegram bot token is not configured.");
            return new TelegramSendResult
            {
                Success = false,
                ProviderResponse = "Missing Telegram bot token.",
            };
        }

        var endpoint = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = JsonSerializer.Serialize(new { chat_id = chatId, text });

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram sendMessage failed with status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return new TelegramSendResult { Success = false, ProviderResponse = body };
            }

            var messageId = ExtractMessageId(body);
            return new TelegramSendResult
            {
                Success = true,
                TelegramMessageId = messageId,
                ProviderResponse = body,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while sending Telegram message.");
            return new TelegramSendResult { Success = false, ProviderResponse = ex.Message };
        }
    }

    public async Task<TelegramFileDownloadResult> DownloadFileAsync(
        string fileId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            logger.LogWarning("Telegram bot token is not configured.");
            return new TelegramFileDownloadResult
            {
                Success = false,
                ProviderResponse = "Missing Telegram bot token.",
            };
        }

        if (string.IsNullOrWhiteSpace(fileId))
        {
            return new TelegramFileDownloadResult
            {
                Success = false,
                ProviderResponse = "Telegram file id is required.",
            };
        }

        var getFileEndpoint =
            $"https://api.telegram.org/bot{_botToken}/getFile?file_id={Uri.EscapeDataString(fileId)}";

        try
        {
            using var getFileResponse = await httpClient.GetAsync(getFileEndpoint, ct);
            var getFileBody = await getFileResponse.Content.ReadAsStringAsync(ct);

            if (!getFileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram getFile failed with status {StatusCode}. Body: {Body}",
                    getFileResponse.StatusCode,
                    getFileBody
                );
                return new TelegramFileDownloadResult
                {
                    Success = false,
                    ProviderResponse = getFileBody,
                };
            }

            var filePath = ExtractFilePath(getFileBody);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                logger.LogWarning("Telegram getFile response did not contain a file path.");
                return new TelegramFileDownloadResult
                {
                    Success = false,
                    ProviderResponse = getFileBody,
                };
            }

            var downloadEndpoint = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";
            using var downloadResponse = await httpClient.GetAsync(downloadEndpoint, ct);

            if (!downloadResponse.IsSuccessStatusCode)
            {
                var downloadBody = await downloadResponse.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Telegram file download failed with status {StatusCode}. Body: {Body}",
                    downloadResponse.StatusCode,
                    downloadBody
                );
                return new TelegramFileDownloadResult
                {
                    Success = false,
                    FilePath = filePath,
                    ProviderResponse = downloadBody,
                };
            }

            var content = await downloadResponse.Content.ReadAsByteArrayAsync(ct);
            var contentType =
                downloadResponse.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return new TelegramFileDownloadResult
            {
                Success = true,
                Content = content,
                ContentType = contentType,
                FilePath = filePath,
                ProviderResponse = getFileBody,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while downloading Telegram file.");
            return new TelegramFileDownloadResult
            {
                Success = false,
                ProviderResponse = ex.Message,
            };
        }
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
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            logger.LogWarning("Telegram bot token is not configured.");
            return new TelegramSendResult
            {
                Success = false,
                ProviderResponse = "Missing Telegram bot token.",
            };
        }

        if (audioContent.Length == 0)
        {
            return new TelegramSendResult
            {
                Success = false,
                ProviderResponse = "Audio content is empty.",
            };
        }

        var endpoint = $"https://api.telegram.org/bot{_botToken}/sendVoice";
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(chatId.ToString(CultureInfo.InvariantCulture)), "chat_id");

        if (!string.IsNullOrWhiteSpace(caption))
        {
            formData.Add(new StringContent(caption), "caption");
        }

        using var fileContent = new ByteArrayContent(audioContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        formData.Add(fileContent, "voice", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = formData,
        };

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram sendVoice failed with status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return new TelegramSendResult { Success = false, ProviderResponse = body };
            }

            var messageId = ExtractMessageId(body);
            return new TelegramSendResult
            {
                Success = true,
                TelegramMessageId = messageId,
                ProviderResponse = body,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while sending Telegram voice message.");
            return new TelegramSendResult { Success = false, ProviderResponse = ex.Message };
        }
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
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            logger.LogWarning("Telegram bot token is not configured.");
            return new TelegramSendResult
            {
                Success = false,
                ProviderResponse = "Missing Telegram bot token.",
            };
        }

        if (audioContent.Length == 0)
        {
            return new TelegramSendResult
            {
                Success = false,
                ProviderResponse = "Audio content is empty.",
            };
        }

        var endpoint = $"https://api.telegram.org/bot{_botToken}/sendAudio";
        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(chatId.ToString(CultureInfo.InvariantCulture)), "chat_id");

        if (!string.IsNullOrWhiteSpace(caption))
        {
            formData.Add(new StringContent(caption), "caption");
        }

        using var fileContent = new ByteArrayContent(audioContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        formData.Add(fileContent, "audio", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = formData,
        };

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram sendAudio failed with status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return new TelegramSendResult { Success = false, ProviderResponse = body };
            }

            var messageId = ExtractMessageId(body);
            return new TelegramSendResult
            {
                Success = true,
                TelegramMessageId = messageId,
                ProviderResponse = body,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while sending Telegram audio message.");
            return new TelegramSendResult { Success = false, ProviderResponse = ex.Message };
        }
    }

    private static long? ExtractMessageId(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (
            root.TryGetProperty("result", out var resultElement)
            && resultElement.TryGetProperty("message_id", out var messageIdElement)
            && messageIdElement.TryGetInt64(out var messageId)
        )
        {
            return messageId;
        }

        return null;
    }

    private static string ExtractFilePath(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (
            root.TryGetProperty("result", out var resultElement)
            && resultElement.TryGetProperty("file_path", out var filePathElement)
            && filePathElement.ValueKind == JsonValueKind.String
        )
        {
            return filePathElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
