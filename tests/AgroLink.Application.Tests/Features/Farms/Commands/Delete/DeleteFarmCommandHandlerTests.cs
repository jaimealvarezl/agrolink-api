using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Delete;

[TestFixture]
public class DeleteFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteFarmCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingFarm_SoftDeletesFarm()
    {
        var farmId = 1;
        var command = new DeleteFarmCommand(farmId, 10);
        var farm = new Farm { Id = farmId, IsActive = true };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _handler.Handle(command, CancellationToken.None);

        farm.IsActive.ShouldBeFalse();
        farm.DeletedAt.ShouldNotBeNull();
        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(farm), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsSuccessfullyForIdempotency()
    {
        var command = new DeleteFarmCommand(999, 10);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Farm?)null);

        await _handler.Handle(command, CancellationToken.None);

        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_AlreadyDeletedFarm_ReturnsSuccessfullyForIdempotency()
    {
        var farmId = 1;
        var command = new DeleteFarmCommand(farmId, 10);
        var farm = new Farm { Id = farmId, IsActive = false };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        await _handler.Handle(command, CancellationToken.None);

        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
