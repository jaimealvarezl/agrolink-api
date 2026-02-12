using System.Linq.Expressions;
using AgroLink.Application.Features.Animals.Commands.SetProfilePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.SetProfilePhoto;

[TestFixture]
public class SetAnimalProfilePhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SetAnimalProfilePhotoCommandHandler(
            _animalRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private SetAnimalProfilePhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_SetsProfilePhoto()
    {
        // Arrange
        var animalId = 1;
        var photoId = 100;
        var farmId = 10;
        var command = new SetAnimalProfilePhotoCommand(animalId, photoId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        var photo = new AnimalPhoto { Id = photoId, AnimalId = animalId };

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _animalPhotoRepositoryMock.Verify(
            r => r.SetProfilePhotoAsync(animalId, photoId),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_PhotoDoesNotBelongToAnimal_ThrowsArgumentException()
    {
        // Arrange
        var animalId = 1;
        var photoId = 100;
        var command = new SetAnimalProfilePhotoCommand(animalId, photoId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = 10 } },
        };
        var photo = new AnimalPhoto { Id = photoId, AnimalId = 2 }; // Different animal

        _animalRepositoryMock.Setup(r => r.GetAnimalDetailsAsync(animalId)).ReturnsAsync(animal);
        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(5);
        _farmMemberRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);
        _animalPhotoRepositoryMock.Setup(r => r.GetByIdAsync(photoId)).ReturnsAsync(photo);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
