using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Owners.Commands.Create;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Owners.Commands.Create;

[TestFixture]
public class CreateOwnerCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateOwnerCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateOwnerCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCommand_CreatesOwner()
    {
        // Arrange
        var command = new CreateOwnerCommand(1, "New Owner", "123", "test@test.com", 2);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetOwnerByNameAndFarmIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Owner?)null);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Owner>()))
            .Callback<Owner>(o => o.Id = 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(command.Name);
        result.Id.ShouldBe(10);
        result.IsActive.ShouldBeTrue();

        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_FarmDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateOwnerCommand(1, "New Owner", "123", "test@test.com", 2);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_ExistingActiveOwner_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateOwnerCommand(1, "Existing Owner", "123", "test@test.com", 2);
        var existingOwner = new Owner
        {
            Id = 1,
            Name = "Existing Owner",
            FarmId = 1,
            IsActive = true,
        };

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetOwnerByNameAndFarmIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(existingOwner);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_ExistingInactiveOwner_RestoresOwner()
    {
        // Arrange
        var command = new CreateOwnerCommand(1, "Deleted Owner", "999", "new@test.com", 2);
        var existingOwner = new Owner
        {
            Id = 1,
            Name = "Deleted Owner",
            FarmId = 1,
            IsActive = false,
            Phone = "111",
            UserId = 3,
        };

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetOwnerByNameAndFarmIncludingDeletedAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(existingOwner);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsActive.ShouldBeTrue();
        result.Phone.ShouldBe("999");
        result.UserId.ShouldBe(2);

        _mocker.GetMock<IOwnerRepository>().Verify(r => r.Update(existingOwner), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
