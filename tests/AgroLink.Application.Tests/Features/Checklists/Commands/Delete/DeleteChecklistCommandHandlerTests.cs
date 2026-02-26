using AgroLink.Application.Features.Checklists.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Delete;

[TestFixture]
public class DeleteChecklistCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _checklistRepositoryMock = new Mock<IChecklistRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteChecklistCommandHandler(
            _checklistRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeleteChecklistCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingChecklist_DeletesChecklist()
    {
        // Arrange
        var checklistId = 1;
        var farmId = 10;
        var command = new DeleteChecklistCommand(checklistId);
        var checklist = new Checklist
        {
            Id = checklistId,
            ScopeType = "LOT",
            ScopeId = 1,
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = farmId },
        };

        _checklistRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);
        _checklistRepositoryMock.Setup(r => r.Remove(checklist));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _checklistRepositoryMock.Verify(r => r.GetByIdAsync(checklistId), Times.Once);
        _checklistRepositoryMock.Verify(r => r.Remove(checklist), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ThrowsArgumentException()
    {
        // Arrange
        var checklistId = 999;
        var command = new DeleteChecklistCommand(checklistId);

        _checklistRepositoryMock
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Checklist not found");
        _checklistRepositoryMock.Verify(r => r.Remove(It.IsAny<Checklist>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
