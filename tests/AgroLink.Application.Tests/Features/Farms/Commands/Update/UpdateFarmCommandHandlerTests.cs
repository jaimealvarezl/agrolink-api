using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Domain.Constants;
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
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateFarmCommandHandler(
            _farmRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdateFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateByOwner_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var name = "Updated Farm Name";
        var location = "Updated Location";
        var cue = "ABC12345";
        var command = new UpdateFarmCommand(farmId, name, location, cue, userId);

        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = 20,
        };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(name);
        result.Location.ShouldBe(location);
        result.CUE.ShouldBe(cue);
        result.Role.ShouldBe(FarmMemberRoles.Owner);

        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_ValidUpdateByAdmin_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Admin,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(FarmMemberRoles.Admin);
    }

    [Test]
    public async Task Handle_ForbiddenRole_ThrowsForbiddenAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Viewer,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NotMember_ThrowsForbiddenAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdateFarmCommand(999, "Name", null, null, 10);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
