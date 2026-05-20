using AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.MilkLogs.Queries;

[TestFixture]
public class GetMilkLogsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetMilkLogsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetMilkLogsQueryHandler _handler = null!;

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
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                ((IEnumerable<DailyMilkLog>)logs, totalCount == 0 ? logs.Length : totalCount)
            );
    }

    private void SetupLastPrice(decimal? price)
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.FindLastPricePerLiterAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(price);
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

    // --- Date range defaults ---

    [Test]
    public async Task Handle_NoDateRange_QueriesLast30Days()
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
                    It.IsAny<int>(),
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
        SetupLastPrice(null);

        await _handler.Handle(new GetMilkLogsQuery(1, null, null), CancellationToken.None);

        capturedFrom.ShouldBe(Today.AddDays(-30));
        capturedTo.ShouldBe(Today);
    }

    [Test]
    public async Task Handle_ExplicitDateRange_PassesRangeToRepository()
    {
        var from = Today.AddDays(-10);
        var to = Today.AddDays(-1);

        DateOnly? capturedFrom = null;
        DateOnly? capturedTo = null;

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    1,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<int, DateOnly, DateOnly, int, int, CancellationToken>(
                (_, f, t, _, _, _) =>
                {
                    capturedFrom = f;
                    capturedTo = t;
                }
            )
            .ReturnsAsync(((IEnumerable<DailyMilkLog>)Array.Empty<DailyMilkLog>(), 0));
        SetupLastPrice(null);

        await _handler.Handle(new GetMilkLogsQuery(1, from, to), CancellationToken.None);

        capturedFrom.ShouldBe(from);
        capturedTo.ShouldBe(to);
    }

    // --- Pagination params forwarded ---

    [Test]
    public async Task Handle_PassesPageAndPageSizeToRepository()
    {
        int? capturedPage = null;
        int? capturedPageSize = null;

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<int, DateOnly, DateOnly, int, int, CancellationToken>(
                (_, _, _, page, size, _) =>
                {
                    capturedPage = page;
                    capturedPageSize = size;
                }
            )
            .ReturnsAsync(((IEnumerable<DailyMilkLog>)Array.Empty<DailyMilkLog>(), 0));
        SetupLastPrice(null);

        await _handler.Handle(new GetMilkLogsQuery(1, null, null, 2, 10), CancellationToken.None);

        capturedPage.ShouldBe(2);
        capturedPageSize.ShouldBe(10);
    }

    // --- Pagination metadata in response ---

    [Test]
    public async Task Handle_ReturnsPaginationMetadata()
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    2,
                    10,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(((IEnumerable<DailyMilkLog>)Array.Empty<DailyMilkLog>(), 25));
        SetupLastPrice(null);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null, 2, 10),
            CancellationToken.None
        );

        result.TotalCount.ShouldBe(25);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(10);
        result.TotalPages.ShouldBe(3); // ceil(25/10)
    }

    // --- Items mapping ---

    [Test]
    public async Task Handle_ReturnsMappedDtos()
    {
        SetupPaged(2, MakeLog(Today, 150m), MakeLog(Today.AddDays(-1), 200m));
        SetupLastPrice(null);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.Items.Count().ShouldBe(2);
    }

    [Test]
    public async Task Handle_EmptyResult_ReturnsEmptyItemsCollection()
    {
        SetupPaged();
        SetupLastPrice(null);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
    }

    // --- LastUsedPricePerLiter ---

    [Test]
    public async Task Handle_NoPricedLogs_ReturnsNullLastUsedPrice()
    {
        SetupPaged();
        SetupLastPrice(null);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.LastUsedPricePerLiter.ShouldBeNull();
    }

    [Test]
    public async Task Handle_HasPricedLog_ReturnsLastUsedPrice()
    {
        SetupPaged();
        SetupLastPrice(1.75m);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.LastUsedPricePerLiter.ShouldBe(1.75m);
    }

    [Test]
    public async Task Handle_LastPriceQueryUsesCorrectFarmId()
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.GetPagedByDateRangeAsync(
                    42,
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(((IEnumerable<DailyMilkLog>)Array.Empty<DailyMilkLog>(), 0));
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindLastPricePerLiterAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        await _handler.Handle(new GetMilkLogsQuery(42, null, null), CancellationToken.None);

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(
                r => r.FindLastPricePerLiterAsync(42, It.IsAny<CancellationToken>()),
                Times.Once
            );
    }

    // --- Revenue in returned DTOs ---

    [Test]
    public async Task Handle_LogsWithPrices_RevenueTotalComputedInItems()
    {
        SetupPaged(1, MakeLog(Today, 200m, 1.5m));
        SetupLastPrice(1.5m);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.Items.Single().RevenueTotal.ShouldBe(300m);
    }

    [Test]
    public async Task Handle_LogWithNullPrice_RevenueTotalIsNull()
    {
        SetupPaged(1, MakeLog(Today, 200m));
        SetupLastPrice(null);

        var result = await _handler.Handle(
            new GetMilkLogsQuery(1, null, null),
            CancellationToken.None
        );

        result.Items.Single().RevenueTotal.ShouldBeNull();
    }
}
