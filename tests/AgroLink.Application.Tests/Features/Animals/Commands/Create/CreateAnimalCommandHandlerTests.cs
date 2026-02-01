using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Create;

[TestFixture]
public class CreateAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateAnimalCommandHandler _handler = null!;

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
            LotId = 1,
            Sex = "FEMALE",
            LifeStatus = "Active",
            ProductionStatus = "Heifer",
            ReproductiveStatus = "Open",
            Owners = new List<AnimalOwnerDto>
            {
                new()
                {
                    OwnerId = 1,
                    OwnerName = "Test Owner",
                    SharePercent = 100,
                },
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

        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(lot.Id)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalRepositoryMock
            .Setup(r => r.IsCuiaUniqueInFarmAsync(createAnimalDto.Cuia, farmId, null))
            .ReturnsAsync(true);

        _animalRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Animal>()))
            .Callback<Animal>(a => a.Id = 1);
        _ownerRepositoryMock.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _animalOwnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AnimalOwner>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Cuia.ShouldBe(createAnimalDto.Cuia);
        result.LotName.ShouldBe(lot.Name);
        _animalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Animal>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Test]
    public async Task Handle_LotNotFound_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 999,
            TagVisual = "V001",
            Sex = "FEMALE",
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(999)).ReturnsAsync((Lot?)null);

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
            Sex = "FEMALE",
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

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
    public async Task Handle_CuiaNotUniqueInFarm_ThrowsArgumentException()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            LotId = 1,
            Cuia = "A001",
            TagVisual = "V001",
            Sex = "FEMALE",
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalRepositoryMock
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
            Sex = "MALE",
            ProductionStatus = "Bull",
            ReproductiveStatus = "Pregnant", // Inconsistent
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("ReproductiveStatus set to NotApplicable");
    }
}
