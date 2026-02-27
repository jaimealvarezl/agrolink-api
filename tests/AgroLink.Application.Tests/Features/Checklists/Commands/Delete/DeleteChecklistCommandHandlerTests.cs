using AgroLink.Application.Features.Checklists.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Delete;

[TestFixture]
public class DeleteChecklistCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteChecklistCommandHandler>();
    }

    private AutoMocker _mocker = null!;
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

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(checklist);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker.GetMock<IChecklistRepository>().Setup(r => r.Remove(checklist));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.GetByIdAsync(checklistId), Times.Once);
        _mocker.GetMock<IChecklistRepository>().Verify(r => r.Remove(checklist), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ThrowsArgumentException()
    {
        // Arrange
        var checklistId = 999;
        var command = new DeleteChecklistCommand(checklistId);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Checklist not found");
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.Remove(It.IsAny<Checklist>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
