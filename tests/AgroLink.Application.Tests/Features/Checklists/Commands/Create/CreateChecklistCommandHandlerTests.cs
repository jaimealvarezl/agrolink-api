using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Features.Checklists.DTOs;
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
        var createChecklistDto = new CreateChecklistDto
        {
            ScopeType = "LOT",
            ScopeId = 1,
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
            TagVisual = "V001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Checklist>()))
            .Callback<Checklist>(c => c.Id = checklist.Id);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.AddAsync(It.IsAny<ChecklistItem>()))
            .Returns(Task.CompletedTask);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(
                new List<ChecklistItem>
                {
                    new() { ChecklistId = checklist.Id, AnimalId = animal.Id },
                }
            );
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(animal.Id))
            .ReturnsAsync(animal);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklist.Id);
        result.ScopeName.ShouldBe(lot.Name);
        result.Items.Count.ShouldBe(1);
        _mocker
            .GetMock<IChecklistRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Checklist>()), Times.Once);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Verify(r => r.AddAsync(It.IsAny<ChecklistItem>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }
}
