using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Delete;

[TestFixture]
public class DeleteAnimalCommandHandlerTests
{
    private AutoMocker _mocker = null!;
    private DeleteAnimalCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteAnimalCommandHandler>();
    }

    [Test]
    public async Task Handle_ValidDeleteAnimalCommand_PerformsSoftDelete()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 5;
        var command = new DeleteAnimalCommand(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            LifeStatus = LifeStatus.Active,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } }
        };

        _mocker.GetMock<IAnimalRepository>().Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        animal.LifeStatus.ShouldBe(LifeStatus.Deleted);
        _mocker.GetMock<IAnimalRepository>().Verify(r => r.Update(animal), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 5;
        var command = new DeleteAnimalCommand(animalId, userId);

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldBe("Animal not found or access denied.");
    }
}
