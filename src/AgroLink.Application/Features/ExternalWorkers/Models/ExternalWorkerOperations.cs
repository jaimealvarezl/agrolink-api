namespace AgroLink.Application.Features.ExternalWorkers.Models;

public static class ExternalWorkerOperations
{
    public const string GetMedicationAdvice = "GetMedicationAdvice";
    public const string TranscribeAudio = "TranscribeAudio";
    public const string SynthesizeSpeech = "SynthesizeSpeech";
    public const string SendTelegramText = "SendTelegramText";
    public const string SendTelegramVoice = "SendTelegramVoice";
    public const string DownloadTelegramFile = "DownloadTelegramFile";
    public const string TranscribeVoiceAudio = "TranscribeVoiceAudio";
    public const string ExtractVoiceIntent = "ExtractVoiceIntent";
}
