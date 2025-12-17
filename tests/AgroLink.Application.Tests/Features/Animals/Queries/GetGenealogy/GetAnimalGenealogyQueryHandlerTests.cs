using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetGenealogy;

[TestFixture]
public class GetAnimalGenealogyQueryHandlerTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalGenealogyQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalGenealogyQueryHandler(_animalRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingAnimalWithGenealogy_ReturnsAnimalGenealogyDto()
    {
        // Arrange
        var animalId = 1;
        var query = new GetAnimalGenealogyQuery(animalId);

        var child = new Animal
        {
            Id = 3,
            Tag = "C001",
            Name = "Child",
            Sex = "F",
            BirthDate = DateTime.Now.AddYears(-1),
        };
        var mother = new Animal
        {
            Id = 2,
            Tag = "M001",
            Name = "Mother",
            Sex = "F",
            BirthDate = DateTime.Now.AddYears(-5),
            Children = new List<Animal> { child },
        };
        var father = new Animal
        {
            Id = 4,
            Tag = "F001",
            Name = "Father",
            Sex = "M",
            BirthDate = DateTime.Now.AddYears(-6),
        };
        var animal = new Animal
        {
            Id = animalId,
            Tag = "A001",
            Name = "Animal",
            Sex = "M",
            BirthDate = DateTime.Now.AddYears(-2),
            MotherId = mother.Id,
            FatherId = father.Id,
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalWithGenealogyAsync(animalId))
            .ReturnsAsync(animal);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(mother.Id)).ReturnsAsync(mother);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(father.Id)).ReturnsAsync(father);
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(animalId))
            .ReturnsAsync(new List<Animal>()); // This handler's BuildGenealogyAsync calls GetChildrenAsync

        // When BuildGenealogyAsync calls recursively
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(mother.Id))
            .ReturnsAsync(new List<Animal> { child });
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(father.Id))
            .ReturnsAsync(new List<Animal>());
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(child.Id))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.Mother.ShouldNotBeNull();
        result.Mother.Id.ShouldBe(mother.Id);
        result.Father.ShouldNotBeNull();
        result.Father.Id.ShouldBe(father.Id);
        result.Children.ShouldBeEmpty(); // This test only covers direct parent/child link, not recursive children
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        var animalId = 999;
        var query = new GetAnimalGenealogyQuery(animalId);

        _animalRepositoryMock
            .Setup(r => r.GetAnimalWithGenealogyAsync(animalId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
