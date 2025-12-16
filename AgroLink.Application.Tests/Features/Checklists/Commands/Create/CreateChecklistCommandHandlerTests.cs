using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Commands.Create;

[TestFixture]
public class CreateChecklistCommandHandlerTests
{
    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<IRepository<ChecklistItem>> _checklistItemRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<AgroLink.Application.Interfaces.IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateChecklistCommandHandler _handler = null!;

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
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateChecklistCommandHandler(
            _checklistRepositoryMock.Object,
            _checklistItemRepositoryMock.Object,
            _userRepositoryMock.Object,
            _animalRepositoryMock.Object,
            _photoRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidCreateChecklistCommand_ReturnsChecklistDto()
    {
        // Arrange
        var createChecklistDto = new CreateChecklistDto
        {
            ScopeType = "LOT",
            ScopeId = 1,
            Date = DateTime.Today,
            Notes = "Test Notes",
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
        var userId = 1;
        var command = new CreateChecklistCommand(createChecklistDto, userId);
        var checklist = new Checklist
        {
            Id = 1,
            ScopeType = "LOT",
            ScopeId = 1,
            UserId = userId,
            Date = DateTime.Today,
        };
        var user = new User { Id = userId, Name = "Test User" };
        var lot = new Lot { Id = 1, Name = "Test Lot" };
        var animal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Animal 1",
        };

        _checklistRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .Callback<Checklist>(c => c.Id = checklist.Id);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _checklistItemRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChecklistItem>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _checklistItemRepositoryMock
            .Setup(r =>
                r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<ChecklistItem, bool>>>()
                )
            )
            .ReturnsAsync(
                new List<ChecklistItem>
                {
                    new ChecklistItem { ChecklistId = checklist.Id, AnimalId = animal.Id },
                }
            );
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animal.Id)).ReturnsAsync(animal);
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklist.Id);
        result.ScopeName.ShouldBe(lot.Name);
        result.Items.Count.ShouldBe(1);
        _checklistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Once);
        _checklistItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ChecklistItem>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }
}