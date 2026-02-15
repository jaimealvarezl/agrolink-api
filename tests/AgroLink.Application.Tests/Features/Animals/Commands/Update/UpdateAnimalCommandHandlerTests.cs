using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
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
        _currentUserServiceMock = new Mock<ICurrentUserService>();
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
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
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
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);
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
        };
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
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
        var updateAnimalDto = new UpdateAnimalDto { Name = "Existing Name" };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            Name = "Old Name",
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

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
        var updateAnimalDto = new UpdateAnimalDto { Name = "Updated Name" };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found");
    }

    [Test]
    public async Task Handle_NoPermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int animalId = 1;
        var command = new UpdateAnimalCommand(animalId, new UpdateAnimalDto());
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_EmptyOwnersProvided_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 1;
        var updateAnimalDto = new UpdateAnimalDto { Owners = new List<AnimalOwnerCreateDto>() };
        var command = new UpdateAnimalCommand(animalId, updateAnimalDto);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
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
}
