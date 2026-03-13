using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Checklists.Commands.Update;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Update;

[TestFixture]
public class UpdateChecklistCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateChecklistCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateChecklistCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateChecklistCommand_ReturnsChecklistDto()
    {
        // Arrange
        var checklistId = 1;
        var farmId = 10;
        var updateChecklistDto = new CreateChecklistDto
        {
            LotId = 1,
            Date = DateTime.Today,
            Notes = "Updated Notes",
            Items = new List<CreateChecklistItemDto>
            {
                new()
                {
                    AnimalId = 1,
                    Present = true,
                    Condition = "OK",
                },
            },
        };
        var command = new UpdateChecklistCommand(checklistId, updateChecklistDto);
        var checklist = new Checklist
        {
            Id = checklistId,
            LotId = 1,
            UserId = 1,
            Date = DateTime.Today,
            Notes = "Old Notes",
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
        };
        var existingItems = new List<ChecklistItem>
        {
            new()
            {
                Id = 1,
                ChecklistId = checklistId,
                AnimalId = 2,
            },
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(checklist);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lot.Id))
            .ReturnsAsync(lot);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal> { animal });
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot> { lot });
        _mocker.GetMock<IChecklistRepository>().Setup(r => r.Update(checklist));
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(existingItems);
        _mocker.GetMock<IRepository<ChecklistItem>>().Setup(r => r.RemoveRange(existingItems));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(checklist.UserId))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklistId);
        result.Notes.ShouldBe(updateChecklistDto.Notes);
        result.Items.Count.ShouldBe(1);
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.GetByIdAsync(checklistId), Times.Once);
        _mocker.GetMock<IChecklistRepository>().Verify(r => r.Update(checklist), Times.Once);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Verify(r => r.RemoveRange(existingItems), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ThrowsNotFoundException()
    {
        // Arrange
        var checklistId = 999;
        var updateChecklistDto = new CreateChecklistDto { LotId = 1, Notes = "Updated Notes" };
        var command = new UpdateChecklistCommand(checklistId, updateChecklistDto);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.Update(It.IsAny<Checklist>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
