using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Create;

[TestFixture]
public class CreateChecklistCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateChecklistCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateChecklistCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreateChecklistCommand_ReturnsChecklistDto()
    {
        // Arrange
        const int farmId = 10;
        var createChecklistDto = new CreateChecklistDto
        {
            LotId = 1,
            Date = DateTime.Today,
            Notes = "Test Notes",
            Items =
            [
                new CreateChecklistItemDto
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
            ],
        };
        const int userId = 1;
        var command = new CreateChecklistCommand(createChecklistDto, userId);
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var user = new User { Id = userId, Name = "Test User" };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
        };
        var animalLot = new Lot { Id = 1, Name = "Test Lot" };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal> { animal });
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot> { animalLot });
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .Callback<Checklist, CancellationToken>((c, _) => c.Id = 1);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.LotName.ShouldBe(lot.Name);
        result.Items.Count.ShouldBe(1);
        result.Items.First().AnimalLotId.ShouldBe(1);
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NoFarmContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new CreateChecklistDto
        {
            LotId = 1,
            Items =
            [
                new CreateChecklistItemDto
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
            ],
        };
        var command = new CreateChecklistCommand(dto, 1);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns((int?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_LotNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateChecklistDto
        {
            LotId = 999,
            Items =
            [
                new CreateChecklistItemDto
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
            ],
        };
        var command = new CreateChecklistCommand(dto, 1);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(999))
            .ReturnsAsync((Lot?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_LotFromDifferentFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        var dto = new CreateChecklistDto
        {
            LotId = 1,
            Items =
            [
                new CreateChecklistItemDto
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
            ],
        };
        var command = new CreateChecklistCommand(dto, 1);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 20 },
        };
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_EmptyItems_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateChecklistDto { LotId = 1, Items = [] };
        var command = new CreateChecklistCommand(dto, 1);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_MissingAnimalIds_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateChecklistDto
        {
            LotId = 1,
            Items =
            [
                new CreateChecklistItemDto
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
                new CreateChecklistItemDto
                {
                    AnimalId = 999,
                    Present = true,
                    Condition = "OK",
                },
            ],
        };
        var command = new CreateChecklistCommand(dto, 1);
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };
        var animal = new Animal { Id = 1, LotId = 1 };
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal> { animal });

        // Act & Assert
        var ex = await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("999");
    }
}
