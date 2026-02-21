using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Update;

[TestFixture]
public class UpdateFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateFarmCommandHandler(
            _farmRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdateFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateByOwner_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var ownerId = 20;
        var name = "Updated Farm Name";
        var location = "Updated Location";
        var cue = "ABC12345";
        var command = new UpdateFarmCommand(farmId, name, location, cue);

        var owner = new Owner { Id = ownerId, UserId = userId };
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = ownerId,
        };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(name);
        result.Location.ShouldBe(location);
        result.CUE.ShouldBe(cue);
        result.Role.ShouldBe("Owner");

        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NotOwner_ThrowsForbiddenAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var ownerId = 20;
        var otherOwnerId = 30;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null);

        var owner = new Owner { Id = ownerId, UserId = userId };
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = otherOwnerId,
        };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var userId = 10;
        var owner = new Owner { Id = 20, UserId = userId };
        var command = new UpdateFarmCommand(999, "Name", null, null);

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidName_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var ownerId = 20;
        var owner = new Owner { Id = ownerId, UserId = userId };
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = ownerId,
        };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act & Assert (Empty Name)
        var commandEmpty = new UpdateFarmCommand(farmId, "", null, null);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(commandEmpty, CancellationToken.None)
        );

        // Act & Assert (Too Long Name)
        var commandLong = new UpdateFarmCommand(farmId, new string('A', 101), null, null);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(commandLong, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidCUE_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var ownerId = 20;
        var owner = new Owner { Id = ownerId, UserId = userId };
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = ownerId,
        };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act & Assert (Special characters in CUE)
        var commandInvalidCue = new UpdateFarmCommand(farmId, "Valid Name", null, "ABC-123");
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(commandInvalidCue, CancellationToken.None)
        );
    }
}
