using System.Linq.Expressions;
using AgroLink.Application.Features.Animals.Queries.GetPagedList;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetPagedList;

[TestFixture]
public class GetAnimalsPagedListQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalsPagedListQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalsPagedListQueryHandler _handler = null!;

    [Test]
    public async Task Handle_WithFilters_ReturnsFilteredPagedResult()
    {
        // Arrange
        var query = new GetAnimalsPagedListQuery(
            1,
            1,
            10,
            2,
            "Test",
            true,
            true,
            false,
            Sex.Female
        );

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(1);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(true);

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
                Sex = Sex.Female,
                Photos = new List<AnimalPhoto>
                {
                    new() { StorageKey = "p1", UriRemote = "old" },
                },
            },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r =>
                r.GetPagedListAsync(
                    query.FarmId,
                    query.Page,
                    query.PageSize,
                    query.LotId,
                    query.SearchTerm,
                    query.IsSick,
                    query.IsPregnant,
                    query.IsMissing,
                    query.Sex
                )
            )
            .ReturnsAsync((animals, 1));

        _mocker
            .GetMock<IStorageService>()
            .Setup(s => s.GetPresignedUrl("p1", It.IsAny<TimeSpan>()))
            .Returns("http://signed.com/p1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        var dto = result.Items.First();
        dto.TagVisual.ShouldBe("Tag1");
        dto.PhotoUrl.ShouldBe("http://signed.com/p1");
        dto.IsSick.ShouldBeTrue();
        dto.IsPregnant.ShouldBeTrue();
        dto.IsMissing.ShouldBeFalse();
        dto.LotName.ShouldBe("Lot A");

        _mocker
            .GetMock<IAnimalRepository>()
            .Verify(
                r =>
                    r.GetPagedListAsync(
                        query.FarmId,
                        query.Page,
                        query.PageSize,
                        query.LotId,
                        query.SearchTerm,
                        query.IsSick,
                        query.IsPregnant,
                        query.IsMissing,
                        query.Sex
                    ),
                Times.Once
            );
    }
}
