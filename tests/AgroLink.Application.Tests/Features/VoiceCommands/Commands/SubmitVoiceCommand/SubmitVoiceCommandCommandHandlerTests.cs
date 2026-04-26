using AgroLink.Application.Features.VoiceCommands.Commands.SubmitVoiceCommand;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.VoiceCommands.Commands.SubmitVoiceCommand;

[TestFixture]
public class SubmitVoiceCommandCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<SubmitVoiceCommandCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private SubmitVoiceCommandCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_UploadsToS3CreatesJobAndReturnsGuid()
    {
        // Arrange
        var command = new SubmitVoiceCommandCommand(
            1,
            5,
            new MemoryStream(new byte[2048]),
            "audio/m4a",
            2048
        );

        _mocker
            .GetMock<IStoragePathProvider>()
            .Setup(p => p.GetVoiceAudioPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("voice-commands/temp/test-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBe(Guid.Empty);

        _mocker
            .GetMock<IStorageService>()
            .Verify(
                s =>
                    s.UploadFileAsync(
                        "voice-commands/temp/test-id",
                        It.IsAny<Stream>(),
                        "audio/m4a",
                        2048
                    ),
                Times.Once
            );

        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Verify(
                r =>
                    r.AddAsync(
                        It.Is<VoiceCommandJob>(j =>
                            j.FarmId == 1
                            && j.UserId == 5
                            && j.Status == "pending"
                            && j.S3Key == "voice-commands/temp/test-id"
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mocker
            .GetMock<IVoiceCommandQueue>()
            .Verify(q => q.EnqueueAsync(result, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WhenS3UploadFails_PropagatesExceptionWithoutSavingJob()
    {
        // Arrange
        var command = new SubmitVoiceCommandCommand(
            1,
            5,
            new MemoryStream(new byte[2048]),
            "audio/m4a",
            2048
        );

        _mocker
            .GetMock<IStoragePathProvider>()
            .Setup(p => p.GetVoiceAudioPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("voice-commands/temp/test-id");

        _mocker
            .GetMock<IStorageService>()
            .Setup(s =>
                s.UploadFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<long>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("S3 unavailable"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );

        _mocker
            .GetMock<IVoiceCommandJobRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<VoiceCommandJob>(), It.IsAny<CancellationToken>()),
                Times.Never
            );

        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
