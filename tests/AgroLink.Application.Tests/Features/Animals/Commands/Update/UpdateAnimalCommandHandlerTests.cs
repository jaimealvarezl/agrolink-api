using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Update;

[TestFixture]
public class UpdateAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _photoRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdateAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        var animalId = 1;
        var updateAnimalDto = new UpdateAnimalDto
        {
            Name = "Updated Name",
            LifeStatus = "Sold",
            Owners = new List<AnimalOwnerDto>
            {
                new()
                {
                    OwnerId = 1,
                    OwnerName = "Test Owner",
                    SharePercent = 100,
                },
            },
        };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Old Name",
            LotId = 1,
            LifeStatus = Domain.Enums.LifeStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        var lot = new Lot { Id = 1, Name = "Test Lot" };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _ownerRepositoryMock.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _animalOwnerRepositoryMock
            .Setup(r => r.RemoveByAnimalIdAsync(animalId))
            .Returns(Task.CompletedTask);
        _animalOwnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AnimalOwner>()))
            .Returns(Task.CompletedTask);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(
                new List<AnimalOwner>
                {
                    new()
                    {
                        AnimalId = 1,
                        OwnerId = 1,
                        SharePercent = 100,
                    },
                }
            );
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.Name.ShouldBe(updateAnimalDto.Name);
        result.LifeStatus.ShouldBe(updateAnimalDto.LifeStatus);
        result.Owners.Count.ShouldBe(1);
        _animalRepositoryMock.Verify(r => r.Update(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _animalOwnerRepositoryMock.Verify(r => r.RemoveByAnimalIdAsync(animalId), Times.Once);
        _animalOwnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AnimalOwner>()), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        var animalId = 999;
        var updateAnimalDto = new UpdateAnimalDto { Name = "Updated Name" };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found");
        _animalRepositoryMock.Verify(r => r.Update(It.IsAny<Animal>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
