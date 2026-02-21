using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Delete;

[TestFixture]
public class DeleteFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteFarmCommandHandler(
            _farmRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeleteFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingFarmByOwner_SoftDeletesFarm()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = true };
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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        farm.IsActive.ShouldBeFalse();
        farm.DeletedAt.ShouldNotBeNull();
        _farmRepositoryMock.Verify(r => r.Update(farm), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NotOwner_ThrowsForbiddenAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = true };
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

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsSuccessfullyForIdempotency()
    {
        // Arrange
        var farmId = 999;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_AlreadyDeletedFarm_ReturnsSuccessfullyForIdempotency()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = false };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
