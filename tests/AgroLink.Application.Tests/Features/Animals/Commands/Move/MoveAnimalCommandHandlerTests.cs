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
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new MoveAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _movementRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IMovementRepository> _movementRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private MoveAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidMoveAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        var animalId = 1;
        var fromLotId = 1;
        var toLotId = 2;
        var userId = 1;
        var command = new MoveAnimalCommand(animalId, fromLotId, toLotId, "Test Reason");
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
        var lotFrom = new Lot { Id = fromLotId, Name = "Lot From" };
        var lotTo = new Lot { Id = toLotId, Name = "Lot To" };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(fromLotId)).ReturnsAsync(lotFrom);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(toLotId)).ReturnsAsync(lotTo);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _movementRepositoryMock
            .Setup(r => r.AddMovementAsync(It.IsAny<Movement>()))
            .Returns(Task.CompletedTask);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(animal.LotId)).ReturnsAsync(lotTo); // After move
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
        var animalId = 999;
        var fromLotId = 1;
        var toLotId = 2;
        var userId = 1;
        var command = new MoveAnimalCommand(animalId, fromLotId, toLotId, "Test Reason");

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId)).ReturnsAsync((Animal?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Animal not found");
        _animalRepositoryMock.Verify(r => r.Update(It.IsAny<Animal>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _movementRepositoryMock.Verify(r => r.AddMovementAsync(It.IsAny<Movement>()), Times.Never);
    }

    [Test]
    public async Task Handle_UnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var animalId = 1;
        var fromLotId = 1;
        var toLotId = 2;
        var command = new MoveAnimalCommand(animalId, fromLotId, toLotId, "Test Reason");

        _currentUserServiceMock
            .Setup(s => s.GetRequiredUserId())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
