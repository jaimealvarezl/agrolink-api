using AgroLink.Application.Features.Animals.Queries.GetDetail;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetDetail;

[TestFixture]
public class GetAnimalDetailQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _handler = new GetAnimalDetailQueryHandler(
            _animalRepositoryMock.Object,
            _storageServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private GetAnimalDetailQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimal_ReturnsDetailDto()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        var birthDate = DateTime.UtcNow.AddYears(-2);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A1",
            Name = "Betsy",
            BirthDate = birthDate,
            LotId = 10,
            Lot = new Lot
            {
                Name = "Pasture 1",
                Paddock = new Paddock { FarmId = 100 },
            },
            Mother = new Animal
            {
                TagVisual = "M1",
                Name = "Mom",
                Photos = new List<AnimalPhoto>
                {
                    new() { StorageKey = "mom-key", IsProfile = true },
                },
            },
            Father = new Animal
            {
                TagVisual = "F1",
                Name = "Dad",
                Photos = new List<AnimalPhoto>
                {
                    new() { StorageKey = "dad-key", IsProfile = true },
                },
            },
            AnimalOwners = new List<AnimalOwner>
            {
                new()
                {
                    Owner = new Owner { Name = "John Doe" },
                    SharePercent = 100,
                },
            },
            Photos = new List<AnimalPhoto>
            {
                new()
                {
                    StorageKey = "photo-key",
                    UriRemote = "http://example.com/photo.jpg",
                    ContentType = "image/jpeg",
                },
            },
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _storageServiceMock
            .Setup(s => s.GetPresignedUrl("photo-key", It.IsAny<TimeSpan>()))
            .Returns("http://signed-url.com/photo.jpg");
        _storageServiceMock
            .Setup(s => s.GetPresignedUrl("mom-key", It.IsAny<TimeSpan>()))
            .Returns("http://signed-url.com/mom.jpg");
        _storageServiceMock
            .Setup(s => s.GetPresignedUrl("dad-key", It.IsAny<TimeSpan>()))
            .Returns("http://signed-url.com/dad.jpg");

        // Act
        var result = await _handler.Handle(
            new GetAnimalDetailQuery(animalId, userId),
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.MotherName.ShouldBe("Mom");
        result.MotherPhotoUrl.ShouldBe("http://signed-url.com/mom.jpg");
        result.FatherName.ShouldBe("Dad");
        result.FatherPhotoUrl.ShouldBe("http://signed-url.com/dad.jpg");
        result.Owners.Count.ShouldBe(1);
        result.Owners[0].OwnerName.ShouldBe("John Doe");
        result.AgeInMonths.ShouldBe(24);
        result.PrimaryPhotoUrl.ShouldBe("http://signed-url.com/photo.jpg");
        result.Photos.ShouldNotBeNull();
        result.Photos.Count.ShouldBe(1);
        result.Photos[0].UriRemote.ShouldBe("http://signed-url.com/photo.jpg");
    }

    [Test]
    public async Task Handle_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(
            new GetAnimalDetailQuery(animalId, userId),
            CancellationToken.None
        );

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        const int animalId = 99;
        const int userId = 1;
        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        var result = await _handler.Handle(
            new GetAnimalDetailQuery(animalId, userId),
            CancellationToken.None
        );

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_AnimalBornAlmostAMonthAgo_ReturnsZeroMonths()
    {
        // Arrange
        const int animalId = 2;
        const int userId = 1;
        var birthDate = DateTime.UtcNow.AddDays(-20);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "Calf",
            BirthDate = birthDate,
            Sex = Sex.Female,
            Lot = new Lot
            {
                Name = "Nursery",
                Paddock = new Paddock { FarmId = 100 },
            },
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);

        // Act
        var result = await _handler.Handle(
            new GetAnimalDetailQuery(animalId, userId),
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.AgeInMonths.ShouldBe(0);
    }

    [Test]
    public async Task Handle_AnimalBornOneMonthAndOneDayAgo_ReturnsOneMonth()
    {
        // Arrange
        const int animalId = 3;
        const int userId = 1;
        var birthDate = DateTime.UtcNow.AddMonths(-1).AddDays(-1);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "Calf2",
            BirthDate = birthDate,
            Sex = Sex.Male,
            Lot = new Lot
            {
                Name = "Nursery",
                Paddock = new Paddock { FarmId = 100 },
            },
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);

        // Act
        var result = await _handler.Handle(
            new GetAnimalDetailQuery(animalId, userId),
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.AgeInMonths.ShouldBe(1);
    }
}
