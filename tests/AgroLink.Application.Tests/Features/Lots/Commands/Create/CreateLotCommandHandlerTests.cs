using AgroLink.Application.Features.Lots.Commands.Create;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Create;

[TestFixture]
public class CreateLotCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateLotCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateLotCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreateLotCommand_ReturnsLotDto()
    {
        // Arrange
        var createLotDto = new CreateLotDto
        {
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
        };
        var command = new CreateLotCommand(createLotDto);
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
        };
        var paddock = new Paddock { Id = 1, Name = "Test Paddock" };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Lot>()))
            .Callback<Lot>(l => l.Id = lot.Id); // Simulate DB ID generation
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddock.Id))
            .ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lot.Id);
        result.Name.ShouldBe(lot.Name);
        result.PaddockName.ShouldBe(paddock.Name);
        _mocker.GetMock<ILotRepository>().Verify(r => r.AddAsync(It.IsAny<Lot>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_InvalidPaddockId_ThrowsArgumentException()
    {
        // Arrange
        var createLotDto = new CreateLotDto
        {
            Name = "Test Lot",
            PaddockId = 999,
            Status = "ACTIVE",
        };
        var command = new CreateLotCommand(createLotDto);

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(createLotDto.PaddockId))
            .ReturnsAsync((Paddock?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
