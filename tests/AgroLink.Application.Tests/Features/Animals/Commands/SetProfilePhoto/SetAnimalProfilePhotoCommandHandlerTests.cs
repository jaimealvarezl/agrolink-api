using AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.SetProfilePhoto;

[TestFixture]
public class SetAnimalProfilePhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<SetAnimalProfilePhotoCommandHandler>();
    }

    private AutoMocker _mocker = null!;
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

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.GetByIdAsync(photoId))
            .ReturnsAsync(photo);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Verify(r => r.SetProfilePhotoAsync(animalId, photoId), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
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

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.GetByIdAsync(photoId))
            .ReturnsAsync(photo);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
