using AgroLink.Application.Features.Animals.Queries.GetDetail;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetDetail;

[TestFixture]
public class GetAnimalDetailQueryHandlerTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalDetailQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalDetailQueryHandler(_animalRepositoryMock.Object);
    }

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
            Lot = new Lot { Name = "Pasture 1" },
            Mother = new Animal { TagVisual = "M1", Name = "Mom" },
            Father = new Animal { TagVisual = "F1", Name = "Dad" },
            AnimalOwners = new List<AnimalOwner>
            {
                new() { Owner = new Owner { Name = "John Doe" }, SharePercent = 100 }
            },
            Photos = new List<Photo>
            {
                new() { UriRemote = "http://example.com/photo.jpg" }
            }
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(1))
            .ReturnsAsync(animal);

        // Act
        var result = await _handler.Handle(new GetAnimalDetailQuery(1), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.MotherName.ShouldBe("Mom");
        result.FatherName.ShouldBe("Dad");
        result.Owners.Count.ShouldBe(1);
        result.Owners[0].OwnerName.ShouldBe("John Doe");
        result.AgeInMonths.ShouldBe(24);
        result.PrimaryPhotoUrl.ShouldBe("http://example.com/photo.jpg");
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
         _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(99))
            .ReturnsAsync((Animal?)null);
            
         var result = await _handler.Handle(new GetAnimalDetailQuery(99), CancellationToken.None);
         
         result.ShouldBeNull();
    }
}
