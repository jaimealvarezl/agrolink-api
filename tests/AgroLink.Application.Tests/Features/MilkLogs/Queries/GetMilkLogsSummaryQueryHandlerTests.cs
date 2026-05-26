using AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogsSummary;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.MilkLogs.Queries;

[TestFixture]
public class GetMilkLogsSummaryQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetMilkLogsSummaryQueryHandler>();

        _mocker.GetMock<IDateTimeProvider>().Setup(d => d.TodayUtc).Returns(Today);
    }

    private AutoMocker _mocker = null!;
    private GetMilkLogsSummaryQueryHandler _handler = null!;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private void SetupPaged(int totalCount = 0, params DailyMilkLog[] logs)
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    1,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ((IEnumerable<DailyMilkLog>)logs, totalCount == 0 ? logs.Length : totalCount)
            );
    }

    private static DailyMilkLog MakeLog(DateOnly date, decimal liters = 100m, decimal? price = null)
    {
        return new DailyMilkLog
        {
            Id = 1,
            FarmId = 1,
            Date = date,
            TotalLiters = liters,
            PricePerLiter = price,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
        };
    }

    [Test]
    public async Task Handle_ReturnsCorrectAggregates()
    {
        SetupPaged(
            3,
            MakeLog(Today, 100m, 1.5m),
            MakeLog(Today.AddDays(-1), 200m, 2m),
            MakeLog(Today.AddDays(-2), 50m)
        );

        var result = await _handler.Handle(
            new GetMilkLogsSummaryQuery(1, Today.AddDays(-7), Today),
            CancellationToken.None
        );

        result.TotalLiters.ShouldBe(350m);
        result.TotalRevenue.ShouldBe(550m);
        result.DaysLogged.ShouldBe(3);
        result.AvgPricePerLiter.ShouldBe(1.75m);
    }

    [Test]
    public async Task Handle_NoLogs_ReturnsZeroTotalsAndNullAvgPrice()
    {
        SetupPaged();

        var result = await _handler.Handle(
            new GetMilkLogsSummaryQuery(1, Today.AddDays(-7), Today),
            CancellationToken.None
        );

        result.TotalLiters.ShouldBe(0m);
        result.TotalRevenue.ShouldBe(0m);
        result.DaysLogged.ShouldBe(0);
        result.AvgPricePerLiter.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NoDateRange_DefaultsToLast30Days()
    {
        DateOnly? capturedFrom = null;
        DateOnly? capturedTo = null;

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    1,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    1,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<int, DateOnly, DateOnly, int, int, CancellationToken>(
                (_, from, to, _, _, _) =>
                {
                    capturedFrom = from;
                    capturedTo = to;
                }
            )
            .ReturnsAsync(((IEnumerable<DailyMilkLog>)Array.Empty<DailyMilkLog>(), 0));

        await _handler.Handle(new GetMilkLogsSummaryQuery(1, null, null), CancellationToken.None);

        capturedFrom.ShouldBe(Today.AddDays(-30));
        capturedTo.ShouldBe(Today);
    }

    [Test]
    public async Task Handle_ExplicitDateRange_ExcludesLogsOutsideRange()
    {
        var from = Today.AddDays(-7);
        var to = Today;
        var allLogs = new[]
        {
            MakeLog(Today.AddDays(-5), 120m, 1.25m),
            MakeLog(Today.AddDays(-40), 900m, 2m),
        };

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    1,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    1,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (
                    int farmId,
                    DateOnly queryFrom,
                    DateOnly queryTo,
                    int page,
                    int pageSize,
                    CancellationToken ct
                ) =>
                {
                    var filtered = allLogs
                        .Where(l => l.Date >= queryFrom && l.Date <= queryTo)
                        .ToList();
                    return ((IEnumerable<DailyMilkLog>)filtered, filtered.Count);
                }
            );

        var result = await _handler.Handle(
            new GetMilkLogsSummaryQuery(1, from, to),
            CancellationToken.None
        );

        result.TotalLiters.ShouldBe(120m);
        result.TotalRevenue.ShouldBe(150m);
        result.DaysLogged.ShouldBe(1);
        result.From.ShouldBe(from);
        result.To.ShouldBe(to);
    }

    [Test]
    public async Task Handle_NoPricedLogs_AvgPricePerLiterIsNull()
    {
        SetupPaged(2, MakeLog(Today), MakeLog(Today.AddDays(-1), 200m));

        var result = await _handler.Handle(
            new GetMilkLogsSummaryQuery(1, Today.AddDays(-7), Today),
            CancellationToken.None
        );

        result.AvgPricePerLiter.ShouldBeNull();
        result.TotalRevenue.ShouldBe(0m);
    }
}
