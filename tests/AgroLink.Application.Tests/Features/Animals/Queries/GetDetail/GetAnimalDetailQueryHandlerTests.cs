using System.Linq.Expressions;
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
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _storageServiceMock = new Mock<IStorageService>();
        _handler = new GetAnimalDetailQueryHandler(
            _animalRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _storageServiceMock.Object
        );

        // Default setup for successful authorization
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(1);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private GetAnimalDetailQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimal_ReturnsDetailDto()
    {
        // Arrange
        var birthDate = DateTime.UtcNow.AddYears(-2);
        var animal = new Animal
        {
            Id = 1,
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

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(1)).ReturnsAsync(animal);
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
        var result = await _handler.Handle(new GetAnimalDetailQuery(1), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
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
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var animal = new Animal
        {
            Id = 1,
            Lot = new Lot { Paddock = new Paddock { FarmId = 100 } },
        };
        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(1)).ReturnsAsync(animal);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(new GetAnimalDetailQuery(1), CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(99)).ReturnsAsync((Animal?)null);

        var result = await _handler.Handle(new GetAnimalDetailQuery(99), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_AnimalBornAlmostAMonthAgo_ReturnsZeroMonths()
    {
        // Arrange
        // Born 20 days ago (should be 0 months old)
        var birthDate = DateTime.UtcNow.AddDays(-20);
        var animal = new Animal
        {
            Id = 2,
            TagVisual = "Calf",
            BirthDate = birthDate,
            Sex = Sex.Female,
            Lot = new Lot
            {
                Name = "Nursery",
                Paddock = new Paddock { FarmId = 100 },
            },
        };

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(2)).ReturnsAsync(animal);

        // Act
        var result = await _handler.Handle(new GetAnimalDetailQuery(2), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AgeInMonths.ShouldBe(0);
    }

    [Test]
    public async Task Handle_AnimalBornOneMonthAndOneDayAgo_ReturnsOneMonth()
    {
        // Arrange
        // Born 1 month and 1 day ago
        var birthDate = DateTime.UtcNow.AddMonths(-1).AddDays(-1);
        var animal = new Animal
        {
            Id = 3,
            TagVisual = "Calf2",
            BirthDate = birthDate,
            Sex = Sex.Male,
            Lot = new Lot
            {
                Name = "Nursery",
                Paddock = new Paddock { FarmId = 100 },
            },
        };

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(3)).ReturnsAsync(animal);

        // Act
        var result = await _handler.Handle(new GetAnimalDetailQuery(3), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AgeInMonths.ShouldBe(1);
    }
}
