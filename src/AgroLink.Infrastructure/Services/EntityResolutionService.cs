using System.Text.RegularExpressions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Services;

public class EntityResolutionService(AgroLinkDbContext context) : IEntityResolutionService
{
    private static readonly Regex ArticleRegex = new(
        @"\b(la|el|los|las|un|una)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public async Task<EntityResolutionResult> ResolveAsync(
        int farmId,
        string? animalMention,
        string? lotMention,
        string? targetPaddockMention,
        string? motherMention,
        CancellationToken ct = default
    )
    {
        var animalTask =
            animalMention != null
                ? ResolveAnimalAsync(farmId, animalMention, ct)
                : Task.FromResult<int?>(null);

        var lotTask =
            lotMention != null
                ? ResolveLotAsync(farmId, lotMention, ct)
                : Task.FromResult<int?>(null);

        var paddockTask =
            targetPaddockMention != null
                ? ResolvePaddockAsync(farmId, targetPaddockMention, ct)
                : Task.FromResult<int?>(null);

        var motherTask =
            motherMention != null
                ? ResolveAnimalAsync(farmId, motherMention, ct)
                : Task.FromResult<int?>(null);

        await Task.WhenAll(animalTask, lotTask, paddockTask, motherTask);

        return new EntityResolutionResult(
            await animalTask,
            await lotTask,
            await paddockTask,
            await motherTask
        );
    }

    private async Task<int?> ResolveAnimalAsync(int farmId, string mention, CancellationToken ct)
    {
        var norm = Normalize(mention);

        // Tier 1: exact normalized match on SearchText
        var exact = await context
            .Animals.Where(a =>
                a.Lot.Paddock.FarmId == farmId
                && a.LifeStatus == LifeStatus.Active
                && a.SearchText != null
                && a.SearchText == norm
            )
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (exact.HasValue)
        {
            return exact;
        }

        // Tier 2: ILIKE containment on SearchText
        try
        {
            var ilike = await context
                .Animals.Where(a =>
                    a.Lot.Paddock.FarmId == farmId
                    && a.LifeStatus == LifeStatus.Active
                    && a.SearchText != null
                    && EF.Functions.ILike(a.SearchText, $"%{norm}%")
                )
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync(ct);

            if (ilike.HasValue)
            {
                return ilike;
            }
        }
        catch (InvalidOperationException)
        {
            // In-memory provider does not support ILike — fall through
        }

        // Tier 3: Levenshtein in-memory fallback
        var candidates = await context
            .Animals.Where(a => a.Lot.Paddock.FarmId == farmId && a.LifeStatus == LifeStatus.Active)
            .Select(a => new
            {
                a.Id,
                NameNorm = a.SearchText
                    ?? Normalize(a.Name + " " + (a.TagVisual ?? "") + " " + (a.Cuia ?? "")),
            })
            .Take(200)
            .ToListAsync(ct);

        var threshold = Math.Max(2, norm.Length / 4);
        var best = candidates
            .Select(c => new { c.Id, Dist = Levenshtein(norm, c.NameNorm) })
            .Where(c => c.Dist <= threshold)
            .MinBy(c => c.Dist);

        return best?.Id;
    }

    private async Task<int?> ResolveLotAsync(int farmId, string mention, CancellationToken ct)
    {
        var norm = Normalize(mention);

        var exact = await context
            .Lots.Where(l =>
                l.Paddock.FarmId == farmId
                && l.Status == "ACTIVE"
                && l.SearchText != null
                && l.SearchText == norm
            )
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(ct);

        if (exact.HasValue)
        {
            return exact;
        }

        try
        {
            var ilike = await context
                .Lots.Where(l =>
                    l.Paddock.FarmId == farmId
                    && l.Status == "ACTIVE"
                    && l.SearchText != null
                    && EF.Functions.ILike(l.SearchText, $"%{norm}%")
                )
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(ct);

            if (ilike.HasValue)
            {
                return ilike;
            }
        }
        catch (InvalidOperationException) { }

        var candidates = await context
            .Lots.Where(l => l.Paddock.FarmId == farmId && l.Status == "ACTIVE")
            .Select(l => new { l.Id, NameNorm = l.SearchText ?? Normalize(l.Name) })
            .Take(200)
            .ToListAsync(ct);

        var threshold = Math.Max(2, norm.Length / 4);
        var best = candidates
            .Select(c => new { c.Id, Dist = Levenshtein(norm, c.NameNorm) })
            .Where(c => c.Dist <= threshold)
            .MinBy(c => c.Dist);

        return best?.Id;
    }

    private async Task<int?> ResolvePaddockAsync(int farmId, string mention, CancellationToken ct)
    {
        var norm = Normalize(mention);

        var exact = await context
            .Paddocks.Where(p => p.FarmId == farmId && Normalize(p.Name) == norm)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (exact.HasValue)
        {
            return exact;
        }

        try
        {
            var ilike = await context
                .Paddocks.Where(p =>
                    p.FarmId == farmId && EF.Functions.ILike(p.Name, $"%{mention}%")
                )
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(ct);

            if (ilike.HasValue)
            {
                return ilike;
            }
        }
        catch (InvalidOperationException) { }

        var candidates = await context
            .Paddocks.Where(p => p.FarmId == farmId)
            .Select(p => new { p.Id, NameNorm = Normalize(p.Name) })
            .Take(100)
            .ToListAsync(ct);

        var threshold = Math.Max(2, norm.Length / 4);
        var best = candidates
            .Select(c => new { c.Id, Dist = Levenshtein(norm, c.NameNorm) })
            .Where(c => c.Dist <= threshold)
            .MinBy(c => c.Dist);

        return best?.Id;
    }

    // Normalize: lowercase → strip accents → strip articles → collapse whitespace
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var lower = input.ToLowerInvariant();

        var accents = lower
            .Replace('á', 'a')
            .Replace('é', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ú', 'u')
            .Replace('ü', 'u')
            .Replace('ñ', 'n')
            .Replace('Á', 'a')
            .Replace('É', 'e')
            .Replace('Í', 'i')
            .Replace('Ó', 'o')
            .Replace('Ú', 'u')
            .Replace('Ü', 'u')
            .Replace('Ñ', 'n');

        var noArticles = ArticleRegex.Replace(accents, " ");

        return string.Join(' ', noArticles.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }

        return d[a.Length, b.Length];
    }
}
