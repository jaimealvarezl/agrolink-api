using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Create;

[TestFixture]
public class CreatePaddockCommandHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreatePaddockCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreatePaddockCommandHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidCreatePaddockCommand_ReturnsPaddockDto()
    {
        // Arrange
        var createPaddockDto = new CreatePaddockDto { Name = "Test Paddock", FarmId = 1 };
        var command = new CreatePaddockCommand(createPaddockDto);
        var paddock = new Paddock
        {
            Id = 1,
            Name = "Test Paddock",
            FarmId = 1,
        };
        var farm = new Farm { Id = 1, Name = "Test Farm" };

        _paddockRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Paddock>()))
            .Callback<Paddock>(p => p.Id = paddock.Id); // Simulate DB ID generation
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farm.Id)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddock.Id);
        result.Name.ShouldBe(paddock.Name);
        result.FarmName.ShouldBe(farm.Name);
        _paddockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Paddock>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
