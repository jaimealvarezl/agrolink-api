using AgroLink.Application.Features.Dashboard.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler(
    IRepository<Animal> animalRepository,
    ILotRepository lotRepository,
    IChecklistRepository checklistRepository,
    IRepository<ChecklistItem> checklistItemRepository
) : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var farmId = request.FarmId;

        var herdCount = await animalRepository.CountAsync(
            a => a.Lot.Paddock.FarmId == farmId && a.LifeStatus != LifeStatus.Retired,
            cancellationToken
        );

        var sickCount = await animalRepository.CountAsync(
            a => a.Lot.Paddock.FarmId == farmId && a.HealthStatus == HealthStatus.Sick,
            cancellationToken
        );

        var lots = (
            await lotRepository.FindAsync(l => l.Paddock.FarmId == farmId, cancellationToken)
        ).ToList();

        var lotIds = lots.Select(l => l.Id).ToList();

        // One latest checklist per lot — avoids loading full checklist history
        var latestChecklists = (
            await checklistRepository.GetLatestPerLotAsync(lotIds, cancellationToken)
        ).ToList();

        // lastChecklistDate = most recent session across all lots
        var mostRecentChecklist = latestChecklists.MaxBy(c => c.CreatedAt);
        var lastChecklistDate = mostRecentChecklist?.CreatedAt;

        // Aggregate issue counts across all lots' latest sessions
        var lastChecklistIssueCount = 0;
        var novedadCount = 0;

        if (latestChecklists.Count > 0)
        {
            var latestIds = latestChecklists.Select(c => c.Id).ToList();
            var allLatestItems = (
                await checklistItemRepository.FindAsync(
                    ci => latestIds.Contains(ci.ChecklistId),
                    cancellationToken
                )
            ).ToList();

            lastChecklistIssueCount = allLatestItems.Count(i =>
                i.Condition != ChecklistItemConditions.Ok
            );
            novedadCount = allLatestItems.Count(i =>
                i.Present && i.Condition != ChecklistItemConditions.Ok
            );
        }

        var cutoff = DateTime.UtcNow.AddDays(-DashboardConstants.OverdueLotThresholdDays);
        var now = DateTime.UtcNow;

        var latestDateByLot = latestChecklists.ToDictionary(c => c.LotId, c => c.CreatedAt);

        var overdueLots = lots.Select(l =>
                (
                    Lot: l,
                    LastDate: latestDateByLot.TryGetValue(l.Id, out var value)
                        ? (DateTime?)value
                        : null
                )
            )
            .Where(x => !x.LastDate.HasValue || x.LastDate < cutoff)
            .Select(x => new OverdueLotDto
            {
                LotId = x.Lot.Id,
                LotName = x.Lot.Name,
                DaysSinceLastChecklist = x.LastDate.HasValue
                    ? (int)(now - x.LastDate.Value).TotalDays
                    : (int)(now - x.Lot.CreatedAt).TotalDays,
            })
            .ToList();

        return new DashboardSummaryDto
        {
            HerdCount = herdCount,
            SickCount = sickCount,
            NovedadCount = novedadCount,
            OverdueLots = overdueLots,
            LastChecklistDate = lastChecklistDate,
            LastChecklistIssueCount = lastChecklistIssueCount,
            MilkToday = null,
        };
    }
}
