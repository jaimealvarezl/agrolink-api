using System.Net;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Notifications;

public class NotificationsIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task SecadoScan_WithMatchingPregnantAnimal_SendsOnceAndDeduplicates()
    {
        var (farm, animal, memberUser) = await SetupFarmWithPregnantAnimalAsync();
        DbContext.DeviceTokens.Add(
            new DeviceToken
            {
                UserId = memberUser.Id,
                Token = $"token-{Guid.NewGuid()}",
                Platform = "android",
            }
        );
        await DbContext.SaveChangesAsync();

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var firstResponse = await CreateSecadoScanRequestAsync();
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK, firstBody);

        var secondResponse = await CreateSecadoScanRequestAsync();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK, secondBody);

        DbContext.ChangeTracker.Clear();
        var rows = await DbContext.SentNotifications.CountAsync(x => x.AnimalId == animal.Id);
        rows.ShouldBe(1);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(1);
    }

    [Test]
    public async Task SecadoScan_WithNonPregnantAnimal_SendsZero()
    {
        await SetupFarmWithAnimalForDueDateAsync(ReproductiveStatus.Open, GetSecadoTargetDueDate());

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var response = await CreateSecadoScanRequestAsync();
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(0);

        var rows = await DbContext.SentNotifications.CountAsync();
        rows.ShouldBe(0);
    }

    [Test]
    public async Task SecadoScan_WithExpectedDueDateOffByOne_SendsZero()
    {
        var offByOneDueDate = GetSecadoTargetDueDate().AddDays(1);
        await SetupFarmWithAnimalForDueDateAsync(ReproductiveStatus.Pregnant, offByOneDueDate);

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var response = await CreateSecadoScanRequestAsync();
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(0);

        var rows = await DbContext.SentNotifications.CountAsync();
        rows.ShouldBe(0);
    }

    [Test]
    public async Task BirthWatchScan_WithMatchingPregnantAnimal_SendsOnceAndDeduplicates()
    {
        var (_, animal, memberUser) = await SetupFarmWithAnimalForDueDateAsync(
            ReproductiveStatus.Pregnant,
            GetBirthWatchWindowEnd()
        );
        DbContext.DeviceTokens.Add(
            new DeviceToken
            {
                UserId = memberUser.Id,
                Token = $"token-{Guid.NewGuid()}",
                Platform = "android",
            }
        );
        await DbContext.SaveChangesAsync();

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var firstResponse = await CreateBirthWatchScanRequestAsync();
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK, firstBody);

        var secondResponse = await CreateBirthWatchScanRequestAsync();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK, secondBody);

        DbContext.ChangeTracker.Clear();
        var rows = await DbContext.SentNotifications.CountAsync(x => x.AnimalId == animal.Id);
        rows.ShouldBe(1);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(1);
    }

    [Test]
    public async Task BirthWatchScan_WithCatchUpWindow_FiresForAnimalInsideWindow()
    {
        var dueDateInsideWindow = GetBirthWatchWindowEnd().AddDays(-9);
        var (_, animal, memberUser) = await SetupFarmWithAnimalForDueDateAsync(
            ReproductiveStatus.Pregnant,
            dueDateInsideWindow
        );
        DbContext.DeviceTokens.Add(
            new DeviceToken
            {
                UserId = memberUser.Id,
                Token = $"token-{Guid.NewGuid()}",
                Platform = "android",
            }
        );
        await DbContext.SaveChangesAsync();

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var response = await CreateBirthWatchScanRequestAsync();
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(1);

        DbContext.ChangeTracker.Clear();
        var rows = await DbContext.SentNotifications.CountAsync(x => x.AnimalId == animal.Id);
        rows.ShouldBe(1);
    }

    [Test]
    public async Task BirthWatchScan_WithBoundary_OutsideWindowSendsZero()
    {
        var outsideWindowDueDate = GetBirthWatchWindowEnd().AddDays(1);
        await SetupFarmWithAnimalForDueDateAsync(ReproductiveStatus.Pregnant, outsideWindowDueDate);

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var response = await CreateBirthWatchScanRequestAsync();
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(0);

        var rows = await DbContext.SentNotifications.CountAsync();
        rows.ShouldBe(0);
    }

    [Test]
    public async Task BirthWatchScan_WithNonPregnantAnimal_SendsZero()
    {
        await SetupFarmWithAnimalForDueDateAsync(ReproductiveStatus.Open, GetBirthWatchWindowEnd());

        var fakePushSender = Factory.Services.GetRequiredService<FakePushNotificationSender>();
        var callsBefore = fakePushSender.Calls.Count;

        var response = await CreateBirthWatchScanRequestAsync();
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, responseBody);

        var callsAfter = fakePushSender.Calls.Count;
        (callsAfter - callsBefore).ShouldBe(0);

        var rows = await DbContext.SentNotifications.CountAsync();
        rows.ShouldBe(0);
    }

    [Test]
    public async Task PostDeviceToken_WithoutAuth_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/me/device-tokens",
            new { Token = "abc", Platform = "ios" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> CreateSecadoScanRequestAsync()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/internal/jobs/secado-alert-scan"
        );
        request.Headers.Add("X-Internal-Job-Key", "test-job-key");
        return await Client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> CreateBirthWatchScanRequestAsync()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/internal/jobs/birth-watch-alert-scan"
        );
        request.Headers.Add("X-Internal-Job-Key", "test-job-key");
        return await Client.SendAsync(request);
    }

    private async Task<(Farm farm, Animal animal, User user)> SetupFarmWithPregnantAnimalAsync()
    {
        return await SetupFarmWithAnimalForDueDateAsync(
            ReproductiveStatus.Pregnant,
            GetSecadoTargetDueDate()
        );
    }

    private async Task<(Farm farm, Animal animal, User user)> SetupFarmWithAnimalForDueDateAsync(
        ReproductiveStatus reproductiveStatus,
        DateOnly dueDate
    )
    {
        var user = new User
        {
            Name = "Member User",
            Email = $"member-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Farm Notifications", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var paddock = new Paddock { Name = "Paddock", FarmId = farm.Id };
        DbContext.Paddocks.Add(paddock);
        await DbContext.SaveChangesAsync();

        var lot = new Lot
        {
            Name = "Lot",
            PaddockId = paddock.Id,
            Status = "Active",
        };
        DbContext.Lots.Add(lot);
        await DbContext.SaveChangesAsync();

        var animal = new Animal
        {
            Name = "Luna",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-4),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = reproductiveStatus,
        };

        DbContext.Animals.Add(animal);

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = "EDITOR",
            }
        );

        await DbContext.SaveChangesAsync();

        DbContext.ReproductiveEvents.Add(
            new ReproductiveEvent
            {
                AnimalId = animal.Id,
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 7,
                ExpectedDueDate = DateTime.SpecifyKind(
                    dueDate.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc
                ),
                CreatedByUserId = user.Id,
            }
        );

        await DbContext.SaveChangesAsync();

        return (farm, animal, user);
    }

    private static DateOnly GetSecadoTargetDueDate()
    {
        var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        var managuaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, managuaTimeZone);
        return DateOnly.FromDateTime(managuaNow).AddDays(60);
    }

    private static DateOnly GetBirthWatchWindowEnd()
    {
        var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        var managuaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, managuaTimeZone);
        return DateOnly.FromDateTime(managuaNow).AddDays(AlertConstants.BIRTH_WATCH_LEAD_DAYS);
    }
}
