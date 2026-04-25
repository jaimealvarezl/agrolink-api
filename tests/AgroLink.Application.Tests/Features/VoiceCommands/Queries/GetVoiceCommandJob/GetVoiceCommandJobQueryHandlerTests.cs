using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.VoiceCommands.Queries.GetVoiceCommandJob;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.VoiceCommands.Queries.GetVoiceCommandJob;

[TestFixture]
public class GetVoiceCommandJobQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetVoiceCommandJobQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetVoiceCommandJobQueryHandler _handler = null!;

    [Test]
    public async Task Handle_PendingJob_ReturnsPendingStatus()
    {
        var jobId = Guid.NewGuid();
        SetupJob(
            new VoiceCommandJob
            {
                Id = jobId,
                UserId = 1,
                Status = "pending",
            }
        );

        var result = await _handler.Handle(
            new GetVoiceCommandJobQuery(jobId, 1),
            CancellationToken.None
        );

        result.Status.ShouldBe("pending");
        result.Result.ShouldBeNull();
        result.Error.ShouldBeNull();
    }

    [Test]
    public async Task Handle_FailedJob_ReturnsErrorMessage()
    {
        var jobId = Guid.NewGuid();
        SetupJob(
            new VoiceCommandJob
            {
                Id = jobId,
                UserId = 1,
                Status = "failed",
                ErrorMessage = "Transcription service unavailable.",
            }
        );

        var result = await _handler.Handle(
            new GetVoiceCommandJobQuery(jobId, 1),
            CancellationToken.None
        );

        result.Status.ShouldBe("failed");
        result.Error.ShouldBe("Transcription service unavailable.");
        result.Result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_CompletedJob_DeserializesResult()
    {
        var jobId = Guid.NewGuid();
        const string resultJson =
            """{"Intent":"move_animal","Confidence":0.91,"Entities":{},"RawTranscription":"mover negrita al lote norte"}""";

        SetupJob(
            new VoiceCommandJob
            {
                Id = jobId,
                UserId = 1,
                Status = "completed",
                ResultJson = resultJson,
            }
        );

        var result = await _handler.Handle(
            new GetVoiceCommandJobQuery(jobId, 1),
            CancellationToken.None
        );

        result.Status.ShouldBe("completed");
        result.Result.ShouldNotBeNull();
        result.Result!.Intent.ShouldBe("move_animal");
        result.Result.Confidence.ShouldBe(0.91);
        result.Result.RawTranscription.ShouldBe("mover negrita al lote norte");
    }

    [Test]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandJob?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(new GetVoiceCommandJobQuery(jobId, 1), CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_JobBelongsToDifferentUser_ThrowsForbiddenAccessException()
    {
        var jobId = Guid.NewGuid();
        SetupJob(
            new VoiceCommandJob
            {
                Id = jobId,
                UserId = 99,
                Status = "pending",
            }
        );

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(new GetVoiceCommandJobQuery(jobId, 1), CancellationToken.None)
        );
    }

    private void SetupJob(VoiceCommandJob job)
    {
        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }
}
