using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetAll;

[TestFixture]
public class GetAllAnimalsQueryHandlerTests
{
    private AutoMocker _mocker = null!;
    private GetAllAnimalsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAllAnimalsQueryHandler>();
    }

    [Test]
    public async Task Handle_ReturnsAllAnimals()
    {
        // Arrange
        var userId = 1;
        var query = new GetAllAnimalsQuery(userId);
        var lot1 = new Lot { Id = 1, Name = "Lot 1" };
        var lot2 = new Lot { Id = 2, Name = "Lot 2" };
        var animals = new List<Animal>
        {
            new()
            {
                Id = 1,
                TagVisual = "A001",
                Cuia = "CUIA-A001",
                Name = "Animal 1",
                LotId = 1,
                Lot = lot1,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                AnimalOwners = [],
                Photos = [],
            },
            new()
            {
                Id = 2,
                TagVisual = "A002",
                Cuia = "CUIA-A002",
                Name = "Animal 2",
                LotId = 2,
                Lot = lot2,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                AnimalOwners = [],
                Photos = [],
            },
        };

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.GetAllByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animals);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().TagVisual.ShouldBe("A001");
        result.First().LotName.ShouldBe("Lot 1");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoAnimalsExist()
    {
        // Arrange
        var userId = 1;
        var query = new GetAllAnimalsQuery(userId);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.GetAllByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
