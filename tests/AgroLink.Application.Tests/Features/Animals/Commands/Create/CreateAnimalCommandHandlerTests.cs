using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Create;

[TestFixture]
public class CreateAnimalCommandHandlerTests
{
    private AutoMocker _mocker = null!;
    private CreateAnimalCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateAnimalCommandHandler>();
    }

    [Test]
    public async Task Handle_ValidCreateAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        var farmId = 10;
        var userId = 5;
        var createAnimalDto = new CreateAnimalDto
        {
            Cuia = "A001",
            TagVisual = "V001",
            Name = "Test Animal",
            Color = "Brown",
            LotId = 1,
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Heifer,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.Open,
            Owners = new List<AnimalOwnerCreateDto>
            {
                new() { OwnerId = 1, SharePercent = 100 },
            },
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(lot.Id)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsCuiaUniqueInFarmAsync(createAnimalDto.Cuia, farmId, null))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsNameUniqueInFarmAsync(createAnimalDto.Name, farmId, null))
            .ReturnsAsync(true);

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Animal>()))
            .Callback<Animal>(a => a.Id = 1);
        _mocker.GetMock<IOwnerRepository>().Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Cuia.ShouldBe(createAnimalDto.Cuia);
        result.Color.ShouldBe(createAnimalDto.Color);
        result.LotName.ShouldBe(lot.Name);
        result.Owners.Count.ShouldBe(1);
        result.Owners[0].OwnerId.ShouldBe(1);
        result.Owners[0].OwnerName.ShouldBe(owner.Name);

        _mocker.GetMock<IAnimalRepository>().Verify(
            r => r.AddAsync(It.Is<Animal>(a => a.AnimalOwners.Count == 1)),
            Times.Once
        );
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NameAlreadyExistsInActiveAnimal_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 10;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Existing Active Name",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 100 }],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsNameUniqueInFarmAsync("Existing Active Name", farmId, null))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("already exists in this Farm");
    }

    [Test]
    public async Task Handle_NameUsedBySoldAnimal_AllowsCreation()
    {
        // Arrange
        var farmId = 10;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Old Name From Sold Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 100 }],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        // IsNameUniqueInFarmAsync will return true because the old animal is SOLD (not active/missing)
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsNameUniqueInFarmAsync("Old Name From Sold Animal", farmId, null))
            .ReturnsAsync(true);

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r =>
                r.IsCuiaUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(createAnimalDto.Name);
    }

    [Test]
    public async Task Handle_LotNotFound_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 999,
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(999)).ReturnsAsync((Lot?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Lot with ID 999 not found");
    }

    [Test]
    public async Task Handle_UserNotMemberOfFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_CuiaNotUniqueInFarm_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Cuia = "A001",
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsCuiaUniqueInFarmAsync("A001", 10, null))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("already exists in this Farm");
    }

    [Test]
    public async Task Handle_InconsistentStatus_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Male,
            ProductionStatus = ProductionStatus.Bull,
            ReproductiveStatus = ReproductiveStatus.Pregnant, // Inconsistent
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            HealthStatus = HealthStatus.Healthy,
            Owners = [],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("ReproductiveStatus set to NotApplicable");
    }

    [Test]
    public async Task Handle_EmptyOwnersList_AutoAssignsFarmOwner()
    {
        // Arrange
        var farmId = 10;
        var farmOwnerId = 99;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [], // Empty list
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };
        var farm = new Farm { Id = farmId, OwnerId = farmOwnerId };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r =>
                r.IsCuiaUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        _mocker.GetMock<IAnimalRepository>().Verify(
            r =>
                r.AddAsync(
                    It.Is<Animal>(a =>
                        a.AnimalOwners.Any(ao =>
                            ao.OwnerId == farmOwnerId && ao.SharePercent == 100
                        )
                    )
                ),
            Times.Once
        );
        _mocker.GetMock<IFarmRepository>().Verify(r => r.GetByIdAsync(farmId), Times.Once);
    }

    [Test]
    public async Task Handle_EmptyOwnersList_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 10;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            TagVisual = "V001",
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [], // Empty list
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r =>
                r.IsNameUniqueInFarmAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>())
            )
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain($"Farm with ID {farmId} not found");
    }

    [Test]
    public async Task Handle_FutureBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Name = "Future Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddDays(1),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Birth date cannot be in the future");
    }

    [Test]
    public async Task Handle_InconsistentOwnersPercentage_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 10;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 50 }], // Sum is 50
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(5);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsNameUniqueInFarmAsync(It.IsAny<string>(), farmId, null))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.IsCuiaUniqueInFarmAsync(It.IsAny<string>(), farmId, null))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Total ownership percentage must be 100%");
    }

    [Test]
    public async Task Handle_MotherIsMale_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 10;
        var userId = 5;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            MotherId = 2,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 100 }],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };
        var mother = new Animal
        {
            Id = 2,
            Sex = Sex.Female,
            Lot = lot,
        };
        var maleMother = new Animal
        {
            Id = 2,
            Sex = Sex.Male, // Wrong sex
            Lot = lot,
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>().Setup(r => r.GetByIdAsync(2, userId)).ReturnsAsync(maleMother);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Mother must be Female");
    }

    [Test]
    public async Task Handle_ParentFromDifferentFarm_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 10;
        var userId = 5;
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            FatherId = 3,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 100 }],
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };
        var father = new Animal
        {
            Id = 3,
            Sex = Sex.Male,
            Lot = new Lot
            {
                Paddock = new Paddock { FarmId = 20 }, // Different farm
            },
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker.GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _mocker.GetMock<IAnimalRepository>().Setup(r => r.GetByIdAsync(3, userId)).ReturnsAsync(father);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("belongs to a different farm");
    }
}
