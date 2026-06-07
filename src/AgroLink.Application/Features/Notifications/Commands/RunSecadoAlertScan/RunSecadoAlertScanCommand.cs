using System.Globalization;
using AgroLink.Application.Features.Notifications.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Notifications.Commands.RunSecadoAlertScan;

public record RunSecadoAlertScanCommand : IRequest<SecadoScanSummaryDto>;

public class RunSecadoAlertScanCommandHandler(
    IRepository<ReproductiveEvent> reproductiveEventRepository,
    IRepository<Animal> animalRepository,
    IRepository<Lot> lotRepository,
    IRepository<Paddock> paddockRepository,
    IDeviceTokenRepository deviceTokenRepository,
    ISentNotificationRepository sentNotificationRepository,
    IPushNotificationSender pushNotificationSender,
    IUnitOfWork unitOfWork
) : IRequestHandler<RunSecadoAlertScanCommand, SecadoScanSummaryDto>
{
    private sealed record Candidate(
        int AnimalId,
        string AnimalName,
        DateOnly ExpectedDueDate,
        int FarmId
    );

    public async Task<SecadoScanSummaryDto> Handle(
        RunSecadoAlertScanCommand request,
        CancellationToken cancellationToken
    )
    {
        var scannedAt = DateTime.UtcNow;
        var managuaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        var managuaNow = TimeZoneInfo.ConvertTimeFromUtc(scannedAt, managuaTimeZone);
        var targetDate = DateOnly.FromDateTime(managuaNow).AddDays(AlertConstants.SECADO_LEAD_DAYS);

        var targetStartUtc = DateTime.SpecifyKind(
            targetDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc
        );
        var targetEndUtc = targetStartUtc.AddDays(1);

        var rawEvents = await reproductiveEventRepository.FindAsync(
            e =>
                e.EventType == ReproductiveEventType.PregnancyCheck
                && e.Status == ReproductiveEventStatus.Positive
                && e.ExpectedDueDate.HasValue
                && e.ExpectedDueDate.Value >= targetStartUtc
                && e.ExpectedDueDate.Value < targetEndUtc
                && e.Animal.ReproductiveStatus == ReproductiveStatus.Pregnant,
            cancellationToken
        );

        var latestByAnimal = rawEvents
            .GroupBy(e => e.AnimalId)
            .Select(g => g.OrderByDescending(x => x.Date).First())
            .ToList();

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

            candidates.Add(
                new Candidate(
                    ev.AnimalId,
                    animal.Name,
                    DateOnly.FromDateTime(ev.ExpectedDueDate.Value),
                    paddock.FarmId
                )
            );
        }

        var sent = 0;
        var skipped = 0;
        var prunedTokens = 0;

        foreach (var candidate in candidates)
        {
            var alreadySent = await sentNotificationRepository.ExistsAsync(
                candidate.AnimalId,
                NotificationType.SecadoDryOff,
                candidate.ExpectedDueDate,
                cancellationToken
            );

            if (alreadySent)
            {
                skipped++;
                continue;
            }

            var tokens = await deviceTokenRepository.GetTokensByFarmAsync(
                candidate.FarmId,
                cancellationToken
            );

            if (tokens.Count > 0)
            {
                var body =
                    $"🔔 Secado pendiente: {candidate.AnimalName} — parto esperado en {AlertConstants.SECADO_LEAD_DAYS} días. Suspender ordeño hoy.";

                var result = await pushNotificationSender.SendAsync(
                    tokens,
                    "🔔 Secado pendiente",
                    body,
                    new Dictionary<string, string>
                    {
                        ["type"] = "secado",
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
                    prunedTokens++;
                }

                sent++;
            }

            await sentNotificationRepository.AddAsync(
                new SentNotification
                {
                    AnimalId = candidate.AnimalId,
                    NotificationType = NotificationType.SecadoDryOff,
                    ExpectedDueDate = candidate.ExpectedDueDate,
                    SentAt = DateTime.UtcNow,
                },
                cancellationToken
            );
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SecadoScanSummaryDto(
            scannedAt,
            targetDate,
            candidates.Count,
            sent,
            skipped,
            prunedTokens
        );
    }
}
