using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Checklists.Commands.Update;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Update;

[TestFixture]
public class UpdateChecklistCommandHandlerTests
{
    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<IRepository<ChecklistItem>> _checklistItemRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<AgroLink.Application.Interfaces.IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private UpdateChecklistCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _checklistRepositoryMock = new Mock<IChecklistRepository>();
        _checklistItemRepositoryMock = new Mock<IRepository<ChecklistItem>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _photoRepositoryMock = new Mock<AgroLink.Application.Interfaces.IPhotoRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new UpdateChecklistCommandHandler(
            _checklistRepositoryMock.Object,
            _checklistItemRepositoryMock.Object,
            _userRepositoryMock.Object,
            _animalRepositoryMock.Object,
            _photoRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidUpdateChecklistCommand_ReturnsChecklistDto()
    {
        // Arrange
        var checklistId = 1;
        var updateChecklistDto = new CreateChecklistDto
        {
            ScopeType = "LOT",
            ScopeId = 1,
            Date = DateTime.Today,
            Notes = "Updated Notes",
            Items = new List<CreateChecklistItemDto>
            {
                new CreateChecklistItemDto
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
            ScopeType = "LOT",
            ScopeId = 1,
            UserId = 1,
            Date = DateTime.Today,
            Notes = "Old Notes",
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot { Id = 1, Name = "Test Lot" };
        var animal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Animal 1",
        };
        var existingItems = new List<ChecklistItem>
        {
            new ChecklistItem
            {
                Id = 1,
                ChecklistId = checklistId,
                AnimalId = 2,
            },
        };

        _checklistRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _checklistRepositoryMock.Setup(r => r.Update(checklist));
        _checklistItemRepositoryMock
            .Setup(r =>
                r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<ChecklistItem, bool>>>()
                )
            )
            .ReturnsAsync(existingItems);
        _checklistItemRepositoryMock.Setup(r => r.RemoveRange(existingItems));
        _checklistItemRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChecklistItem>()))
            .Returns(Task.CompletedTask);
        _checklistItemRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(checklist.UserId)).ReturnsAsync(user);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animal.Id)).ReturnsAsync(animal);
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklistId);
        result.Notes.ShouldBe(updateChecklistDto.Notes);
        result.Items.Count.ShouldBe(1);
        _checklistRepositoryMock.Verify(r => r.GetByIdAsync(checklistId), Times.Once);
        _checklistRepositoryMock.Verify(r => r.Update(checklist), Times.Once);
        _checklistItemRepositoryMock.Verify(r => r.RemoveRange(existingItems), Times.Once);
        _checklistItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ChecklistItem>()), Times.Once);
        _checklistRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ThrowsArgumentException()
    {
        // Arrange
        var checklistId = 999;
        var updateChecklistDto = new CreateChecklistDto { Notes = "Updated Notes" };
        var command = new UpdateChecklistCommand(checklistId, updateChecklistDto);

        _checklistRepositoryMock
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Checklist not found");
        _checklistRepositoryMock.Verify(r => r.Update(It.IsAny<Checklist>()), Times.Never);
        _checklistRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
