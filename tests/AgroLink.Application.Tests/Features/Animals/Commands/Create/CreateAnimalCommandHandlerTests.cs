using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Commands.Create;

[TestFixture]
public class CreateAnimalCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateAnimalCommandHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private CreateAnimalCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreateAnimalCommand_ReturnsAnimalDto()
    {
        // Arrange
        var createAnimalDto = new CreateAnimalDto
        {
            Tag = "A001",
            Name = "Test Animal",
            LotId = 1,
            Sex = "FEMALE",
            Owners = new List<AnimalOwnerDto>
            {
                new()
                {
                    OwnerId = 1,
                    OwnerName = "Test Owner",
                    SharePercent = 100,
                },
            },
        };
        var command = new CreateAnimalCommand(createAnimalDto);
        var animal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Test Animal",
            LotId = 1,
        };
        var lot = new Lot { Id = 1, Name = "Test Lot" };
        var owner = new Owner { Id = 1, Name = "Test Owner" };

        _animalRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Animal>()))
            .Callback<Animal>(a => a.Id = animal.Id); // Simulate DB ID generation
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _ownerRepositoryMock.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        _animalOwnerRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AnimalOwner>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(animal.Id))
            .ReturnsAsync(
                new List<AnimalOwner>
                {
                    new()
                    {
                        AnimalId = 1,
                        OwnerId = 1,
                        SharePercent = 100,
                    },
                }
            );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animal.Id);
        result.Tag.ShouldBe(animal.Tag);
        result.LotName.ShouldBe(lot.Name);
        result.Owners.Count.ShouldBe(1);
        result.Owners[0].OwnerName.ShouldBe(owner.Name);
        _animalRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Animal>()), Times.Once);
        _animalOwnerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AnimalOwner>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }
}
