using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Delete;

[TestFixture]
public class DeleteAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeleteAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithPermission_SoftDeletesAnimal()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 5;
        var command = new DeleteAnimalCommand(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        animal.LifeStatus.ShouldBe(LifeStatus.Deleted);
        _animalRepositoryMock.Verify(r => r.Update(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 5;
        var command = new DeleteAnimalCommand(animalId, userId);

        _animalRepositoryMock
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found or access denied.");
    }
}
