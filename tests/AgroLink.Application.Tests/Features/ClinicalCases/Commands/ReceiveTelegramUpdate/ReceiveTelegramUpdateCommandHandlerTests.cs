using AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;

[TestFixture]
public class ReceiveTelegramUpdateCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<ReceiveTelegramUpdateCommandHandler>();
        _mocker
            .GetMock<ILogger<ReceiveTelegramUpdateCommandHandler>>()
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
        _mocker
            .GetMock<IClinicalTextToSpeechService>()
            .Setup(x =>
                x.SynthesizeAsync(
                    It.IsAny<ClinicalTextToSpeechRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new ClinicalTextToSpeechResult { Success = false });
    }

    private AutoMocker _mocker = null!;
    private ReceiveTelegramUpdateCommandHandler _handler = null!;

    [Test]
    public async Task Handle_WhenAnimalNotFoundButFarmExists_ShouldStillReturnRecommendationDelivered()
    {
        // Arrange
        const long updateId = 999;
        const long chatId = -123456;
        var payload =
            "{\"update_id\":999,\"message\":{\"message_id\":10,\"chat\":{\"id\":-123456},\"text\":\"granja: El Rosario arete: A-123 sintomas: tos\"}}";

        var farm = new Farm { Id = 10, Name = "El Rosario" };

        _mocker
            .GetMock<ITelegramInboundEventLogRepository>()
            .Setup(x => x.GetByTelegramUpdateIdAsync(updateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramInboundEventLog?)null);

        _mocker
            .GetMock<IClinicalExtractionService>()
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ClinicalExtractionResult
                {
                    Intent = ClinicalMessageIntent.NewCaseReport,
                    FarmReference = "El Rosario",
                    AnimalReference = "Lola",
                    EarTag = "A-123",
                    SymptomsSummary = "tos",
                    ConfidenceScore = 0.7,
                    ConfidenceLevel = ExtractionConfidenceLevel.Medium,
                }
            );

        _mocker
            .GetMock<IFarmAnimalResolver>()
            .Setup(x =>
                x.ResolveAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new FarmAnimalResolutionResult
                {
                    Farm = farm,
                    Animal = null,
                    ResolutionMessage = "No pude identificar animal",
                }
            );

        _mocker
            .GetMock<IClinicalCaseRepository>()
            .Setup(x =>
                x.GetOpenCaseByFarmAndReferenceWithinDaysAsync(
                    farm.Id,
                    "A-123",
                    "Lola",
                    7,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ClinicalCase?)null);

        _mocker
            .GetMock<IClinicalMedicationAdvisorService>()
            .Setup(x =>
                x.GetAdviceAsync(
                    It.IsAny<ClinicalMedicationAdviceRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ClinicalMedicationAdviceResult
                {
                    AdviceText = "Orientacion de prueba",
                    Disclaimer = "Validar con veterinario",
                    RiskLevel = ClinicalRiskLevel.Medium,
                    ConfidenceScore = 0.6,
                    RawModelResponse = "{}",
                }
            );

        _mocker
            .GetMock<ITelegramOutboundMessageRepository>()
            .Setup(x =>
                x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((TelegramOutboundMessage?)null);

        _mocker
            .GetMock<ITelegramGateway>()
            .Setup(x =>
                x.SendTextMessageAsync(chatId, It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new TelegramSendResult { Success = true, TelegramMessageId = 101 });

        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(
            new ReceiveTelegramUpdateCommand(payload),
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Processed.ShouldBeTrue();
        result.Status.ShouldBe("RecommendationDelivered");

        _mocker
            .GetMock<IClinicalRecommendationRepository>()
            .Verify(
                x => x.AddAsync(It.IsAny<ClinicalRecommendation>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

        _mocker
            .GetMock<ITelegramGateway>()
            .Verify(
                x =>
                    x.SendTextMessageAsync(
                        chatId,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
    }

    [Test]
    public async Task Handle_WhenFarmNotFound_ShouldStillReturnAiRecommendationWithTemporaryCode()
    {
        // Arrange
        const long updateId = 555;
        const long chatId = -222222;
        var payload =
            "{\"update_id\":555,\"message\":{\"message_id\":10,\"chat\":{\"id\":-222222},\"text\":\"granja: NoExiste animal: Lola sintomas: fiebre\"}}";

        _mocker
            .GetMock<ITelegramInboundEventLogRepository>()
            .Setup(x => x.GetByTelegramUpdateIdAsync(updateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramInboundEventLog?)null);

        _mocker
            .GetMock<IClinicalExtractionService>()
            .Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ClinicalExtractionResult
                {
                    Intent = ClinicalMessageIntent.NewCaseReport,
                    FarmReference = "NoExiste",
                    AnimalReference = "Lola",
                    EarTag = "A-123",
                    SymptomsSummary = "fiebre",
                    ConfidenceScore = 0.5,
                    ConfidenceLevel = ExtractionConfidenceLevel.Medium,
                }
            );

        _mocker
            .GetMock<IFarmAnimalResolver>()
            .Setup(x =>
                x.ResolveAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new FarmAnimalResolutionResult
                {
                    Farm = null,
                    Animal = null,
                    ResolutionMessage = "No encontre granja",
                }
            );

        _mocker
            .GetMock<IClinicalMedicationAdvisorService>()
            .Setup(x =>
                x.GetAdviceAsync(
                    It.IsAny<ClinicalMedicationAdviceRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ClinicalMedicationAdviceResult
                {
                    AdviceText = "Orientacion sin granja",
                    Disclaimer = "Validar con veterinario",
                    RiskLevel = ClinicalRiskLevel.Medium,
                    ConfidenceScore = 0.4,
                    RawModelResponse = "{}",
                }
            );

        _mocker
            .GetMock<ITelegramOutboundMessageRepository>()
            .Setup(x =>
                x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((TelegramOutboundMessage?)null);

        _mocker
            .GetMock<ITelegramGateway>()
            .Setup(x =>
                x.SendTextMessageAsync(chatId, It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new TelegramSendResult { Success = true, TelegramMessageId = 202 });

        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(
            new ReceiveTelegramUpdateCommand(payload),
            CancellationToken.None
        );

        // Assert
        result.Processed.ShouldBeTrue();
        result.Status.ShouldBe("RecommendationDeliveredWithoutFarm");
        result.Message.ShouldContain("TMP-FARM-555");
        _mocker
            .GetMock<ITelegramGateway>()
            .Verify(
                x =>
                    x.SendTextMessageAsync(
                        chatId,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
    }

    [Test]
    public async Task Handle_WhenVoiceMessage_ShouldTranscribeAndUseTranscriptForExtraction()
    {
        // Arrange
        const long updateId = 777;
        const long chatId = -333333;
        const string transcript = "granja: El Rosario arete: A-777 sintomas: diarrea";
        var payload =
            "{\"update_id\":777,\"message\":{\"message_id\":11,\"chat\":{\"id\":-333333},\"voice\":{\"file_id\":\"voice-file-1\",\"mime_type\":\"audio/ogg\"}}}";

        _mocker
            .GetMock<ITelegramInboundEventLogRepository>()
            .Setup(x => x.GetByTelegramUpdateIdAsync(updateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramInboundEventLog?)null);

        _mocker
            .GetMock<ITelegramGateway>()
            .Setup(x => x.DownloadFileAsync("voice-file-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TelegramFileDownloadResult
                {
                    Success = true,
                    Content = [1, 2, 3],
                    FilePath = "voice/file_1.ogg",
                    ContentType = "audio/ogg",
                    ProviderResponse = "{}",
                }
            );

        _mocker
            .GetMock<IClinicalAudioTranscriptionService>()
            .Setup(x =>
                x.TranscribeAsync(
                    It.Is<ClinicalAudioTranscriptionRequest>(r =>
                        r.FileName == "voice.ogg"
                        && r.MimeType == "audio/ogg"
                        && r.AudioContent.SequenceEqual(new byte[] { 1, 2, 3 })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(transcript);

        _mocker
            .GetMock<IClinicalExtractionService>()
            .Setup(x =>
                x.ExtractAsync(It.Is<string>(t => t == transcript), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new ClinicalExtractionResult
                {
                    Intent = ClinicalMessageIntent.NewCaseReport,
                    FarmReference = "El Rosario",
                    AnimalReference = "A-777",
                    EarTag = "A-777",
                    SymptomsSummary = "diarrea",
                    ConfidenceScore = 0.7,
                    ConfidenceLevel = ExtractionConfidenceLevel.Medium,
                }
            );

        _mocker
            .GetMock<IFarmAnimalResolver>()
            .Setup(x =>
                x.ResolveAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new FarmAnimalResolutionResult
                {
                    Farm = null,
                    Animal = null,
                    ResolutionMessage = "No encontre granja",
                }
            );

        _mocker
            .GetMock<IClinicalMedicationAdvisorService>()
            .Setup(x =>
                x.GetAdviceAsync(
                    It.IsAny<ClinicalMedicationAdviceRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ClinicalMedicationAdviceResult
                {
                    AdviceText = "Orientacion para diarrea",
                    Disclaimer = "Validar con veterinario",
                    RiskLevel = ClinicalRiskLevel.Medium,
                    ConfidenceScore = 0.4,
                    RawModelResponse = "{}",
                }
            );

        _mocker
            .GetMock<ITelegramOutboundMessageRepository>()
            .Setup(x =>
                x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((TelegramOutboundMessage?)null);

        _mocker
            .GetMock<ITelegramGateway>()
            .Setup(x =>
                x.SendTextMessageAsync(chatId, It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new TelegramSendResult { Success = true, TelegramMessageId = 303 });

        _mocker
            .GetMock<IClinicalTextToSpeechService>()
            .Setup(x =>
                x.SynthesizeAsync(
                    It.IsAny<ClinicalTextToSpeechRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ClinicalTextToSpeechResult
                {
                    Success = true,
                    AudioContent = [4, 5, 6],
                    FileName = "clinical-recommendation.mp3",
                    MimeType = "audio/mpeg",
                    ProviderResponse = "ok",
                }
            );

        _mocker
            .GetMock<ITelegramGateway>()
            .Setup(x =>
                x.SendVoiceMessageAsync(
                    chatId,
                    It.Is<byte[]>(b => b.SequenceEqual(new byte[] { 4, 5, 6 })),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new TelegramSendResult { Success = true, TelegramMessageId = 304 });

        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(
            new ReceiveTelegramUpdateCommand(payload),
            CancellationToken.None
        );

        // Assert
        result.Processed.ShouldBeTrue();
        result.Status.ShouldBe("RecommendationDeliveredWithoutFarm");
        _mocker
            .GetMock<ITelegramGateway>()
            .Verify(
                x => x.DownloadFileAsync("voice-file-1", It.IsAny<CancellationToken>()),
                Times.Once
            );
        _mocker
            .GetMock<IClinicalAudioTranscriptionService>()
            .Verify(
                x =>
                    x.TranscribeAsync(
                        It.IsAny<ClinicalAudioTranscriptionRequest>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        _mocker
            .GetMock<IClinicalExtractionService>()
            .Verify(x => x.ExtractAsync(transcript, It.IsAny<CancellationToken>()), Times.Once);
        _mocker
            .GetMock<ITelegramGateway>()
            .Verify(
                x =>
                    x.SendVoiceMessageAsync(
                        chatId,
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
    }
}
