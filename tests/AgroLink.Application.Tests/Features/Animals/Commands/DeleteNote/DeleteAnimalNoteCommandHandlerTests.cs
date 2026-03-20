using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.DeleteNote;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.DeleteNote;

[TestFixture]
public class DeleteAnimalNoteCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteAnimalNoteCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteAnimalNoteCommandHandler _handler = null!;

    [Test]
    public async Task Handle_AuthorDeletesNote_RemovesNote()
    {
        // Arrange
        const int animalId = 1;
        const int noteId = 10;
        const int userId = 5;
        var command = new DeleteAnimalNoteCommand(animalId, noteId, userId);
        var note = new AnimalNote
        {
            Id = noteId,
            AnimalId = animalId,
            UserId = userId,
        };

        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByIdAsync(noteId))
            .ReturnsAsync(note);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IAnimalNoteRepository>().Verify(r => r.Remove(note), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NoteNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new DeleteAnimalNoteCommand(1, 999, 5);

        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((AnimalNote?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NoteForDifferentAnimal_ThrowsNotFoundException()
    {
        // Arrange
        const int noteId = 10;
        var command = new DeleteAnimalNoteCommand(99, noteId, 5);
        var note = new AnimalNote
        {
            Id = noteId,
            AnimalId = 1, // belongs to animal 1, not 99
            UserId = 5,
        };

        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByIdAsync(noteId))
            .ReturnsAsync(note);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NonAuthorTriesToDelete_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int animalId = 1;
        const int noteId = 10;
        var command = new DeleteAnimalNoteCommand(animalId, noteId, 99);
        var note = new AnimalNote
        {
            Id = noteId,
            AnimalId = animalId,
            UserId = 5, // original author
        };

        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByIdAsync(noteId))
            .ReturnsAsync(note);

        // Act & Assert
        var ex = await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("own notes");
    }
}
