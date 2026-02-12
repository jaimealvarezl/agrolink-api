using System.Linq.Expressions;
using AgroLink.Application.Features.Animals.Commands.DeletePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgroLink.Application.Tests.Features.Animals.Commands.DeletePhoto;

[TestFixture]
public class DeleteAnimalPhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteAnimalPhotoCommandHandler>>();

        _handler = new DeleteAnimalPhotoCommandHandler(
            _animalRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _storageServiceMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<ILogger<DeleteAnimalPhotoCommandHandler>> _loggerMock = null!;
    private DeleteAnimalPhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_DeletesFromStorageAndDb()
    {
        // Arrange
        var animalId = 1;
        var photoId = 100;
        var command = new DeleteAnimalPhotoCommand(animalId, photoId);

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

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Once);
        _animalPhotoRepositoryMock.Verify(r => r.Remove(photo), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Test]
    public async Task Handle_PhotoIsProfile_SetsAnotherAsProfile()
    {
        // Arrange
        var animalId = 1;
        var photoId = 100;
        var otherPhotoId = 101;
        var command = new DeleteAnimalPhotoCommand(animalId, photoId);

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

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);
        _animalPhotoRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalPhoto> { otherPhoto });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _animalPhotoRepositoryMock.Verify(
            r => r.SetProfilePhotoAsync(animalId, otherPhotoId),
            Times.Once
        );
    }
}
