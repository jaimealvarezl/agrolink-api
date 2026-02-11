using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.UploadPhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.UploadPhoto;

[TestFixture]
public class UploadAnimalPhotoCommandHandlerTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<IStoragePathProvider> _pathProviderMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UploadAnimalPhotoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _pathProviderMock = new Mock<IStoragePathProvider>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UploadAnimalPhotoCommandHandler(
            _animalRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _storageServiceMock.Object,
            _pathProviderMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidRequest_UploadsAndReturnsDto()
    {
        // Arrange
        var animalId = 1;
        var farmId = 10;
        var userId = 5;
        var command = new UploadAnimalPhotoCommand(
            animalId,
            new MemoryStream(),
            "photo.jpg",
            "image/jpeg",
            1024,
            "Test Photo"
        );

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalPhotoRepositoryMock.Setup(r => r.HasPhotosAsync(animalId)).ReturnsAsync(false);
        _pathProviderMock
            .Setup(p =>
                p.GetAnimalPhotoPath(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()
                )
            )
            .Returns("path/to/photo.jpg");
        _storageServiceMock
            .Setup(s => s.GetFileUrl(It.IsAny<string>()))
            .Returns("http://storage.com/photo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UriRemote.ShouldBe("http://storage.com/photo.jpg");
        result.IsProfile.ShouldBeTrue(); // First photo
        _storageServiceMock.Verify(
            s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
    }

    [Test]
    public async Task Handle_NoPermissions_ThrowsForbiddenAccessException()
    {
        // Arrange
        var animalId = 1;
        var command = new UploadAnimalPhotoCommand(
            animalId,
            new MemoryStream(),
            "p.jpg",
            "img",
            100
        );
        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
