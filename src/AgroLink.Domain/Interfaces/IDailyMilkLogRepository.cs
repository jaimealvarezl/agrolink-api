using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IDailyMilkLogRepository
{
    Task<DailyMilkLog?> FindByDateAsync(int farmId, DateOnly date, CancellationToken ct = default);

    Task<(IEnumerable<DailyMilkLog> Items, int TotalCount)> GetPagedByDateRangeAsync(
        int farmId,
        DateOnly from,
        DateOnly to,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task<decimal?> FindLastPricePerLiterAsync(int farmId, CancellationToken ct = default);
    Task AddAsync(DailyMilkLog entity, CancellationToken ct = default);
    void Update(DailyMilkLog entity);
}
