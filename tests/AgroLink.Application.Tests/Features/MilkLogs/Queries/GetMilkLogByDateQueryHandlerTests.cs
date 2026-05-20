using AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogByDate;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.MilkLogs.Queries;

[TestFixture]
public class GetMilkLogByDateQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetMilkLogByDateQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetMilkLogByDateQueryHandler _handler = null!;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Test]
    public async Task Handle_LogExists_ReturnsMappedDto()
    {
        var log = new DailyMilkLog
        {
            Id = 3,
            FarmId = 1,
            Date = Today,
            TotalLiters = 250m,
            PricePerLiter = 2m,
            UserId = 7,
            Notes = "post-rain",
            CreatedAt = DateTime.UtcNow,
        };

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindByDateAsync(1, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        var result = await _handler.Handle(
            new GetMilkLogByDateQuery(1, Today),
            CancellationToken.None
        );

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(3);
        result.TotalLiters.ShouldBe(250m);
        result.PricePerLiter.ShouldBe(2m);
        result.RevenueTotal.ShouldBe(500m);
        result.Notes.ShouldBe("post-rain");
        result.Date.ShouldBe(Today);
    }

    [Test]
    public async Task Handle_LogNotFound_ReturnsNull()
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindByDateAsync(1, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyMilkLog?)null);

        var result = await _handler.Handle(
            new GetMilkLogByDateQuery(1, Today),
            CancellationToken.None
        );

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_PassesFarmIdAndDateToRepository()
    {
        var date = Today.AddDays(-5);

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindByDateAsync(42, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyMilkLog?)null);

        await _handler.Handle(new GetMilkLogByDateQuery(42, date), CancellationToken.None);

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(r => r.FindByDateAsync(42, date, It.IsAny<CancellationToken>()), Times.Once);
    }
}
