using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using Shouldly;

namespace AgroLink.Tests.Repositories;

[TestFixture]
public class FarmRepositoryTests : TestBase
{
    private AgroLinkDbContext _context = null!;
    private IFarmRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
        _repository = new FarmRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_WhenFarmExists_ShouldReturnFarm()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context, "Test Farm");

        // Act
        var result = await _repository.GetByIdAsync(farm.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farm.Id);
        result.Name.ShouldBe("Test Farm");
    }

    [Test]
    public async Task GetByIdAsync_WhenFarmDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllFarms()
    {
        // Arrange
        await CreateTestFarmAsync(_context, "Farm 1");
        await CreateTestFarmAsync(_context, "Farm 2");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetFarmsWithPaddocksAsync_ShouldReturnFarmsWithPaddocks()
    {
        // Arrange
        var farm1 = await CreateTestFarmAsync(_context, "Farm 1");
        var farm2 = await CreateTestFarmAsync(_context, "Farm 2");

        await CreateTestPaddockAsync(_context, farm1.Id, "Paddock 1");
        await CreateTestPaddockAsync(_context, farm1.Id, "Paddock 2");
        await CreateTestPaddockAsync(_context, farm2.Id, "Paddock 3");

        // Act
        var result = await _repository.GetFarmsWithPaddocksAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);

        var farm1Result = result.First(f => f.Id == farm1.Id);
        farm1Result.Paddocks.Count.ShouldBe(2);

        var farm2Result = result.First(f => f.Id == farm2.Id);
        farm2Result.Paddocks.Count.ShouldBe(1);
    }

    [Test]
    public async Task GetFarmWithPaddocksAsync_WhenFarmExists_ShouldReturnFarmWithPaddocks()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context, "Test Farm");
        await CreateTestPaddockAsync(_context, farm.Id, "Paddock 1");
        await CreateTestPaddockAsync(_context, farm.Id, "Paddock 2");

        // Act
        var result = await _repository.GetFarmWithPaddocksAsync(farm.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farm.Id);
        result.Name.ShouldBe("Test Farm");
        result.Paddocks.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetFarmWithPaddocksAsync_WhenFarmDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetFarmWithPaddocksAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task AddAsync_ShouldAddFarm()
    {
        // Arrange
        var farm = new Farm
        {
            Name = "New Farm",
            Location = "New Location",
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        await _repository.AddAsync(farm);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(farm.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("New Farm");
    }

    [Test]
    public async Task Update_ShouldUpdateFarm()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context, "Original Name");

        // Act
        farm.Name = "Updated Name";
        farm.UpdatedAt = DateTime.UtcNow;
        _repository.Update(farm);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(farm.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
    }

    [Test]
    public async Task Remove_ShouldRemoveFarm()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context, "Test Farm");

        // Act
        _repository.Remove(farm);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(farm.Id);
        result.ShouldBeNull();
    }

    [Test]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await CreateTestFarmAsync(_context, "Farm 1");
        await CreateTestFarmAsync(_context, "Farm 2");

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.ShouldBe(2);
    }

    [Test]
    public async Task ExistsAsync_WhenFarmExists_ShouldReturnTrue()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context, "Test Farm");

        // Act
        var exists = await _repository.ExistsAsync(f => f.Id == farm.Id);

        // Assert
        exists.ShouldBeTrue();
    }

    [Test]
    public async Task ExistsAsync_WhenFarmDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(f => f.Id == 999);

        // Assert
        exists.ShouldBeFalse();
    }
}
