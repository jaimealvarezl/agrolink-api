using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.ReproductiveEvents.Commands.Create;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace AgroLink.Application.Tests.Features.ReproductiveEvents.Commands;

[TestFixture]
public class CreateReproductiveEventCommandHandlerTests
{
    [SetUp]
    public void SetUp()
    {
        _animalRepository = Substitute.For<IAnimalRepository>();
        _reproductiveEventRepository = Substitute.For<IReproductiveEventRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new CreateReproductiveEventCommandHandler(
            _animalRepository,
            _reproductiveEventRepository,
            _unitOfWork
        );

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private IAnimalRepository _animalRepository = null!;
    private IReproductiveEventRepository _reproductiveEventRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateReproductiveEventCommandHandler _handler = null!;

    [Test]
    public async Task Handle_MatingPositive_ShouldSetExpectedDueDateToDatePlus283Days()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var date = new DateTime(2026, 5, 1);
        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = date,
                Status = ReproductiveEventStatus.Positive,
            }
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ExpectedDueDate.ShouldBe(date.AddDays(283));
    }

    [Test]
    public async Task Handle_MatingNegative_ShouldLeaveExpectedDueDateNull()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Status = ReproductiveEventStatus.Negative,
            }
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ExpectedDueDate.ShouldBeNull();
    }

    [Test]
    public async Task Handle_PregnancyCheckPositiveMonths3_ShouldSetExpectedDueDateToTodayPlus180()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var start = DateTime.UtcNow.Date;
        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 3,
            }
        );

        var result = await _handler.Handle(command, CancellationToken.None);
        var end = DateTime.UtcNow.Date;

        result.ExpectedDueDate.ShouldNotBeNull();
        result.ExpectedDueDate.Value.ShouldBeInRange(start.AddDays(180), end.AddDays(180));
    }

    [Test]
    public async Task Handle_PregnancyCheckPositiveMonths9_ShouldSetExpectedDueDateToToday()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var start = DateTime.UtcNow.Date;
        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 9,
            }
        );

        var result = await _handler.Handle(command, CancellationToken.None);
        var end = DateTime.UtcNow.Date;

        result.ExpectedDueDate.ShouldNotBeNull();
        result.ExpectedDueDate.Value.ShouldBeInRange(start, end);
    }

    [Test]
    public async Task Handle_PregnancyCheckPositive_ShouldFlipAnimalStatusToPregnant()
    {
        var animal = BuildFemaleAnimal();
        animal.ReproductiveStatus = ReproductiveStatus.Open;

        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 4,
            }
        );

        await _handler.Handle(command, CancellationToken.None);

        animal.ReproductiveStatus.ShouldBe(ReproductiveStatus.Pregnant);
        animal.UpdatedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_PregnancyCheckNegative_WithNoLaterPositive_ShouldFlipToOpen()
    {
        var animal = BuildFemaleAnimal();
        animal.ReproductiveStatus = ReproductiveStatus.Pregnant;

        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);
        _reproductiveEventRepository
            .GetLatestPositivePregnancyOrMatingAsync(1, Arg.Any<CancellationToken>())
            .Returns((ReproductiveEvent?)null);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Negative,
            }
        );

        await _handler.Handle(command, CancellationToken.None);

        animal.ReproductiveStatus.ShouldBe(ReproductiveStatus.Open);
    }

    [Test]
    public async Task Handle_PregnancyCheckNegative_BackdatedAgainstLaterPositive_ShouldKeepStatusUntouched()
    {
        var animal = BuildFemaleAnimal();
        animal.ReproductiveStatus = ReproductiveStatus.Pregnant;

        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);
        _reproductiveEventRepository
            .GetLatestPositivePregnancyOrMatingAsync(1, Arg.Any<CancellationToken>())
            .Returns(
                new ReproductiveEvent
                {
                    AnimalId = 1,
                    EventType = ReproductiveEventType.PregnancyCheck,
                    Status = ReproductiveEventStatus.Positive,
                    Date = DateTime.UtcNow.Date.AddDays(-2),
                }
            );

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date.AddDays(-10),
                Status = ReproductiveEventStatus.Negative,
            }
        );

        await _handler.Handle(command, CancellationToken.None);

        animal.ReproductiveStatus.ShouldBe(ReproductiveStatus.Pregnant);
    }

    [Test]
    public async Task Handle_MaleAnimal_ShouldThrowArgumentException()
    {
        _animalRepository
            .GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new Animal { Id = 1, Sex = Sex.Male });

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date,
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_FutureDatedEvent_ShouldThrowArgumentException()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date.AddDays(1),
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_EstimatedMonthsOnHeatOrMating_ShouldThrowArgumentException()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date,
                EstimatedMonths = 4,
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_EstimatedMonthsOutOfRange_ShouldThrowArgumentException()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 10,
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_AnimalNotInFarm_ShouldThrowNotFoundException()
    {
        _animalRepository
            .GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns((Animal?)null);

        var command = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date,
            }
        );

        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_BullIdFemaleOrOffFarm_ShouldThrowArgumentException()
    {
        var animal = BuildFemaleAnimal();
        _animalRepository.GetByIdInFarmAsync(1, 10, Arg.Any<CancellationToken>()).Returns(animal);

        _animalRepository
            .GetByIdInFarmAsync(88, 10, Arg.Any<CancellationToken>())
            .Returns(new Animal { Id = 88, Sex = Sex.Female });

        var femaleBullCommand = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = DateTime.UtcNow.Date,
                BullId = 88,
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(femaleBullCommand, CancellationToken.None)
        );

        _animalRepository
            .GetByIdInFarmAsync(99, 10, Arg.Any<CancellationToken>())
            .Returns((Animal?)null);

        var offFarmBullCommand = BuildCommand(
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = DateTime.UtcNow.Date,
                BullId = 99,
            }
        );

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(offFarmBullCommand, CancellationToken.None)
        );
    }

    private static Animal BuildFemaleAnimal()
    {
        return new Animal
        {
            Id = 1,
            Sex = Sex.Female,
            ReproductiveStatus = ReproductiveStatus.Open,
        };
    }

    private static CreateReproductiveEventCommand BuildCommand(CreateReproductiveEventDto dto)
    {
        return new CreateReproductiveEventCommand(10, 1, 5, dto);
    }
}
