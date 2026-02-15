using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Commands.Move;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Move;

[TestFixture]
public class MoveAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _movementRepositoryMock = new Mock<IMovementRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new MoveAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _movementRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _storageServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IMovementRepository> _movementRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private MoveAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidMoveAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        const int animalId = 1;
        const int fromLotId = 1;
        const int toLotId = 2;
        const int userId = 1;
        const int farmId = 10;
        var command = new MoveAnimalCommand(animalId, fromLotId, toLotId, userId, "Test Reason");
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = fromLotId,
            CreatedAt = DateTime.UtcNow,
            LifeStatus = LifeStatus.Active,
        };
        var lotTo = new Lot
        {
            Id = toLotId,
            Name = "Lot To",
            Paddock = new Paddock { FarmId = farmId },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(toLotId)).ReturnsAsync(lotTo);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _movementRepositoryMock
            .Setup(r => r.AddMovementAsync(It.IsAny<Movement>()))
            .Returns(Task.CompletedTask);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(toLotId)).ReturnsAsync(lotTo);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalOwner>());
        _animalPhotoRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalPhoto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.LotId.ShouldBe(toLotId);
        _animalRepositoryMock.Verify(r => r.Update(animal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _movementRepositoryMock.Verify(
            r =>
                r.AddMovementAsync(
                    It.Is<Movement>(m =>
                        m.FromId == fromLotId && m.ToId == toLotId && m.UserId == userId
                    )
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ThrowsArgumentException()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 1;
        var command = new MoveAnimalCommand(animalId, 1, 2, userId, "Test Reason");

        _animalRepositoryMock
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found or access denied.");
    }

    [Test]
    public async Task Handle_NoPermissionOnTargetFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int toLotId = 2;
        var command = new MoveAnimalCommand(animalId, 1, toLotId, userId, "Test Reason");
        var animal = new Animal { Id = animalId, LotId = 1 };
        var lotTo = new Lot
        {
            Id = toLotId,
            Paddock = new Paddock { FarmId = 20 },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetLotWithPaddockAsync(toLotId)).ReturnsAsync(lotTo);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
