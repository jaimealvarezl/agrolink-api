using System.Globalization;
using AgroLink.Application.Features.Notifications.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.Notifications.Commands.RunBirthWatchAlertScan;

public record RunBirthWatchAlertScanCommand : IRequest<BirthWatchScanSummaryDto>;

public class RunBirthWatchAlertScanCommandHandler(
    IRepository<ReproductiveEvent> reproductiveEventRepository,
    IRepository<Animal> animalRepository,
    IRepository<Lot> lotRepository,
    IRepository<Paddock> paddockRepository,
    IDeviceTokenRepository deviceTokenRepository,
    ISentNotificationRepository sentNotificationRepository,
    IPushNotificationSender pushNotificationSender,
    IUnitOfWork unitOfWork,
    ILogger<RunBirthWatchAlertScanCommandHandler> logger
) : IRequestHandler<RunBirthWatchAlertScanCommand, BirthWatchScanSummaryDto>
{
    public async Task<BirthWatchScanSummaryDto> Handle(
        RunBirthWatchAlertScanCommand request,
        CancellationToken cancellationToken
    )
    {
        var scannedAt = DateTime.UtcNow;
        var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        var managuaNow = TimeZoneInfo.ConvertTimeFromUtc(scannedAt, managuaTimeZone);
        var todayManagua = DateOnly.FromDateTime(managuaNow);
        var windowEnd = todayManagua.AddDays(AlertConstants.BIRTH_WATCH_LEAD_DAYS);

        var todayStartUtc = DateTime.SpecifyKind(
            todayManagua.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc
        );
        var windowEndUtc = DateTime.SpecifyKind(
            todayManagua
                .AddDays(AlertConstants.BIRTH_WATCH_LEAD_DAYS + 1)
                .ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc
        );

        var rawEvents = await reproductiveEventRepository.FindAsync(
            e =>
                e.EventType == ReproductiveEventType.PregnancyCheck
                && e.Status == ReproductiveEventStatus.Positive
                && e.ExpectedDueDate.HasValue
                && e.ExpectedDueDate.Value > todayStartUtc
                && e.ExpectedDueDate.Value < windowEndUtc
                && e.Animal.ReproductiveStatus == ReproductiveStatus.Pregnant,
            cancellationToken
        );

        var latestByAnimal = rawEvents
            .GroupBy(e => e.AnimalId)
            .Select(g => g.OrderByDescending(x => x.Date).First())
            .ToList();

        if (latestByAnimal.Count == 0)
        {
            return new BirthWatchScanSummaryDto(scannedAt, windowEnd, 0, 0, 0, 0);
        }

        var animalIds = latestByAnimal.Select(e => e.AnimalId).Distinct().ToList();

        var animals = (
            await animalRepository.FindAsync(a => animalIds.Contains(a.Id), cancellationToken)
        ).ToDictionary(a => a.Id);

        var lotIds = animals.Values.Select(a => a.LotId).Distinct().ToList();
        var lots = (
            await lotRepository.FindAsync(l => lotIds.Contains(l.Id), cancellationToken)
        ).ToDictionary(l => l.Id);

        var paddockIds = lots.Values.Select(l => l.PaddockId).Distinct().ToList();
        var paddocks = (
            await paddockRepository.FindAsync(p => paddockIds.Contains(p.Id), cancellationToken)
        ).ToDictionary(p => p.Id);

        var candidates = new List<Candidate>(latestByAnimal.Count);
        foreach (var ev in latestByAnimal)
        {
            if (!ev.ExpectedDueDate.HasValue)
            {
                continue;
            }

            if (!animals.TryGetValue(ev.AnimalId, out var animal))
            {
                continue;
            }

            if (!lots.TryGetValue(animal.LotId, out var lot))
            {
                continue;
            }

            if (!paddocks.TryGetValue(lot.PaddockId, out var paddock))
            {
                continue;
            }

            var expectedDueDateOnly = DateOnly.FromDateTime(ev.ExpectedDueDate.Value);
            var daysUntilDue = (
                expectedDueDateOnly.ToDateTime(TimeOnly.MinValue)
                - todayManagua.ToDateTime(TimeOnly.MinValue)
            ).Days;

            candidates.Add(
                new Candidate(
                    ev.AnimalId,
                    animal.Name,
                    expectedDueDateOnly,
                    daysUntilDue,
                    paddock.FarmId
                )
            );
        }

        var sent = 0;
        var skipped = 0;
        var prunedTokens = 0;
        var tokensByFarm = new Dictionary<int, List<string>>();

        foreach (var candidate in candidates)
        {
            var alreadySent = await sentNotificationRepository.ExistsAsync(
                candidate.AnimalId,
                NotificationType.BirthWatch,
                candidate.ExpectedDueDate,
                cancellationToken
            );

            if (alreadySent)
            {
                skipped++;
                continue;
            }

            if (!tokensByFarm.TryGetValue(candidate.FarmId, out var tokens))
            {
                tokens = (
                    await deviceTokenRepository.GetTokensByFarmAsync(
                        candidate.FarmId,
                        cancellationToken
                    )
                ).ToList();
                tokensByFarm[candidate.FarmId] = tokens;
            }

            if (tokens.Count > 0)
            {
                var body =
                    $"🐄 Parto en {candidate.DaysUntilDue} días: {candidate.AnimalName}. Pásela a potrero de maternidad y revise ubre/condición.";

                var result = await pushNotificationSender.SendAsync(
                    tokens,
                    "🐄 Parto cercano",
                    body,
                    new Dictionary<string, string>
                    {
                        ["type"] = "birth_watch",
                        ["animalId"] = candidate.AnimalId.ToString(CultureInfo.InvariantCulture),
                        ["tab"] = "reproductive",
                    },
                    cancellationToken
                );

                foreach (var unregisteredToken in result.UnregisteredTokens)
                {
                    await deviceTokenRepository.DeleteAsync(
                        unregisteredToken,
                        0,
                        cancellationToken
                    );
                    tokens.Remove(unregisteredToken);
                    prunedTokens++;
                }

                sent++;
            }

            await sentNotificationRepository.AddAsync(
                new SentNotification
                {
                    AnimalId = candidate.AnimalId,
                    NotificationType = NotificationType.BirthWatch,
                    ExpectedDueDate = candidate.ExpectedDueDate,
                    SentAt = DateTime.UtcNow,
                },
                cancellationToken
            );
        }

        logger.LogInformation(
            "BirthWatchAlertScan: candidates={Candidates} sent={Sent} skipped={Skipped} pruned={PrunedTokens} windowEnd={WindowEnd}",
            candidates.Count,
            sent,
            skipped,
            prunedTokens,
            windowEnd
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BirthWatchScanSummaryDto(
            scannedAt,
            windowEnd,
            candidates.Count,
            sent,
            skipped,
            prunedTokens
        );
    }

    private sealed record Candidate(
        int AnimalId,
        string AnimalName,
        DateOnly ExpectedDueDate,
        int DaysUntilDue,
        int FarmId
    );
}
