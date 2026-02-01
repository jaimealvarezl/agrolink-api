using AgroLink.Application.Features.Animals.Queries.GetPagedList;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetPagedList;

[TestFixture]
public class GetAnimalsPagedListQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalsPagedListQueryHandler(_animalRepositoryMock.Object);
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalsPagedListQueryHandler _handler = null!;

    [Test]
    public async Task Handle_WithFilters_ReturnsFilteredPagedResult()
    {
        // Arrange
        var query = new GetAnimalsPagedListQuery(1, 1, 10, 2, "Test", true, true, false);

        var animals = new List<Animal>
        {
            new()
            {
                Id = 1,
                TagVisual = "Tag1",
                Name = "TestCow",
                Lot = new Lot { Name = "Lot A" },
                HealthStatus = HealthStatus.Sick,
                ReproductiveStatus = ReproductiveStatus.Pregnant,
            },
        };

        _animalRepositoryMock
            .Setup(r =>
                r.GetPagedListAsync(
                    query.FarmId,
                    query.Page,
                    query.PageSize,
                    query.LotId,
                    query.SearchTerm,
                    query.IsSick,
                    query.IsPregnant,
                    query.IsMissing
                )
            )
            .ReturnsAsync((animals, 1));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        var dto = result.Items.First();
        dto.TagVisual.ShouldBe("Tag1");
        dto.IsSick.ShouldBeTrue();
        dto.IsPregnant.ShouldBeTrue();
        dto.IsMissing.ShouldBeFalse();
        dto.LotName.ShouldBe("Lot A");

        _animalRepositoryMock.Verify(
            r =>
                r.GetPagedListAsync(
                    query.FarmId,
                    query.Page,
                    query.PageSize,
                    query.LotId,
                    query.SearchTerm,
                    query.IsSick,
                    query.IsPregnant,
                    query.IsMissing
                ),
            Times.Once
        );
    }
}
