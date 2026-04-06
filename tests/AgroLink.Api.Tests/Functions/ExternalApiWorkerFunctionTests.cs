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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_medicationAdvisorMock.Object);
        services.AddSingleton(_transcriptionServiceMock.Object);
        services.AddSingleton(_textToSpeechServiceMock.Object);
        services.AddSingleton(_telegramGatewayMock.Object);

        _function = new ExternalApiWorkerFunction(services.BuildServiceProvider());
    }

    private Mock<IClinicalMedicationAdvisorService> _medicationAdvisorMock = null!;
    private Mock<IClinicalAudioTranscriptionService> _transcriptionServiceMock = null!;
    private Mock<IClinicalTextToSpeechService> _textToSpeechServiceMock = null!;
    private Mock<ITelegramGateway> _telegramGatewayMock = null!;
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
}
