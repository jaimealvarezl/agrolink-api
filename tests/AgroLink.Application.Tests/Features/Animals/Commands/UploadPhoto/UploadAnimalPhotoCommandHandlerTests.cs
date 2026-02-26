using AgroLink.Application.Features.Animals.Commands.UploadPhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.UploadPhoto;

[TestFixture]
public class UploadAnimalPhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UploadAnimalPhotoCommandHandler>();
    }

    private AutoMocker _mocker = null!;
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
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.HasPhotosAsync(animalId))
            .ReturnsAsync(false);
        _mocker
            .GetMock<IStoragePathProvider>()
            .Setup(p =>
                p.GetAnimalPhotoPath(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()
                )
            )
            .Returns("path/to/photo.jpg");
        _mocker
            .GetMock<IStorageService>()
            .Setup(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns("http://storage.com/photo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UriRemote.ShouldBe("http://storage.com/photo.jpg");
        result.IsProfile.ShouldBeTrue(); // First photo
        _mocker
            .GetMock<IStorageService>()
            .Verify(
                s =>
                    s.UploadFileAsync(
                        It.IsAny<string>(),
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<long>()
                    ),
                Times.Once
            );
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
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

        _mocker
            .GetMock<IAnimalRepository>()
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
