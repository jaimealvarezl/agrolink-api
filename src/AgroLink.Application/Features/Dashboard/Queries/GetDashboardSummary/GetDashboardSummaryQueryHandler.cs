using AgroLink.Application.Features.Dashboard.DTOs;
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

        var allChecklists =
            lotIds.Count > 0
                ? (
                    await checklistRepository.FindAsync(
                        c => lotIds.Contains(c.LotId),
                        cancellationToken
                    )
                ).ToList()
                : new List<Checklist>();

        var mostRecentChecklist = allChecklists.MaxBy(c => c.CreatedAt);

        DateTime? lastChecklistDate = null;
        var lastChecklistIssueCount = 0;
        var novedadCount = 0;

        if (mostRecentChecklist != null)
        {
            lastChecklistDate = mostRecentChecklist.CreatedAt;

            var sessionItems = (
                await checklistItemRepository.FindAsync(
                    ci => ci.ChecklistId == mostRecentChecklist.Id,
                    cancellationToken
                )
            ).ToList();

            lastChecklistIssueCount = sessionItems.Count(i => i.Condition != "OK");
            novedadCount = sessionItems.Count(i => i.Present && i.Condition != "OK");
        }

        var cutoff = DateTime.UtcNow.AddDays(-7);
        var now = DateTime.UtcNow;

        var latestByLot = allChecklists
            .GroupBy(c => c.LotId)
            .ToDictionary(g => g.Key, g => g.Max(c => c.CreatedAt));

        var overdueLots = lots.Select(l =>
                (
                    Lot: l,
                    LastDate: latestByLot.ContainsKey(l.Id) ? (DateTime?)latestByLot[l.Id] : null
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
