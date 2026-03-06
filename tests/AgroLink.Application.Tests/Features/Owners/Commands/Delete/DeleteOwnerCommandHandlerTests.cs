using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Owners.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Owners.Commands.Delete;

[TestFixture]
public class DeleteOwnerCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteOwnerCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteOwnerCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCommand_SoftDeletesOwner()
    {
        // Arrange
        var command = new DeleteOwnerCommand(1, 10);
        var farm = new Farm { Id = 1, OwnerId = 99 }; // Different owner ID
        var owner = new Owner
        {
            Id = 10,
            FarmId = 1,
            IsActive = true,
        };

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(farm);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        owner.IsActive.ShouldBeFalse();
        _mocker.GetMock<IOwnerRepository>().Verify(r => r.Update(owner), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_IsMainOwner_ThrowsArgumentException()
    {
        // Arrange
        var command = new DeleteOwnerCommand(1, 10);
        var farm = new Farm { Id = 1, OwnerId = 10 }; // Same owner ID

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(farm);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldBe("Cannot delete the main owner of the farm.");
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new DeleteOwnerCommand(1, 10);
        var farm = new Farm { Id = 1, OwnerId = 99 };

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(farm);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync((Owner?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
