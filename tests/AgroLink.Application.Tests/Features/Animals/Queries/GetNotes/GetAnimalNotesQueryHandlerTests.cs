using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Queries.GetNotes;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetNotes;

[TestFixture]
public class GetAnimalNotesQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalNotesQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalNotesQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ValidQuery_ReturnsNotesForAnimal()
    {
        // Arrange
        const int animalId = 1;
        const int farmId = 10;
        var query = new GetAnimalNotesQuery(animalId, farmId);
        var animal = new Animal { Id = animalId };
        var notes = new List<AnimalNote>
        {
            new()
            {
                Id = 1,
                AnimalId = animalId,
                Content = "First note",
                UserId = 5,
                User = new User { Name = "Dr. Smith" },
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            },
            new()
            {
                Id = 2,
                AnimalId = animalId,
                Content = "Second note",
                UserId = 6,
                User = new User { Name = "Vet Jones" },
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(animalId, farmId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(notes);

        // Act
        var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Content.ShouldBe("First note");
        result[0].UserName.ShouldBe("Dr. Smith");
        result[1].Content.ShouldBe("Second note");
        result[1].UserName.ShouldBe("Vet Jones");
    }

    [Test]
    public async Task Handle_AnimalNotFoundInFarm_ThrowsNotFoundException()
    {
        // Arrange — covers both "doesn't exist" and "belongs to a different farm"
        var query = new GetAnimalNotesQuery(999, 10);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(999, 10))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }
}
