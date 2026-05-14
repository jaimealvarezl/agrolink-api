using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.CreateBcsReading;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.CreateBcsReading;

[TestFixture]
public class CreateBcsReadingCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateBcsReadingCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateBcsReadingCommandHandler _handler = null!;

    private static CreateBcsReadingCommand BuildCommand(
        int farmId = 10,
        int animalId = 1,
        int userId = 5,
        double score = 3.0,
        BcsReadingSource source = BcsReadingSource.AI,
        bool hasAlerts = false,
        string? alertDescription = null,
        string? rawAiResponse = null
    )
    {
        return new CreateBcsReadingCommand(
            farmId,
            animalId,
            userId,
            new CreateBcsReadingDto
            {
                Score = score,
                Source = source,
                HasAlerts = hasAlerts,
                AlertDescription = alertDescription,
                RawAiResponse = rawAiResponse,
            }
        );
    }

    [Test]
    public async Task Handle_ValidCommand_WritesBcsReadingAndBcsNote()
    {
        // Arrange
        var animal = new Animal { Id = 1 };
        var user = new User { Id = 5, Name = "Dr. Smith" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AnimalId.ShouldBe(1);
        result.Score.ShouldBe(3.0);
        result.Source.ShouldBe(BcsReadingSource.AI);
        result.ConfirmedByUserId.ShouldBe(5);

        _mocker
            .GetMock<IAnimalBcsReadingRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<AnimalBcsReading>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

        // One note for BCS confirmation, none for alert
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<AnimalNote>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WithAlerts_WritesBothNotes()
    {
        // Arrange
        var animal = new Animal { Id = 1 };
        var user = new User { Id = 5, Name = "Dr. Smith" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = BuildCommand(
            hasAlerts: true,
            alertDescription: "Garrapatas visibles en lomo."
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — two notes: BCS confirmation + alert
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<AnimalNote>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2)
            );
    }

    [Test]
    public async Task Handle_HasAlertsTrue_ButNoAlertDescription_WritesOnlyBcsNote()
    {
        // hasAlerts=true but empty description — alert note must not be created
        var animal = new Animal { Id = 1 };
        var user = new User { Id = 5, Name = "Dr. Smith" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = BuildCommand(hasAlerts: true, alertDescription: null);

        await _handler.Handle(command, CancellationToken.None);

        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<AnimalNote>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
    }

    [Test]
    public async Task Handle_BcsNoteContent_ContainsFormattedScoreAndUserName()
    {
        // Arrange
        var animal = new Animal { Id = 1 };
        var user = new User { Id = 5, Name = "María García" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        AnimalNote? capturedNote = null;
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.AddAsync(It.IsAny<AnimalNote>(), It.IsAny<CancellationToken>()))
            .Callback<AnimalNote, CancellationToken>((note, _) => capturedNote = note);

        // Act
        await _handler.Handle(BuildCommand(score: 3.5), CancellationToken.None);

        // Assert
        capturedNote.ShouldNotBeNull();
        capturedNote!.Content.ShouldBe("CC 3.5 — Análisis IA confirmado por María García");
    }

    [Test]
    public async Task Handle_ManualSource_BcsNoteUsesLecturaManual()
    {
        var animal = new Animal { Id = 1 };
        var user = new User { Id = 5, Name = "Juan López" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        AnimalNote? capturedNote = null;
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.AddAsync(It.IsAny<AnimalNote>(), It.IsAny<CancellationToken>()))
            .Callback<AnimalNote, CancellationToken>((note, _) => capturedNote = note);

        await _handler.Handle(
            BuildCommand(score: 4.0, source: BcsReadingSource.Manual),
            CancellationToken.None
        );

        capturedNote.ShouldNotBeNull();
        capturedNote!.Content.ShouldBe("CC 4.0 — Lectura manual confirmado por Juan López");
    }

    [Test]
    public async Task Handle_ScoreBelowMin_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(score: 0.9), CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_ScoreAboveMax_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(score: 5.1), CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_AnimalNotFoundInFarm_ThrowsNotFoundException()
    {
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(999, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Animal?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(BuildCommand(animalId: 999), CancellationToken.None)
        );
    }
}
