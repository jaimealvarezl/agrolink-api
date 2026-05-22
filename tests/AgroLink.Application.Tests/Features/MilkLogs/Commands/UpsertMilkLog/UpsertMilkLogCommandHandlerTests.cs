using AgroLink.Application.Features.MilkLogs.Commands.UpsertMilkLog;
using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.MilkLogs.Commands.UpsertMilkLog;

[TestFixture]
public class UpsertMilkLogCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpsertMilkLogCommandHandler>();

        _mocker.GetMock<IDateTimeProvider>().Setup(d => d.TodayUtc).Returns(Today);
        _mocker
            .GetMock<IDateTimeProvider>()
            .Setup(d => d.UtcNow)
            .Returns(Today.ToDateTime(TimeOnly.MinValue));

        _mocker
            .GetMock<IUnitOfWork>()
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private AutoMocker _mocker = null!;
    private UpsertMilkLogCommandHandler _handler = null!;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static UpsertMilkLogCommand BuildCommand(
        int farmId = 1,
        int userId = 5,
        DateOnly? date = null,
        decimal totalLiters = 100m,
        decimal? pricePerLiter = null,
        string? notes = null
    )
    {
        return new UpsertMilkLogCommand(
            farmId,
            userId,
            new UpsertMilkLogRequest
            {
                Date = date ?? Today,
                TotalLiters = totalLiters,
                PricePerLiter = pricePerLiter,
                Notes = notes,
            }
        );
    }

    private void SetupNoExistingLog(DateOnly? date = null)
    {
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r =>
                r.FindByDateAsync(It.IsAny<int>(), date ?? Today, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((DailyMilkLog?)null);
    }

    // --- Create path ---

    [Test]
    public async Task Handle_NewLog_CallsAddAndReturnsMilkLogDto()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsNew.ShouldBeTrue();
        result.Log.FarmId.ShouldBe(1);
        result.Log.TotalLiters.ShouldBe(100m);
        result.Log.UserId.ShouldBe(5);

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<DailyMilkLog>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(r => r.Update(It.IsAny<DailyMilkLog>()), Times.Never);
    }

    [Test]
    public async Task Handle_NewLog_SavesCorrectFields()
    {
        SetupNoExistingLog();

        DailyMilkLog? captured = null;
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.AddAsync(It.IsAny<DailyMilkLog>(), It.IsAny<CancellationToken>()))
            .Callback<DailyMilkLog, CancellationToken>((log, _) => captured = log);

        await _handler.Handle(
            BuildCommand(pricePerLiter: 1.5m, notes: "good day"),
            CancellationToken.None
        );

        captured.ShouldNotBeNull();
        captured!.FarmId.ShouldBe(1);
        captured.Date.ShouldBe(Today);
        captured.TotalLiters.ShouldBe(100m);
        captured.PricePerLiter.ShouldBe(1.5m);
        captured.UserId.ShouldBe(5);
        captured.Notes.ShouldBe("good day");
        captured.UpdatedAt.ShouldBeNull();
    }

    // --- Update path ---

    [Test]
    public async Task Handle_ExistingLog_CallsUpdateAndReturnsIsNewFalse()
    {
        var existing = new DailyMilkLog
        {
            Id = 7,
            FarmId = 1,
            Date = Today,
            TotalLiters = 50m,
            PricePerLiter = 1.0m,
            UserId = 3,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
        };

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindByDateAsync(1, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(
            BuildCommand(totalLiters: 200m, pricePerLiter: 2.5m, notes: "revised"),
            CancellationToken.None
        );

        result.IsNew.ShouldBeFalse();
        result.Log.Id.ShouldBe(7);
        result.Log.TotalLiters.ShouldBe(200m);
        result.Log.PricePerLiter.ShouldBe(2.5m);
        result.Log.Notes.ShouldBe("revised");

        _mocker.GetMock<IDailyMilkLogRepository>().Verify(r => r.Update(existing), Times.Once);
        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(
                r => r.AddAsync(It.IsAny<DailyMilkLog>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
    }

    [Test]
    public async Task Handle_ExistingLog_SetsUpdatedAtAndNewUserId()
    {
        var existing = new DailyMilkLog
        {
            Id = 1,
            FarmId = 1,
            Date = Today,
            TotalLiters = 50m,
            UserId = 3,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
        };

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Setup(r => r.FindByDateAsync(1, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(BuildCommand(userId: 9, totalLiters: 300m), CancellationToken.None);

        existing.UserId.ShouldBe(9);
        existing.UpdatedAt.ShouldNotBeNull();
    }

    // --- SaveChanges ---

    [Test]
    public async Task Handle_AlwaysCallsSaveChanges()
    {
        SetupNoExistingLog();

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Validation: date ---

    [Test]
    public async Task Handle_FutureDate_ThrowsArgumentException()
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(date: Today.AddDays(1)), CancellationToken.None)
        );
        ex.Message.ShouldContain("future");
    }

    [Test]
    public async Task Handle_DateExactly30DaysAgo_Succeeds()
    {
        var date = Today.AddDays(-UpsertMilkLogCommandHandler.MaxDaysInPast);
        SetupNoExistingLog(date);

        var result = await _handler.Handle(BuildCommand(date: date), CancellationToken.None);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_Date31DaysAgo_ThrowsArgumentException()
    {
        var date = Today.AddDays(-(UpsertMilkLogCommandHandler.MaxDaysInPast + 1));
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(date: date), CancellationToken.None)
        );
        ex.Message.ShouldContain("past");
    }

    [Test]
    public async Task Handle_DateIsToday_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(BuildCommand(date: Today), CancellationToken.None);

        result.ShouldNotBeNull();
    }

    // --- Validation: totalLiters ---

    [Test]
    public async Task Handle_NegativeLiters_ThrowsArgumentException()
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(totalLiters: -0.01m), CancellationToken.None)
        );
        ex.Message.ShouldContain("TotalLiters");
    }

    [Test]
    public async Task Handle_ZeroLiters_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(BuildCommand(totalLiters: 0m), CancellationToken.None);

        result.Log.TotalLiters.ShouldBe(0m);
    }

    [Test]
    public async Task Handle_LitersAtMaxBoundary_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(
            BuildCommand(totalLiters: UpsertMilkLogCommandHandler.MaxLiters),
            CancellationToken.None
        );

        result.Log.TotalLiters.ShouldBe(UpsertMilkLogCommandHandler.MaxLiters);
    }

    [Test]
    public async Task Handle_LitersAboveMax_ThrowsArgumentException()
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(
                BuildCommand(totalLiters: UpsertMilkLogCommandHandler.MaxLiters + 0.01m),
                CancellationToken.None
            )
        );
        ex.Message.ShouldContain("TotalLiters");
    }

    // --- Validation: pricePerLiter ---

    [Test]
    public async Task Handle_NullPrice_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(
            BuildCommand(pricePerLiter: null),
            CancellationToken.None
        );

        result.Log.PricePerLiter.ShouldBeNull();
        result.Log.RevenueTotal.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ZeroPrice_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(BuildCommand(pricePerLiter: 0m), CancellationToken.None);

        result.Log.PricePerLiter.ShouldBe(0m);
        result.Log.RevenueTotal.ShouldBe(0m);
    }

    [Test]
    public async Task Handle_PriceAtMaxBoundary_Succeeds()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(
            BuildCommand(pricePerLiter: UpsertMilkLogCommandHandler.MaxPricePerLiter),
            CancellationToken.None
        );

        result.Log.PricePerLiter.ShouldBe(UpsertMilkLogCommandHandler.MaxPricePerLiter);
    }

    [Test]
    public async Task Handle_NegativePrice_ThrowsArgumentException()
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(pricePerLiter: -0.01m), CancellationToken.None)
        );
        ex.Message.ShouldContain("PricePerLiter");
    }

    [Test]
    public async Task Handle_PriceAboveMax_ThrowsArgumentException()
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(
                BuildCommand(pricePerLiter: UpsertMilkLogCommandHandler.MaxPricePerLiter + 0.0001m),
                CancellationToken.None
            )
        );
        ex.Message.ShouldContain("PricePerLiter");
    }

    // --- Revenue computation ---

    [Test]
    [TestCase(100, 2.50, 250.00)]
    [TestCase(333.33, 1.2345, 411.50)] // 333.33 * 1.2345 = 411.4959 rounds to 411.50
    [TestCase(0, 5.00, 0.00)]
    public async Task Handle_WithPrice_RevenueTotalIsRoundedProduct(
        double liters,
        double price,
        double expectedRevenue
    )
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(
            BuildCommand(totalLiters: (decimal)liters, pricePerLiter: (decimal)price),
            CancellationToken.None
        );

        result.Log.RevenueTotal.ShouldBe((decimal)expectedRevenue);
    }

    [Test]
    public async Task Handle_WithoutPrice_RevenueTotalIsNull()
    {
        SetupNoExistingLog();

        var result = await _handler.Handle(
            BuildCommand(pricePerLiter: null),
            CancellationToken.None
        );

        result.Log.RevenueTotal.ShouldBeNull();
    }

    // --- No repository calls on validation failure ---

    [Test]
    public async Task Handle_ValidationFailure_NeverCallsRepository()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(BuildCommand(date: Today.AddDays(5)), CancellationToken.None)
        );

        _mocker
            .GetMock<IDailyMilkLogRepository>()
            .Verify(
                r =>
                    r.FindByDateAsync(
                        It.IsAny<int>(),
                        It.IsAny<DateOnly>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        _mocker
            .GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
