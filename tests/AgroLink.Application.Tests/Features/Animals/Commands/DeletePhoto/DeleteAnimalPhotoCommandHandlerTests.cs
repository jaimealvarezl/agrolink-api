using AgroLink.Application.Features.Animals.Commands.DeletePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;

namespace AgroLink.Application.Tests.Features.Animals.Commands.DeletePhoto;

[TestFixture]
public class DeleteAnimalPhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteAnimalPhotoCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteAnimalPhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_DeletesFromStorageAndDb()
    {
        // Arrange
        const int animalId = 1;
        const int photoId = 100;
        const int userId = 5;
        var command = new DeleteAnimalPhotoCommand(animalId, photoId, userId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };
        var photo = new AnimalPhoto
        {
            Id = photoId,
            AnimalId = animalId,
            UriRemote = "http://storage.com/bucket/file.jpg",
            StorageKey = "bucket/file.jpg",
        };

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
            .GetMock<IStorageService>()
            .Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Once);
        _mocker.GetMock<IAnimalPhotoRepository>().Verify(r => r.Remove(photo), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Test]
    public async Task Handle_PhotoIsProfile_SetsAnotherAsProfile()
    {
        // Arrange
        const int animalId = 1;
        const int photoId = 100;
        const int otherPhotoId = 101;
        const int userId = 5;
        var command = new DeleteAnimalPhotoCommand(animalId, photoId, userId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };
        var photo = new AnimalPhoto
        {
            Id = photoId,
            AnimalId = animalId,
            IsProfile = true,
        };
        var otherPhoto = new AnimalPhoto
        {
            Id = otherPhotoId,
            AnimalId = animalId,
            IsProfile = false,
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.GetByIdAsync(photoId))
            .ReturnsAsync(photo);
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalPhoto> { otherPhoto });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Verify(r => r.SetProfilePhotoAsync(animalId, otherPhotoId), Times.Once);
    }
}
