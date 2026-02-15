using AgroLink.Application.Features.Animals.Commands.UploadPhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.UploadPhoto;

[TestFixture]
public class UploadAnimalPhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _pathProviderMock = new Mock<IStoragePathProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UploadAnimalPhotoCommandHandler>>();

        _handler = new UploadAnimalPhotoCommandHandler(
            _animalRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _storageServiceMock.Object,
            _pathProviderMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<IStoragePathProvider> _pathProviderMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<ILogger<UploadAnimalPhotoCommandHandler>> _loggerMock = null!;
    private UploadAnimalPhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_UploadsAndReturnsDto()
    {
        // Arrange
        var animalId = 1;
        var farmId = 10;
        var userId = 5;

        // Create a stream with valid JPEG header
        var jpegHeader = new byte[]
        {
            0xFF,
            0xD8,
            0xFF,
            0xE0,
            0x00,
            0x10,
            0x4A,
            0x46,
            0x49,
            0x46,
            0x00,
            0x01,
        };
        var stream = new MemoryStream();
        stream.Write(jpegHeader, 0, jpegHeader.Length);
        stream.Position = 0;

        var command = new UploadAnimalPhotoCommand(
            animalId,
            stream,
            "photo.jpg",
            "image/jpeg",
            stream.Length,
            userId,
            "Test Photo"
        );

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
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
            .Setup(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns("http://storage.com/photo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UriRemote.ShouldBe("http://storage.com/photo.jpg");
        result.IsProfile.ShouldBeTrue(); // First photo
        _storageServiceMock.Verify(
            s =>
                s.UploadFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<long>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
    }

    [Test]
    public async Task Handle_NoPermissions_ThrowsArgumentException()
    {
        // Arrange
        var animalId = 1;

        // Create a stream with valid JPEG header
        var jpegHeader = new byte[]
        {
            0xFF,
            0xD8,
            0xFF,
            0xE0,
            0x00,
            0x10,
            0x4A,
            0x46,
            0x49,
            0x46,
            0x00,
            0x01,
        };
        var stream = new MemoryStream();
        stream.Write(jpegHeader, 0, jpegHeader.Length);
        stream.Position = 0;

        var command = new UploadAnimalPhotoCommand(
            animalId,
            stream,
            "p.jpg",
            "image/jpeg",
            stream.Length,
            5
        );

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, 5))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidExtension_ThrowsArgumentException()
    {
        // Arrange
        var command = new UploadAnimalPhotoCommand(
            1,
            new MemoryStream(),
            "file.exe",
            "image/jpeg",
            100,
            1
        );

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidMimeType_ThrowsArgumentException()
    {
        // Arrange
        var command = new UploadAnimalPhotoCommand(
            1,
            new MemoryStream(),
            "file.jpg",
            "text/html",
            100,
            1
        );

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidSignature_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream(new byte[12]); // All zeros
        var command = new UploadAnimalPhotoCommand(
            1,
            stream,
            "p.jpg",
            "image/jpeg",
            stream.Length,
            1
        );

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
