using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using Shouldly;

namespace AgroLink.Tests.Repositories;

[TestFixture]
public class AnimalRepositoryTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
        _repository = new AnimalRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private AgroLinkDbContext _context = null!;
    private IAnimalRepository _repository = null!;

    [Test]
    public async Task GetByIdAsync_WhenAnimalExists_ShouldReturnAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        // Act
        var result = await _repository.GetByIdAsync(animal.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animal.Id);
        result.Tag.ShouldBe("A001");
        result.Name.ShouldBe("Test Animal");
    }

    [Test]
    public async Task GetByIdAsync_WhenAnimalDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllAnimals()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        await CreateTestAnimalAsync(_context, lot.Id);
        await CreateTestAnimalAsync(_context, lot.Id, "A002");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetByLotIdAsync_ShouldReturnAnimalsInLot()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot1 = await CreateTestLotAsync(_context, paddock.Id, "Lot 1");
        var lot2 = await CreateTestLotAsync(_context, paddock.Id, "Lot 2");

        await CreateTestAnimalAsync(_context, lot1.Id);
        await CreateTestAnimalAsync(_context, lot1.Id, "A002");
        await CreateTestAnimalAsync(_context, lot2.Id, "A003");

        // Act
        var result = await _repository.GetByLotIdAsync(lot1.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(a => a.LotId == lot1.Id).ShouldBeTrue();
    }

    [Test]
    public async Task GetByTagAsync_WhenTagExists_ShouldReturnAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        // Act
        var result = await _repository.GetByTagAsync("A001");

        // Assert
        result.ShouldNotBeNull();
        result.Tag.ShouldBe("A001");
        result.Id.ShouldBe(animal.Id);
    }

    [Test]
    public async Task GetByTagAsync_WhenTagDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByTagAsync("NONEXISTENT");

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task AddAsync_ShouldAddAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);

        var animal = new Animal
        {
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = lot.Id,
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        await _repository.AddAsync(animal);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(animal.Id);
        result.ShouldNotBeNull();
        result.Tag.ShouldBe("A001");
    }

    [Test]
    public async Task Update_ShouldUpdateAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        // Act
        animal.Name = "Updated Name";
        animal.UpdatedAt = DateTime.UtcNow;
        _repository.Update(animal);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(animal.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
    }

    [Test]
    public async Task Remove_ShouldRemoveAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        // Act
        _repository.Remove(animal);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(animal.Id);
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetChildrenAsync_ShouldReturnChildren()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);

        var mother = await CreateTestAnimalAsync(_context, lot.Id, "M001");
        var father = await CreateTestAnimalAsync(_context, lot.Id, "F001");

        var child1 = new Animal
        {
            Tag = "C001",
            Name = "Child 1",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddMonths(-6),
            LotId = lot.Id,
            MotherId = mother.Id,
            FatherId = father.Id,
            CreatedAt = DateTime.UtcNow,
        };

        var child2 = new Animal
        {
            Tag = "C002",
            Name = "Child 2",
            Color = "Black",
            Breed = "Holstein",
            Sex = "Male",
            BirthDate = DateTime.UtcNow.AddMonths(-3),
            LotId = lot.Id,
            MotherId = mother.Id,
            FatherId = father.Id,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Animals.AddRange(child1, child2);
        await _context.SaveChangesAsync();

        // Act
        var motherChildren = await _repository.GetChildrenAsync(mother.Id);
        var fatherChildren = await _repository.GetChildrenAsync(father.Id);

        // Assert
        motherChildren.Count().ShouldBe(2);
        fatherChildren.Count().ShouldBe(2);
        motherChildren.All(c => c.MotherId == mother.Id).ShouldBeTrue();
        fatherChildren.All(c => c.FatherId == father.Id).ShouldBeTrue();
    }
}
