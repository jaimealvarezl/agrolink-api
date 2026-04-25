using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Functions;

[TestFixture]
public class ExternalApiWorkerFunctionTests
{
    [SetUp]
    public void Setup()
    {
        _medicationAdvisorMock = new Mock<IClinicalMedicationAdvisorService>();
        _transcriptionServiceMock = new Mock<IClinicalAudioTranscriptionService>();
        _textToSpeechServiceMock = new Mock<IClinicalTextToSpeechService>();
        _telegramGatewayMock = new Mock<ITelegramGateway>();
        _voiceTranscriptionMock = new Mock<IVoiceTranscriptionService>();
        _voiceIntentMock = new Mock<IVoiceIntentService>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_medicationAdvisorMock.Object);
        services.AddSingleton(_transcriptionServiceMock.Object);
        services.AddSingleton(_textToSpeechServiceMock.Object);
        services.AddSingleton(_telegramGatewayMock.Object);
        services.AddSingleton(_voiceTranscriptionMock.Object);
        services.AddSingleton(_voiceIntentMock.Object);

        _function = new ExternalApiWorkerFunction(services.BuildServiceProvider());
    }

    private Mock<IClinicalMedicationAdvisorService> _medicationAdvisorMock = null!;
    private Mock<IClinicalAudioTranscriptionService> _transcriptionServiceMock = null!;
    private Mock<IClinicalTextToSpeechService> _textToSpeechServiceMock = null!;
    private Mock<ITelegramGateway> _telegramGatewayMock = null!;
    private Mock<IVoiceTranscriptionService> _voiceTranscriptionMock = null!;
    private Mock<IVoiceIntentService> _voiceIntentMock = null!;
    private ExternalApiWorkerFunction _function = null!;

    [Test]
    public async Task FunctionHandler_GetMedicationAdvice_ReturnsSuccessResponse()
    {
        var adviceResult = new ClinicalMedicationAdviceResult
        {
            AdviceText = "Give aspirin",
            Disclaimer = "Not real advice",
        };
        _medicationAdvisorMock
            .Setup(s =>
                s.GetAdviceAsync(
                    It.IsAny<ClinicalMedicationAdviceRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(adviceResult);

        var request = new ExternalWorkerRequest(
            "corr-1",
            ExternalWorkerOperations.GetMedicationAdvice,
            JsonSerializer.SerializeToElement(
                new ClinicalMedicationAdviceRequest { SymptomsSummary = "coughing" }
            )
        );

        var response = await _function.FunctionHandler(request, null!);

        response.CorrelationId.ShouldBe("corr-1");
        response.Success.ShouldBeTrue();
        response.Error.ShouldBeNull();
        response.Result.ShouldNotBeNull();
    }

    [Test]
    public async Task FunctionHandler_TranscribeAudio_ReturnsTranscribedText()
    {
        _transcriptionServiceMock
            .Setup(s =>
                s.TranscribeAsync(
                    It.IsAny<ClinicalAudioTranscriptionRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("the transcribed text");

        var payload = new TranscribeAudioPayload(
            Convert.ToBase64String([1, 2, 3]),
            "voice.ogg",
            "audio/ogg"
        );
        var request = new ExternalWorkerRequest(
            "corr-2",
            ExternalWorkerOperations.TranscribeAudio,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        response.Result!.Value.GetProperty("text").GetString().ShouldBe("the transcribed text");
    }

    [Test]
    public async Task FunctionHandler_SynthesizeSpeech_ReturnsBase64Audio()
    {
        var ttsResult = new ClinicalTextToSpeechResult
        {
            Success = true,
            AudioContent = [10, 20, 30],
            FileName = "speech.ogg",
            MimeType = "audio/ogg",
            ProviderResponse = "ok",
        };
        _textToSpeechServiceMock
            .Setup(s =>
                s.SynthesizeAsync(
                    It.IsAny<ClinicalTextToSpeechRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ttsResult);

        var request = new ExternalWorkerRequest(
            "corr-3",
            ExternalWorkerOperations.SynthesizeSpeech,
            JsonSerializer.SerializeToElement(new ClinicalTextToSpeechRequest("hello"))
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        var resultJson = response.Result!.Value.GetRawText();
        resultJson.ShouldContain(Convert.ToBase64String(ttsResult.AudioContent));
    }

    [Test]
    public async Task FunctionHandler_SynthesizeSpeech_HandlesLargeAudioWithoutSizeConstraints()
    {
        var largeAudio = new byte[300_000]; // 300KB — exceeds old SQS 256KB limit
        new Random(42).NextBytes(largeAudio);

        var ttsResult = new ClinicalTextToSpeechResult
        {
            Success = true,
            AudioContent = largeAudio,
            FileName = "speech.ogg",
            MimeType = "audio/ogg",
            ProviderResponse = "ok",
        };
        _textToSpeechServiceMock
            .Setup(s =>
                s.SynthesizeAsync(
                    It.IsAny<ClinicalTextToSpeechRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ttsResult);

        var request = new ExternalWorkerRequest(
            "corr-large",
            ExternalWorkerOperations.SynthesizeSpeech,
            JsonSerializer.SerializeToElement(new ClinicalTextToSpeechRequest("long text"))
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        var base64 = response.Result!.Value.GetProperty("Base64AudioContent").GetString()!;
        Convert.FromBase64String(base64).ShouldBe(largeAudio);
    }

    [Test]
    public async Task FunctionHandler_SendTelegramText_ReturnsSuccessResponse()
    {
        _telegramGatewayMock
            .Setup(s => s.SendTextMessageAsync(42L, "hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramSendResult { Success = true, TelegramMessageId = 99 });

        var payload = new SendTelegramTextPayload(42L, "hello");
        var request = new ExternalWorkerRequest(
            "corr-4",
            ExternalWorkerOperations.SendTelegramText,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
    }

    [Test]
    public async Task FunctionHandler_SendTelegramVoice_ReturnsSuccessResponse()
    {
        _telegramGatewayMock
            .Setup(s =>
                s.SendVoiceMessageAsync(
                    42L,
                    It.IsAny<byte[]>(),
                    "audio.ogg",
                    "audio/ogg",
                    null,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TelegramSendResult { Success = true });

        var payload = new SendTelegramVoicePayload(
            42L,
            Convert.ToBase64String([1, 2, 3]),
            "audio.ogg",
            "audio/ogg",
            null
        );
        var request = new ExternalWorkerRequest(
            "corr-5",
            ExternalWorkerOperations.SendTelegramVoice,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
    }

    [Test]
    public async Task FunctionHandler_UnknownOperation_ReturnsFailureResponse()
    {
        var request = new ExternalWorkerRequest(
            "corr-6",
            "DoSomethingUnknown",
            JsonSerializer.SerializeToElement(new { })
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeFalse();
        response.Error.ShouldContain("DoSomethingUnknown");
    }

    // ── TranscribeVoiceAudio ───────────────────────────────────────────────────

    [Test]
    public async Task FunctionHandler_TranscribeVoiceAudio_ReturnsTranscribedText()
    {
        _voiceTranscriptionMock
            .Setup(s =>
                s.TranscribeAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    "es",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("mover la vaca Rosa al lote norte");

        var payload = new TranscribeVoiceAudioPayload(
            Convert.ToBase64String([1, 2, 3, 4, 5]),
            "command.m4a",
            "audio/mp4"
        );
        var request = new ExternalWorkerRequest(
            "corr-va-1",
            ExternalWorkerOperations.TranscribeVoiceAudio,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        response.Error.ShouldBeNull();
        response
            .Result!.Value.GetProperty("text")
            .GetString()
            .ShouldBe("mover la vaca Rosa al lote norte");
    }

    [Test]
    public async Task FunctionHandler_TranscribeVoiceAudio_WhenServiceReturnsNull_ReturnsEmptyText()
    {
        _voiceTranscriptionMock
            .Setup(s =>
                s.TranscribeAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((string?)null);

        var payload = new TranscribeVoiceAudioPayload(
            Convert.ToBase64String([1, 2, 3]),
            "command.m4a",
            null
        );
        var request = new ExternalWorkerRequest(
            "corr-va-2",
            ExternalWorkerOperations.TranscribeVoiceAudio,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        response.Result!.Value.GetProperty("text").GetString().ShouldBe(string.Empty);
    }

    [Test]
    public async Task FunctionHandler_TranscribeVoiceAudio_PassesLanguageEsToService()
    {
        _voiceTranscriptionMock
            .Setup(s =>
                s.TranscribeAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("algo");

        var payload = new TranscribeVoiceAudioPayload(Convert.ToBase64String([1]), "x.m4a", null);
        var request = new ExternalWorkerRequest(
            "c",
            ExternalWorkerOperations.TranscribeVoiceAudio,
            JsonSerializer.SerializeToElement(payload)
        );

        await _function.FunctionHandler(request, null!);

        _voiceTranscriptionMock.Verify(
            s =>
                s.TranscribeAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    "es",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    // ── ExtractVoiceIntent ─────────────────────────────────────────────────────

    [Test]
    public async Task FunctionHandler_ExtractVoiceIntent_ReturnsIntentJson()
    {
        const string intentJson =
            """{"intent":"move_animal","confidence":0.92,"animalMention":"Rosa","lotMention":"lote norte"}""";
        _voiceIntentMock
            .Setup(s => s.ExtractIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(intentJson);

        var payload = new ExtractVoiceIntentPayload("mover Rosa al lote norte");
        var request = new ExternalWorkerRequest(
            "corr-vi-1",
            ExternalWorkerOperations.ExtractVoiceIntent,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeTrue();
        response.Error.ShouldBeNull();
        response.Result!.Value.GetProperty("intent").GetString().ShouldBe("move_animal");
        response.Result!.Value.GetProperty("confidence").GetDouble().ShouldBe(0.92);
    }

    [Test]
    public async Task FunctionHandler_ExtractVoiceIntent_WhenServiceReturnsNull_ReturnsFailure()
    {
        _voiceIntentMock
            .Setup(s => s.ExtractIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var payload = new ExtractVoiceIntentPayload("algo");
        var request = new ExternalWorkerRequest(
            "corr-vi-2",
            ExternalWorkerOperations.ExtractVoiceIntent,
            JsonSerializer.SerializeToElement(payload)
        );

        var response = await _function.FunctionHandler(request, null!);

        response.Success.ShouldBeFalse();
        response.Error.ShouldNotBeNullOrEmpty();
        response.Result.ShouldBeNull();
    }

    [Test]
    public async Task FunctionHandler_ExtractVoiceIntent_PassesTranscriptToService()
    {
        _voiceIntentMock
            .Setup(s => s.ExtractIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"intent":"unknown","confidence":0.0}""");

        var payload = new ExtractVoiceIntentPayload("nota para Lola");
        var request = new ExternalWorkerRequest(
            "c",
            ExternalWorkerOperations.ExtractVoiceIntent,
            JsonSerializer.SerializeToElement(payload)
        );

        await _function.FunctionHandler(request, null!);

        _voiceIntentMock.Verify(
            s => s.ExtractIntentAsync("nota para Lola", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
