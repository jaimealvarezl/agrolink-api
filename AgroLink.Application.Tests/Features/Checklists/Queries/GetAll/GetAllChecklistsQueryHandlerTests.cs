using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Checklists.Queries.GetAll;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetAll;

[TestFixture]
public class GetAllChecklistsQueryHandlerTests
{
    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<IRepository<ChecklistItem>> _checklistItemRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<AgroLink.Application.Interfaces.IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetAllChecklistsQueryHandler _handler = null!;

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
        _handler = new GetAllChecklistsQueryHandler(
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
    public async Task Handle_ReturnsAllChecklists()
    {
        // Arrange
        var query = new GetAllChecklistsQuery();
        var checklists = new List<Checklist>
        {
            new Checklist
            {
                Id = 1,
                ScopeType = "LOT",
                ScopeId = 1,
                Date = DateTime.Today,
                UserId = 1,
            },
            new Checklist
            {
                Id = 2,
                ScopeType = "LOT",
                ScopeId = 1,
                Date = DateTime.Today.AddDays(-1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot { Id = 1, Name = "Test Lot" };

        _checklistRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(checklists);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _checklistItemRepositoryMock
            .Setup(r =>
                r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<ChecklistItem, bool>>>()
                )
            )
            .ReturnsAsync(new List<ChecklistItem>());
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().ScopeName.ShouldBe(lot.Name);
        result.First().UserName.ShouldBe(user.Name);
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoChecklistsExist()
    {
        // Arrange
        var query = new GetAllChecklistsQuery();
        _checklistRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Checklist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
