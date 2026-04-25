using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgroLink.Infrastructure.Data.Interceptors;

public class SearchTextInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        SetSearchText(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        SetSearchText(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetSearchText(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<Animal>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var a = entry.Entity;
            var parts = new[] { a.Name, a.TagVisual, a.Cuia }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => EntityResolutionService.Normalize(s!));

            a.SearchText = string.Join(" ", parts);
        }

        foreach (var entry in context.ChangeTracker.Entries<Lot>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            entry.Entity.SearchText = EntityResolutionService.Normalize(entry.Entity.Name);
        }
    }
}
