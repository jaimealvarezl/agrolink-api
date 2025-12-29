using System.Linq.Expressions;
using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Create;

[TestFixture]
public class CreatePaddockCommandHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreatePaddockCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreatePaddockCommandHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidCreatePaddockCommand_ReturnsPaddockDto()
    {
        // Arrange
        var userId = 10;
        var farmId = 1;
        var name = "Test Paddock";
        var area = 10.5m;
        var areaType = "Hectare";
        var command = new CreatePaddockCommand(name, farmId, userId, area, areaType);

        var farm = new Farm { Id = farmId, Name = "Test Farm" };
        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(member);

        _paddockRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Paddock>()))
            .Callback<Paddock>(p => p.Id = 1);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe(name);
        result.FarmName.ShouldBe(farm.Name);
        result.Area.ShouldBe(area);
        result.AreaType.ShouldBe(areaType);
        _paddockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Paddock>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreatePaddockCommand("Test", 99, 1, null, null);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new CreatePaddockCommand("Test", farmId, userId, null, null);

        _farmRepositoryMock
            .Setup(r => r.GetByIdAsync(farmId))
            .ReturnsAsync(new Farm { Id = farmId });
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
