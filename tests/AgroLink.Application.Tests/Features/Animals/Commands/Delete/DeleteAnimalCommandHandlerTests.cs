using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Delete;

[TestFixture]
public class DeleteAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeleteAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithPermission_SoftDeletesAnimal()
    {
        // Arrange
        var animalId = 1;
        var command = new DeleteAnimalCommand(animalId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        animal.LifeStatus.ShouldBe(LifeStatus.Deleted);
        _animalRepositoryMock.Verify(r => r.Update(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        var animalId = 999;
        var command = new DeleteAnimalCommand(animalId);

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found");
    }

    [Test]
    public async Task Handle_NoPermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        var animalId = 1;
        var command = new DeleteAnimalCommand(animalId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = 10 },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
