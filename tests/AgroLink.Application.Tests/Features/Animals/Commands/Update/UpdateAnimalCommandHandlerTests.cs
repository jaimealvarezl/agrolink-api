using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Update;

[TestFixture]
public class UpdateAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _storageServiceMock = new Mock<IStorageService>();
        _handler = new UpdateAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _storageServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private UpdateAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        const int animalId = 1;
        const int farmId = 10;
        const int userId = 5;
        var updateAnimalDto = new UpdateAnimalDto
        {
            Name = "Updated Name",
            Color = "Black",
            LifeStatus = LifeStatus.Sold,
            Owners = new List<AnimalOwnerCreateDto>
            {
                new() { OwnerId = 1, SharePercent = 100 },
            },
        };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Old Name",
            LotId = 1,
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Milking,
            ReproductiveStatus = ReproductiveStatus.Open,
            CreatedAt = DateTime.UtcNow,
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _ownerRepositoryMock.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _animalOwnerRepositoryMock
            .Setup(r => r.RemoveByAnimalIdAsync(animalId))
            .Returns(Task.CompletedTask);
        _animalOwnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AnimalOwner>()))
            .Returns(Task.CompletedTask);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(
                new List<AnimalOwner>
                {
                    new()
                    {
                        AnimalId = 1,
                        OwnerId = 1,
                        SharePercent = 100,
                    },
                }
            );
        _animalPhotoRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalPhoto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.Name.ShouldBe(updateAnimalDto.Name);
        result.Color.ShouldBe(updateAnimalDto.Color);
        result.LifeStatus.ShouldBe(updateAnimalDto.LifeStatus!.Value);
        result.Owners.Count.ShouldBe(1);
        result.Owners[0].OwnerId.ShouldBe(1);
        result.Owners[0].OwnerName.ShouldBe(owner.Name);

        _animalRepositoryMock.Verify(r => r.Update(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _animalOwnerRepositoryMock.Verify(r => r.RemoveByAnimalIdAsync(animalId), Times.Once);
        _animalOwnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AnimalOwner>()), Times.Once);
    }

    [Test]
    public async Task Handle_UpdateToExistingActiveName_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        const int farmId = 10;
        const int userId = 5;
        var updateAnimalDto = new UpdateAnimalDto { Name = "Existing Name" };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            Name = "Old Name",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);

        _animalRepositoryMock
            .Setup(r => r.IsNameUniqueInFarmAsync("Existing Name", farmId, animalId))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("already exists in this Farm");
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 5;
        var updateAnimalDto = new UpdateAnimalDto { Name = "Updated Name" };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);

        _animalRepositoryMock
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found or access denied.");
    }

    [Test]
    public async Task Handle_EmptyOwnersProvided_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 5;
        const int farmId = 10;
        var updateAnimalDto = new UpdateAnimalDto { Owners = new List<AnimalOwnerCreateDto>() };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("At least one owner is required");
    }

    [Test]
    public async Task Handle_InconsistentOwnersSum_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 5;
        const int farmId = 10;
        var updateAnimalDto = new UpdateAnimalDto
        {
            Owners = new List<AnimalOwnerCreateDto>
            {
                new() { OwnerId = 1, SharePercent = 40 },
            },
        };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Total ownership percentage must be 100%");
    }

    [Test]
    public async Task Handle_UpdateMotherToMale_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 5;
        const int farmId = 10;
        var updateAnimalDto = new UpdateAnimalDto { MotherId = 2 };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        var mother = new Animal
        {
            Id = 2,
            Sex = Sex.Male, // Wrong sex
            Lot = animal.Lot,
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(2, userId)).ReturnsAsync(mother);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Mother must be Female");
    }
}
