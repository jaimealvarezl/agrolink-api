using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Repositories;

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
        result.TagVisual.ShouldBe("A001");
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
    public async Task GetByCuiaAsync_WhenCuiaExists_ShouldReturnAnimal()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        // Act
        var result = await _repository.GetByCuiaAsync("CUIA-A001");

        // Assert
        result.ShouldNotBeNull();
        result.Cuia.ShouldBe("CUIA-A001");
        result.Id.ShouldBe(animal.Id);
    }

    [Test]
    public async Task GetByCuiaAsync_WhenCuiaDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByCuiaAsync("NONEXISTENT");

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
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = lot.Id,
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        await _repository.AddAsync(animal);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(animal.Id);
        result.ShouldNotBeNull();
        result.TagVisual.ShouldBe("A001");
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
        await _context.SaveChangesAsync();

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
        await _context.SaveChangesAsync();

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
            TagVisual = "C001",
            Cuia = "CUIA-C001",
            Name = "Child 1",
            Color = "Brown",
            Breed = "Holstein",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddMonths(-6),
            LotId = lot.Id,
            MotherId = mother.Id,
            FatherId = father.Id,
            CreatedAt = DateTime.UtcNow,
        };

        var child2 = new Animal
        {
            TagVisual = "C002",
            Cuia = "CUIA-C002",
            Name = "Child 2",
            Color = "Black",
            Breed = "Holstein",
            Sex = Sex.Male,
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

    [Test]
    public async Task GetPagedListAsync_ShouldFilterAndPaginate()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);

        // Animal 1: Sick, Pregnant
        var a1 = await CreateTestAnimalAsync(_context, lot.Id, "A1");
        a1.HealthStatus = HealthStatus.Sick;
        a1.ReproductiveStatus = ReproductiveStatus.Pregnant;

        // Animal 2: Missing
        var a2 = await CreateTestAnimalAsync(_context, lot.Id, "A2");
        a2.LifeStatus = LifeStatus.Missing;

        // Animal 3: Healthy
        var a3 = await CreateTestAnimalAsync(_context, lot.Id, "A3");

        await _context.SaveChangesAsync();

        // Act - Filter Sick
        var sickResult = await _repository.GetPagedListAsync(farm.Id, 1, 10, isSick: true);
        sickResult.Items.Count().ShouldBe(1);
        sickResult.Items.First().TagVisual.ShouldBe("A1");

        // Act - Filter Missing
        var missingResult = await _repository.GetPagedListAsync(farm.Id, 1, 10, isMissing: true);
        missingResult.Items.Count().ShouldBe(1);
        missingResult.Items.First().TagVisual.ShouldBe("A2");

        // Act - Search
        var searchResult = await _repository.GetPagedListAsync(farm.Id, 1, 10, searchTerm: "A3");
        searchResult.Items.Count().ShouldBe(1);
        searchResult.Items.First().TagVisual.ShouldBe("A3");

        // Act - Case Insensitive Search
        var caseInsensitiveResult = await _repository.GetPagedListAsync(
            farm.Id,
            1,
            10,
            searchTerm: "a3"
        );
        caseInsensitiveResult.Items.Count().ShouldBe(1);
        caseInsensitiveResult.Items.First().TagVisual.ShouldBe("A3");

        // Act - Filter Sex
        var sexResult = await _repository.GetPagedListAsync(farm.Id, 1, 10, sex: Sex.Female);
        sexResult.Items.Count().ShouldBe(3); // All test animals created by helper are Female by default
    }

    [Test]
    public async Task GetAnimalDetailsAsync_ShouldReturnDetailsWithIncludes()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);

        var mother = await CreateTestAnimalAsync(_context, lot.Id, "M1");
        var father = await CreateTestAnimalAsync(_context, lot.Id, "F1");
        var child = await CreateTestAnimalAsync(_context, lot.Id, "C1");

        child.MotherId = mother.Id;
        child.FatherId = father.Id;

        var owner = await CreateTestOwnerAsync(_context, "Owner1");
        child.AnimalOwners.Add(new AnimalOwner { OwnerId = owner.Id, SharePercent = 50 });

        child.Photos.Add(
            new AnimalPhoto
            {
                UriRemote = "http://photo.com",
                ContentType = "image/jpeg",
                Size = 1024,
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAnimalDetailsAsync(child.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Mother.ShouldNotBeNull();
        result.Mother.TagVisual.ShouldBe("M1");
        result.Father.ShouldNotBeNull();
        result.Father.TagVisual.ShouldBe("F1");
        result.AnimalOwners.Count.ShouldBe(1);
        result.Photos.Count.ShouldBe(1);
        result.Lot.ShouldNotBeNull();
    }

    [Test]
    public async Task GetDistinctColorsAsync_ShouldReturnDistinctFilteredColors()
    {
        // Arrange
        var user = await CreateTestUserAsync(_context);
        var farm = await CreateTestFarmAsync(_context);
        await AddUserToFarmAsync(_context, user.Id, farm.Id);

        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);

        var a1 = await CreateTestAnimalAsync(_context, lot.Id, "A1");
        a1.Color = "Negro";
        var a2 = await CreateTestAnimalAsync(_context, lot.Id, "A2");
        a2.Color = "negro"; // Same color, different casing
        var a3 = await CreateTestAnimalAsync(_context, lot.Id, "A3");
        a3.Color = "Blanco";
        var a4 = await CreateTestAnimalAsync(_context, lot.Id, "A4");
        a4.Color = "Pinto Negro";
        var a5 = await CreateTestAnimalAsync(_context, lot.Id, "A5");
        a5.Color = null;

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDistinctColorsAsync(user.Id);

        // Assert
        result.Count.ShouldBe(3); // Case-insensitive distinct: "Negro", "Blanco", "Pinto Negro"
        result.Any(c => c.ToLower() == "negro").ShouldBeTrue();
        result.ShouldContain("Blanco");
        result.ShouldContain("Pinto Negro");
    }

    [Test]
    public async Task GetDistinctColorsAsync_ShouldOnlyReturnColorsFromAccessibleFarms()
    {
        // Arrange
        var user = await CreateTestUserAsync(_context, "user1@test.com");
        var otherUser = await CreateTestUserAsync(_context, "user2@test.com");

        var farm1 = await CreateTestFarmAsync(_context, "Farm 1");
        await AddUserToFarmAsync(_context, user.Id, farm1.Id);

        var farm2 = await CreateTestFarmAsync(_context, "Farm 2");
        await AddUserToFarmAsync(_context, otherUser.Id, farm2.Id);

        var p1 = await CreateTestPaddockAsync(_context, farm1.Id);
        var l1 = await CreateTestLotAsync(_context, p1.Id);
        var a1 = await CreateTestAnimalAsync(_context, l1.Id, "A1");
        a1.Color = "Negro";

        var p2 = await CreateTestPaddockAsync(_context, farm2.Id);
        var l2 = await CreateTestLotAsync(_context, p2.Id);
        var a2 = await CreateTestAnimalAsync(_context, l2.Id, "A2");
        a2.Color = "Blanco";

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDistinctColorsAsync(user.Id);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain("Negro");
        result.ShouldNotContain("Blanco");
    }

    [Test]
    public async Task GetDistinctColorsAsync_ShouldReturnColors_WhenUserIsFarmOwnerViaOwnerEntity()
    {
        // Arrange
        var user = await CreateTestUserAsync(_context);
        var owner = await CreateTestOwnerAsync(_context, "Owner", user.Id);
        var farm = await CreateTestFarmAsync(_context, "Owned Farm");
        farm.OwnerId = owner.Id;
        await _context.SaveChangesAsync();

        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);
        animal.Color = "Rojo";
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDistinctColorsAsync(user.Id);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain("Rojo");
    }
}
