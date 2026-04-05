using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface ITelegramGateway
{
    Task<TelegramSendResult> SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken ct = default
    );

    Task<TelegramSendResult> SendVoiceMessageAsync(
        long chatId,
        byte[] audioContent,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken ct = default
    );

    Task<TelegramSendResult> SendAudioMessageAsync(
        long chatId,
        byte[] audioContent,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken ct = default
    );

    Task<TelegramFileDownloadResult> DownloadFileAsync(
        string fileId,
        CancellationToken ct = default
    );
}
