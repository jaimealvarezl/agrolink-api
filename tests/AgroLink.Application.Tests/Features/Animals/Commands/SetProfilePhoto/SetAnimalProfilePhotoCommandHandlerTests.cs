using AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.SetProfilePhoto;

[TestFixture]
public class SetAnimalProfilePhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SetAnimalProfilePhotoCommandHandler(
            _animalRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private SetAnimalProfilePhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_SetsProfilePhoto()
    {
        // Arrange
        const int animalId = 1;
        const int photoId = 100;
        const int userId = 5;
        var command = new SetAnimalProfilePhotoCommand(animalId, photoId, userId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };
        var photo = new AnimalPhoto { Id = photoId, AnimalId = animalId };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _animalPhotoRepositoryMock.Verify(
            r => r.SetProfilePhotoAsync(animalId, photoId),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_PhotoDoesNotBelongToAnimal_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        const int photoId = 100;
        const int userId = 5;
        var command = new SetAnimalProfilePhotoCommand(animalId, photoId, userId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };
        var photo = new AnimalPhoto { Id = photoId, AnimalId = 2 }; // Different animal

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
