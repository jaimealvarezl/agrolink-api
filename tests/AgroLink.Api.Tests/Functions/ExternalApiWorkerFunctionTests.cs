using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
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
        _sqsMock = new Mock<IAmazonSQS>();
        _capturedSqsMessages = [];

        _sqsMock
            .Setup(s =>
                s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>())
            )
            .Callback<SendMessageRequest, CancellationToken>(
                (req, _) => _capturedSqsMessages.Add(req)
            )
            .ReturnsAsync(new SendMessageResponse());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_medicationAdvisorMock.Object);
        services.AddSingleton(_transcriptionServiceMock.Object);
        services.AddSingleton(_textToSpeechServiceMock.Object);
        services.AddSingleton(_telegramGatewayMock.Object);
        services.AddSingleton(_sqsMock.Object);

        _function = new ExternalApiWorkerFunction(services.BuildServiceProvider(), ResultsQueueUrl);
    }

    private Mock<IClinicalMedicationAdvisorService> _medicationAdvisorMock = null!;
    private Mock<IClinicalAudioTranscriptionService> _transcriptionServiceMock = null!;
    private Mock<IClinicalTextToSpeechService> _textToSpeechServiceMock = null!;
    private Mock<ITelegramGateway> _telegramGatewayMock = null!;
    private Mock<IAmazonSQS> _sqsMock = null!;
    private ExternalApiWorkerFunction _function = null!;
    private List<SendMessageRequest> _capturedSqsMessages = null!;

    private const string ResultsQueueUrl = "https://sqs.us-east-1.amazonaws.com/123/results";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Test]
    public async Task FunctionHandler_GetMedicationAdvice_PublishesResultToQueue()
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

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        _capturedSqsMessages.Count.ShouldBe(1);
        _capturedSqsMessages[0].QueueUrl.ShouldBe(ResultsQueueUrl);
        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
        response.CorrelationId.ShouldBe("corr-1");
        response.Success.ShouldBeTrue();
        response.Error.ShouldBeNull();
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

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        _capturedSqsMessages.Count.ShouldBe(1);
        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
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

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
        response.Success.ShouldBeTrue();
        var resultJson = response.Result!.Value.GetRawText();
        resultJson.ShouldContain(Convert.ToBase64String(ttsResult.AudioContent));
    }

    [Test]
    public async Task FunctionHandler_SendTelegramText_PublishesResult()
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

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
        response.Success.ShouldBeTrue();
    }

    [Test]
    public async Task FunctionHandler_SendTelegramVoice_PublishesResult()
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

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
        response.Success.ShouldBeTrue();
    }

    [Test]
    public async Task FunctionHandler_UnknownOperation_PublishesFailureResult()
    {
        var request = new ExternalWorkerRequest(
            "corr-6",
            "DoSomethingUnknown",
            JsonSerializer.SerializeToElement(new { })
        );

        await _function.FunctionHandler(BuildSqsEvent(request), null!);

        var response = JsonSerializer.Deserialize<ExternalWorkerResponse>(
            _capturedSqsMessages[0].MessageBody,
            JsonOptions
        )!;
        response.Success.ShouldBeFalse();
        response.Error.ShouldContain("DoSomethingUnknown");
    }

    [Test]
    public async Task FunctionHandler_EmptyResultsQueueUrl_SkipsPublish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_medicationAdvisorMock.Object);
        services.AddSingleton(_transcriptionServiceMock.Object);
        services.AddSingleton(_textToSpeechServiceMock.Object);
        services.AddSingleton(_telegramGatewayMock.Object);
        services.AddSingleton(_sqsMock.Object);

        var functionWithNoQueue = new ExternalApiWorkerFunction(
            services.BuildServiceProvider(),
            string.Empty
        );

        _medicationAdvisorMock
            .Setup(s =>
                s.GetAdviceAsync(
                    It.IsAny<ClinicalMedicationAdviceRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new ClinicalMedicationAdviceResult());

        var request = new ExternalWorkerRequest(
            "corr-7",
            ExternalWorkerOperations.GetMedicationAdvice,
            JsonSerializer.SerializeToElement(new ClinicalMedicationAdviceRequest())
        );

        await functionWithNoQueue.FunctionHandler(BuildSqsEvent(request), null!);

        _sqsMock.Verify(
            s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public void FunctionHandler_InvalidJson_ThrowsAndDoesNotPublish()
    {
        var sqsEvent = new SQSEvent
        {
            Records = [new SQSEvent.SQSMessage { Body = "not-valid-json", MessageId = "msg-1" }],
        };

        Should.Throw<Exception>(() =>
            _function.FunctionHandler(sqsEvent, null!).GetAwaiter().GetResult()
        );
        _capturedSqsMessages.ShouldBeEmpty();
    }

    private static SQSEvent BuildSqsEvent(ExternalWorkerRequest request)
    {
        return new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    Body = JsonSerializer.Serialize(request),
                    MessageId = $"msg-{request.CorrelationId}",
                },
            ],
        };
    }
}
