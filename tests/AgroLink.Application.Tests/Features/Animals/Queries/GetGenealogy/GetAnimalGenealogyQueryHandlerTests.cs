using System.Linq.Expressions;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetGenealogy;

[TestFixture]
public class GetAnimalGenealogyQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetAnimalGenealogyQueryHandler(
            _animalRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private GetAnimalGenealogyQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithGenealogy_ReturnsAnimalGenealogyDto()
    {
        // Arrange
        var animalId = 1;
        var query = new GetAnimalGenealogyQuery(animalId);

        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Animal",
            Sex = Sex.Male,
            BirthDate = DateTime.Now.AddYears(-2),
            LotId = 1,
            Lot = new Lot { Paddock = new Paddock { FarmId = 1 } },
        };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(1);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(animalId))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        var animalId = 999;
        var query = new GetAnimalGenealogyQuery(animalId);

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
