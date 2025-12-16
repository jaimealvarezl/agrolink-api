using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetById;

[TestFixture]
public class GetAnimalByIdQueryHandlerTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private GetAnimalByIdQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _handler = new GetAnimalByIdQueryHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _photoRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ExistingAnimal_ReturnsAnimalDto()
    {
        // Arrange
        var animalId = 1;
        var query = new GetAnimalByIdQuery(animalId);
        var animal = new Animal
        {
            Id = animalId,
            Tag = "A001",
            Name = "Animal 1",
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
            Status = "ACTIVE",
        };
        var lot = new Lot { Id = 1, Name = "Test Lot" };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(animal.LotId)).ReturnsAsync(lot);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.Tag.ShouldBe(animal.Tag);
        result.LotName.ShouldBe(lot.Name);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        var animalId = 999;
        var query = new GetAnimalByIdQuery(animalId);

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
