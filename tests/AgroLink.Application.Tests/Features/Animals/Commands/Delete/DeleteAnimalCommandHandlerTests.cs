using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Domain.Entities;
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
    public async Task Handle_ExistingAnimal_DeletesAnimal()
    {
        // Arrange
        var animalId = 1;
        var command = new DeleteAnimalCommand(animalId);
        var animal = new Animal { Id = animalId };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _animalRepositoryMock.Setup(r => r.Remove(animal));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _animalRepositoryMock.Verify(r => r.GetByIdAsync(animalId), Times.Once);
        _animalRepositoryMock.Verify(r => r.Remove(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        var animalId = 999;
        var command = new DeleteAnimalCommand(animalId);

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found");
        _animalRepositoryMock.Verify(r => r.Remove(It.IsAny<Animal>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
