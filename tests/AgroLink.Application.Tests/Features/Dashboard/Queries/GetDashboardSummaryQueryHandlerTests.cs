using System.Linq.Expressions;
using AgroLink.Application.Features.Dashboard.Queries.GetDashboardSummary;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Dashboard.Queries;

[TestFixture]
public class GetDashboardSummaryQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetDashboardSummaryQueryHandler>();

        SetupDefaultDependencies();
    }

    private AutoMocker _mocker = null!;
    private GetDashboardSummaryQueryHandler _handler = null!;

    private void SetupDefaultDependencies()
    {
        _mocker
            .GetMock<IRepository<Animal>>()
            .Setup(r =>
                r.CountAsync(
                    It.IsAny<Expression<Func<Animal, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(0);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r =>
                r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r =>
                r.GetLatestPerLotAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r =>
                r.FindAsync(
                    It.IsAny<Expression<Func<ChecklistItem, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    [Test]
    public async Task Handle_LogExistsForToday_SetsMilkTodayTotalLiters()
    {
        _mocker
            .GetMock<IRepository<DailyMilkLog>>()
            .Setup(r =>
                r.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<DailyMilkLog, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new DailyMilkLog { TotalLiters = 432.75m });

        var result = await _handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        result.MilkToday.ShouldBe(432.75m);
    }

    [Test]
    public async Task Handle_NoLogForToday_SetsMilkTodayNull()
    {
        _mocker
            .GetMock<IRepository<DailyMilkLog>>()
            .Setup(r =>
                r.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<DailyMilkLog, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((DailyMilkLog?)null);

        var result = await _handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        result.MilkToday.ShouldBeNull();
    }
}
