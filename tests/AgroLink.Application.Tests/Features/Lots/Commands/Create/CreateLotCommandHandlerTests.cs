using AgroLink.Application.Features.Lots.Commands.Create;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Create;

[TestFixture]
public class CreateLotCommandHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateLotCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateLotCommandHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

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

        _lotRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Lot>()))
            .Callback<Lot>(l => l.Id = lot.Id); // Simulate DB ID generation
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddock.Id)).ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lot.Id);
        result.Name.ShouldBe(lot.Name);
        result.PaddockName.ShouldBe(paddock.Name);
        _lotRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Lot>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
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

        _paddockRepositoryMock
            .Setup(r => r.GetByIdAsync(createLotDto.PaddockId))
            .ReturnsAsync((Paddock?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
