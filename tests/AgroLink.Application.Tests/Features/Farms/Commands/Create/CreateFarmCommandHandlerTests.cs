using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Create;

[TestFixture]
public class CreateFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateFarmCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreateFarmCommand_ReturnsFarmDto()
    {
        // Arrange
        var userId = 10;
        var name = "Test Farm";
        var location = "Test Location";
        var command = new CreateFarmCommand(name, location, null, userId);

        var user = new User { Id = userId, Name = "Test User" };
        var owner = new Owner { Id = 5, Name = "Test User" };
        var farm = new Farm
        {
            Id = 1,
            Name = "Test Farm",
            Location = "Test Location",
            OwnerId = 5,
        };

        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync((Owner?)null);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Owner>()))
            .Callback<Owner, CancellationToken>((o, _) => o.Id = owner.Id);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Farm>()))
            .Callback<Farm, CancellationToken>((f, _) =>
            {
                f.Id = farm.Id;
                // Simulate EF Core Foreign Key Fixup
                if (f.Owner != null)
                {
                    f.OwnerId = f.Owner.Id;
                }
            });

        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farm.Id);
        result.Name.ShouldBe(farm.Name);
        result.OwnerId.ShouldBe(owner.Id);
        result.Role.ShouldBe(FarmMemberRoles.Owner);

        _mocker.GetMock<IUserRepository>().Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mocker
            .GetMock<IOwnerRepository>()
            .Verify(r => r.AddAsync(It.Is<Owner>(o => o.Name == user.Name)), Times.Once);
        _mocker.GetMock<IFarmRepository>().Verify(r => r.AddAsync(It.IsAny<Farm>()), Times.Once);
        // Verify via IDs as Navigation Properties might not be initialized in test POCOs
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Verify(
                r =>
                    r.AddAsync(
                        It.Is<FarmMember>(m =>
                            m.UserId == userId && m.Role == FarmMemberRoles.Owner
                        )
                    ),
                Times.Once
            );
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Exactly(3));
    }

    [Test]
    public async Task Handle_ExistingOwner_CreatesNewOwnerForNewFarm()
    {
        // Arrange
        const int userId = 10;
        const string name = "Second Farm";
        var command = new CreateFarmCommand(name, "Location", null, userId);

        var user = new User { Id = userId, Name = "Test User" };
        var existingOwner = new Owner
        {
            Id = 5,
            Name = "Test User",
            UserId = userId,
            FarmId = 99, // different farm
        };

        var newOwnerId = 6;

        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Setup FirstOrDefaultAsync to return existing owner
        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(existingOwner);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Owner>()))
            .Callback<Owner, CancellationToken>((o, _) => o.Id = newOwnerId);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Farm>()))
            .Callback<Farm, CancellationToken>((f, _) =>
            {
                f.Id = 2;
                if (f.Owner != null)
                {
                    f.OwnerId = f.Owner.Id;
                }
            });

        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.OwnerId.ShouldBe(newOwnerId);

        // Verify Owner WAS added again
        _mocker.GetMock<IOwnerRepository>().Verify(r => r.AddAsync(It.IsAny<Owner>()), Times.Once);
        _mocker.GetMock<IFarmRepository>().Verify(r => r.AddAsync(It.IsAny<Farm>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Exactly(3));
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 10;
        var command = new CreateFarmCommand("Test", null, null, userId);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
