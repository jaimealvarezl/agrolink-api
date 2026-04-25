using System.Text.Json;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.VoiceCommands.Commands.ProcessVoiceCommand;

[TestFixture]
public class ProcessVoiceCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<ProcessVoiceCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private ProcessVoiceCommandHandler _handler = null!;

    private static readonly byte[] AudioBytes = [1, 2, 3, 4, 5];
    private static readonly FarmRosterDto EmptyRoster = new([], []);

    private static readonly FarmRosterDto RosterWithAnimalsAndLots = new(
        [new AnimalRosterEntry(10, "Rosa", "042", null, 1, "Lote Norte")],
        [new LotRosterEntry(1, "Lote Norte", 100, "Potrero Grande")]
    );

    // ── idempotency ────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenJobNotFound_SkipsAllProcessing()
    {
        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandJob?)null);

        await _handler.Handle(
            new ProcessVoiceCommandCommand(Guid.NewGuid(), 1, 1),
            CancellationToken.None
        );

        _mocker
            .GetMock<IStorageService>()
            .Verify(
                s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [TestCase("completed")]
    [TestCase("failed")]
    public async Task Handle_WhenJobAlreadyInTerminalState_SkipsAllProcessing(string status)
    {
        var job = BuildJob(status);
        SetupJob(job);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        _mocker
            .GetMock<IStorageService>()
            .Verify(
                s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── S3 download failure ────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenS3DownloadFails_MarksJobFailed()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        _mocker
            .GetMock<IStorageService>()
            .Setup(s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("failed");
        job.ErrorMessage.ShouldNotBeNullOrEmpty();
        _mocker
            .GetMock<IExternalApiWorkerClient>()
            .Verify(
                c =>
                    c.ExecuteAsync(
                        It.IsAny<ExternalWorkerRequest>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
    }

    // ── transcription failure ──────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenTranscriptionFails_MarksJobFailed()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(false);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("failed");
        _mocker
            .GetMock<IFarmRosterService>()
            .Verify(
                r => r.GetRosterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
    }

    // ── empty transcript ───────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenTranscriptIsEmpty_CompletesWithUnknownWithoutCallingGpt4o()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        job.ResultJson.ShouldNotBeNull();
        var result = JsonSerializer.Deserialize<JsonElement>(job.ResultJson!);
        result.GetProperty("intent").GetString().ShouldBe("unknown");

        _mocker
            .GetMock<IFarmRosterService>()
            .Verify(
                r => r.GetRosterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        VerifyGpt4oNotCalled();
    }

    [Test]
    public async Task Handle_WhenTranscriptIsWhitespace_CompletesWithUnknown()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "   ");

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        VerifyGpt4oNotCalled();
    }

    // ── GPT-4o failures ────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenIntentExtractionTimesOut_MarksJobFailed()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);

        _mocker
            .GetMock<IExternalApiWorkerClient>()
            .Setup(c =>
                c.ExecuteAsync(
                    It.Is<ExternalWorkerRequest>(r =>
                        r.Operation == ExternalWorkerOperations.ExtractVoiceIntent
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new OperationCanceledException());

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("failed");
        job.ErrorMessage.ShouldContain("timed out");
    }

    [Test]
    public async Task Handle_WhenIntentExtractionFails_MarksJobFailed()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(false);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("failed");
    }

    [Test]
    public async Task Handle_WhenLlmJsonIsMalformed_CompletesWithUnknownIntent()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(true, """{ "not_valid_at_all": }""");

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(job.ResultJson!);
        result.GetProperty("intent").GetString().ShouldBe("unknown");
    }

    // ── happy path ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_HappyPath_MoveAnimal_CompletesWithIntent()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            { "intent": "move_animal", "confidence": 0.92, "animalId": 10, "lotId": 1 }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        job.CompletedAt.ShouldNotBeNull();
        job.ResultJson.ShouldNotBeNull();

        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("move_animal");
        result.GetProperty("confidence").GetDouble().ShouldBe(0.92);
        result.GetProperty("rawTranscription").GetString().ShouldBe("mover Rosa al lote norte");
    }

    [Test]
    public async Task Handle_HappyPath_CreateNote_CompletesWithNoteText()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "nota para Rosa: cojea de la pata");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            { "intent": "create_note", "confidence": 0.88, "animalId": 10, "noteText": "cojea de la pata" }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("create_note");
        var entities = result.GetProperty("entities");
        entities.GetProperty("animalId").GetInt32().ShouldBe(10);
        entities.GetProperty("noteText").GetString().ShouldBe("cojea de la pata");
    }

    // ── entity validation ──────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenLlmHallucinatesAnimalId_NullsIdAndPenalizesConfidence()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover el toro al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        // animalId 999 not in roster → penalized
        SetupIntentExtraction(
            true,
            """
            { "intent": "move_animal", "confidence": 0.85, "animalId": 999, "lotId": 1 }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("move_animal");
        result.GetProperty("confidence").GetDouble().ShouldBe(0.65, 0.001);
        result.GetProperty("entities").TryGetProperty("animalId", out _).ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WhenConfidenceDropsBelowThreshold_DowngradesToUnknown()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover algo a algún lado");
        SetupRoster(RosterWithAnimalsAndLots);
        // Both IDs hallucinated: 0.8 - 0.4 = 0.4 < 0.5
        SetupIntentExtraction(
            true,
            """
            { "intent": "move_animal", "confidence": 0.8, "animalId": 999, "lotId": 999 }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("unknown");
        result.GetProperty("confidence").GetDouble().ShouldBe(0.0);
    }

    // ── create_animal ──────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_HappyPath_CreateAnimal_CompletesWithAllEntities()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(
            true,
            "registrar vaca colorada arete 017683344, la milagro, lote forro, pertenece a Carla y Jaime"
        );
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            {
              "intent": "create_animal",
              "confidence": 0.91,
              "lotId": 1,
              "sex": "female",
              "animalName": "la milagro",
              "earTag": "017683344",
              "color": "colorada",
              "ownerNames": ["Carla", "Jaime"]
            }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("create_animal");
        result.GetProperty("confidence").GetDouble().ShouldBe(0.91);
        var entities = result.GetProperty("entities");
        entities.GetProperty("animalName").GetString().ShouldBe("la milagro");
        entities.GetProperty("earTag").GetString().ShouldBe("017683344");
        entities.GetProperty("color").GetString().ShouldBe("colorada");
        entities.GetProperty("lotId").GetInt32().ShouldBe(1);
        var owners = entities
            .GetProperty("ownerNames")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();
        owners.ShouldBe(["Carla", "Jaime"]);
    }

    [Test]
    public async Task Handle_HappyPath_RegisterNewborn_CompletesWithColorAndBirthDate()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "la bonita tuvo ternero macho colorado ayer");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            {
              "intent": "register_newborn",
              "confidence": 0.89,
              "motherId": 10,
              "sex": "male",
              "color": "colorado",
              "birthDate": "2024-05-21"
            }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        job.Status.ShouldBe("completed");
        var result = JsonSerializer.Deserialize<JsonElement>(
            job.ResultJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        result.GetProperty("intent").GetString().ShouldBe("register_newborn");
        var entities = result.GetProperty("entities");
        entities.GetProperty("motherId").GetInt32().ShouldBe(10);
        entities.GetProperty("color").GetString().ShouldBe("colorado");
        entities.GetProperty("birthDate").GetString().ShouldBe("2024-05-21");
    }

    // ── S3 cleanup ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_AfterSuccessfulCompletion_DeletesAudioFromS3()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            { "intent": "move_animal", "confidence": 0.9, "animalId": 10, "lotId": 1 }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        _mocker.GetMock<IStorageService>().Verify(s => s.DeleteFileAsync(job.S3Key), Times.Once);
    }

    [Test]
    public async Task Handle_WhenTranscriptIsEmpty_DeletesAudioFromS3()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        _mocker.GetMock<IStorageService>().Verify(s => s.DeleteFileAsync(job.S3Key), Times.Once);
    }

    [Test]
    public async Task Handle_WhenTranscriptionFails_DoesNotDeleteAudio()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(false);

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        _mocker
            .GetMock<IStorageService>()
            .Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── SaveChanges call count ─────────────────────────────────────────────────

    [Test]
    public async Task Handle_HappyPath_CallsSaveChangesExactlyTwice()
    {
        var job = BuildJob("pending");
        SetupJob(job);
        SetupS3(AudioBytes);
        SetupTranscription(true, "mover Rosa al lote norte");
        SetupRoster(RosterWithAnimalsAndLots);
        SetupIntentExtraction(
            true,
            """
            { "intent": "move_animal", "confidence": 0.9, "animalId": 10, "lotId": 1 }
            """
        );

        await _handler.Handle(new ProcessVoiceCommandCommand(job.Id, 1, 1), CancellationToken.None);

        // Once to set "processing", once to set "completed"
        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static VoiceCommandJob BuildJob(string status)
    {
        return new VoiceCommandJob
        {
            Id = Guid.NewGuid(),
            FarmId = 1,
            UserId = 5,
            S3Key = "voice-commands/temp/test.m4a",
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private void SetupJob(VoiceCommandJob job)
    {
        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    private void SetupS3(byte[] bytes)
    {
        _mocker
            .GetMock<IStorageService>()
            .Setup(s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);
    }

    private void SetupTranscription(bool success, string text = "")
    {
        var transcriptResponse = success
            ? new ExternalWorkerResponse(
                "id",
                ExternalWorkerOperations.TranscribeVoiceAudio,
                true,
                JsonSerializer.SerializeToElement(new { text }),
                null
            )
            : new ExternalWorkerResponse(
                "id",
                ExternalWorkerOperations.TranscribeVoiceAudio,
                false,
                null,
                "Whisper failed"
            );

        _mocker
            .GetMock<IExternalApiWorkerClient>()
            .Setup(c =>
                c.ExecuteAsync(
                    It.Is<ExternalWorkerRequest>(r =>
                        r.Operation == ExternalWorkerOperations.TranscribeVoiceAudio
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(transcriptResponse);
    }

    private void SetupRoster(FarmRosterDto roster)
    {
        _mocker
            .GetMock<IFarmRosterService>()
            .Setup(r => r.GetRosterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roster);
    }

    private void SetupIntentExtraction(bool success, string intentJson = "")
    {
        ExternalWorkerResponse response;
        if (success)
        {
            try
            {
                var doc = JsonDocument.Parse(intentJson);
                response = new ExternalWorkerResponse(
                    "id",
                    ExternalWorkerOperations.ExtractVoiceIntent,
                    true,
                    doc.RootElement.Clone(),
                    null
                );
            }
            catch (JsonException)
            {
                // intentionally malformed — return it as a string so deserialization fails later
                response = new ExternalWorkerResponse(
                    "id",
                    ExternalWorkerOperations.ExtractVoiceIntent,
                    true,
                    JsonSerializer.SerializeToElement(intentJson),
                    null
                );
            }
        }
        else
        {
            response = new ExternalWorkerResponse(
                "id",
                ExternalWorkerOperations.ExtractVoiceIntent,
                false,
                null,
                "GPT-4o failed"
            );
        }

        _mocker
            .GetMock<IExternalApiWorkerClient>()
            .Setup(c =>
                c.ExecuteAsync(
                    It.Is<ExternalWorkerRequest>(r =>
                        r.Operation == ExternalWorkerOperations.ExtractVoiceIntent
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);
    }

    private void VerifyGpt4oNotCalled()
    {
        _mocker
            .GetMock<IExternalApiWorkerClient>()
            .Verify(
                c =>
                    c.ExecuteAsync(
                        It.Is<ExternalWorkerRequest>(r =>
                            r.Operation == ExternalWorkerOperations.ExtractVoiceIntent
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
    }
}
