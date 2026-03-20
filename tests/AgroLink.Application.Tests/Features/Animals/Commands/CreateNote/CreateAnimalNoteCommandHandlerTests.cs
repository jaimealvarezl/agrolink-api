using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.CreateNote;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.CreateNote;

[TestFixture]
public class CreateAnimalNoteCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateAnimalNoteCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateAnimalNoteCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCommand_CreatesNoteAndReturnsDto()
    {
        // Arrange
        const int farmId = 10;
        const int animalId = 1;
        const int userId = 5;
        var command = new CreateAnimalNoteCommand(farmId, animalId, "Cow looks healthy", userId);
        var animal = new Animal { Id = animalId };
        var user = new User { Id = userId, Name = "Dr. Smith" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(animalId, farmId))
            .ReturnsAsync(animal);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AnimalId.ShouldBe(animalId);
        result.Content.ShouldBe("Cow looks healthy");
        result.UserId.ShouldBe(userId);
        result.UserName.ShouldBe("Dr. Smith");
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Verify(r => r.AddAsync(It.IsAny<AnimalNote>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_AnimalNotFoundInFarm_ThrowsNotFoundException()
    {
        // Arrange — animal 999 not found in farm 10 (returns null)
        var command = new CreateAnimalNoteCommand(10, 999, "Note", 5);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(999, 10))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
