using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Create;

[TestFixture]
public class CreateFarmCommandHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateFarmCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateFarmCommandHandler(
            _farmRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidCreateFarmCommand_ReturnsFarmDto()
    {
        // Arrange
        var userId = 10;
        var createFarmDto = new CreateFarmDto { Name = "Test Farm", Location = "Test Location" };
        var command = new CreateFarmCommand(createFarmDto, userId);

        var user = new User { Id = userId, Name = "Test User" };
        var owner = new Owner { Id = 5, Name = "Test User" };
        var farm = new Farm
        {
            Id = 1,
            Name = "Test Farm",
            Location = "Test Location",
            OwnerId = 5,
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync((Owner?)null);

        _ownerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Owner>()))
            .Callback<Owner>(o => o.Id = owner.Id);

        _farmRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Farm>()))
            .Callback<Farm>(f =>
            {
                f.Id = farm.Id;
                // Simulate EF Core Foreign Key Fixup
                if (f.Owner != null)
                {
                    f.OwnerId = f.Owner.Id;
                }
            });

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farm.Id);
        result.Name.ShouldBe(farm.Name);
        result.OwnerId.ShouldBe(owner.Id);
        result.Role.ShouldBe(FarmMemberRoles.Owner);

        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _ownerRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Owner>(o => o.Name == user.Name)),
            Times.Once
        );
        _farmRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Farm>()), Times.Once);
        // Verify via Navigation Property as FarmId might be 0 before SaveChanges in test POCO
        _farmMemberRepositoryMock.Verify(
            r =>
                r.AddAsync(
                    It.Is<FarmMember>(m =>
                        m.Farm.Id == farm.Id
                        && m.UserId == userId
                        && m.Role == FarmMemberRoles.Owner
                    )
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_ExistingOwner_UsesExistingOwnerInsteadOfCreatingNewOne()
    {
        // Arrange
        var userId = 10;
        var createFarmDto = new CreateFarmDto { Name = "Second Farm", Location = "Location" };
        var command = new CreateFarmCommand(createFarmDto, userId);

        var user = new User { Id = userId, Name = "Test User" };
        var existingOwner = new Owner
        {
            Id = 5,
            Name = "Test User",
            UserId = userId,
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Setup FirstOrDefaultAsync to return existing owner
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(existingOwner);

        _farmRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Farm>()))
            .Callback<Farm>(f =>
            {
                f.Id = 2;
                if (f.Owner != null)
                    f.OwnerId = f.Owner.Id;
            });

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.OwnerId.ShouldBe(existingOwner.Id);

        // Verify Owner was NOT added again
        _ownerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Owner>()), Times.Never);
        _farmRepositoryMock.Verify(
            r =>
                r.AddAsync(
                    It.Is<Farm>(f => f.Owner == existingOwner || f.OwnerId == existingOwner.Id)
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 10;
        var command = new CreateFarmCommand(new CreateFarmDto { Name = "Test" }, userId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
