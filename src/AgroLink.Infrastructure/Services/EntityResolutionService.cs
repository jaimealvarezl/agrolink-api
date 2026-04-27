using System.Text.RegularExpressions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
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
        string[]? ownerMentions = null,
        CancellationToken ct = default
    )
    {
        // Sequential to avoid concurrent DbContext operations on the same scoped instance
        var animal =
            animalMention != null ? await ResolveAnimalAsync(farmId, animalMention, ct) : null;
        var lot = lotMention != null ? await ResolveLotAsync(farmId, lotMention, ct) : null;
        var paddock =
            targetPaddockMention != null
                ? await ResolvePaddockAsync(farmId, targetPaddockMention, ct)
                : null;
        var mother =
            motherMention != null ? await ResolveAnimalAsync(farmId, motherMention, ct) : null;
        var owners = ownerMentions is { Length: > 0 }
            ? await ResolveOwnersAsync(farmId, ownerMentions, ct)
            : null;

        return new EntityResolutionResult(animal, lot, paddock, mother, owners);
    }

    private async Task<Animal?> ResolveAnimalAsync(int farmId, string mention, CancellationToken ct)
    {
        var norm = Normalize(mention);
        if (string.IsNullOrEmpty(norm))
        {
            return null;
        }

        // Tier 1: exact normalized match on SearchText
        var exact = await context
            .Animals.Include(a => a.Lot)
            .Where(a =>
                a.Lot.Paddock.FarmId == farmId
                && a.LifeStatus == LifeStatus.Active
                && a.SearchText != null
                && a.SearchText == norm
            )
            .FirstOrDefaultAsync(ct);

        if (exact != null)
        {
            return exact;
        }

        // Tier 2: ILIKE containment on SearchText
        try
        {
            var ilike = await context
                .Animals.Include(a => a.Lot)
                .Where(a =>
                    a.Lot.Paddock.FarmId == farmId
                    && a.LifeStatus == LifeStatus.Active
                    && a.SearchText != null
                    && EF.Functions.ILike(a.SearchText, $"%{norm}%")
                )
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (ilike != null)
            {
                return ilike;
            }
        }
        catch (InvalidOperationException)
        {
            // In-memory provider does not support ILike — fall through
        }

        return null;
    }

    private async Task<Lot?> ResolveLotAsync(int farmId, string mention, CancellationToken ct)
    {
        var norm = Normalize(mention);
        if (string.IsNullOrEmpty(norm))
        {
            return null;
        }

        // Tier 1: exact normalized match on SearchText
        var exact = await context
            .Lots.Include(l => l.Paddock)
            .Where(l =>
                l.Paddock.FarmId == farmId
                && l.Status == "ACTIVE"
                && l.SearchText != null
                && l.SearchText == norm
            )
            .FirstOrDefaultAsync(ct);

        if (exact != null)
        {
            return exact;
        }

        // Tier 2: ILIKE containment on SearchText
        try
        {
            var ilike = await context
                .Lots.Include(l => l.Paddock)
                .Where(l =>
                    l.Paddock.FarmId == farmId
                    && l.Status == "ACTIVE"
                    && l.SearchText != null
                    && EF.Functions.ILike(l.SearchText, $"%{norm}%")
                )
                .FirstOrDefaultAsync(ct);

            if (ilike != null)
            {
                return ilike;
            }
        }
        catch (InvalidOperationException) { }

        return null;
    }

    private async Task<Paddock?> ResolvePaddockAsync(
        int farmId,
        string mention,
        CancellationToken ct
    )
    {
        var norm = Normalize(mention);
        if (string.IsNullOrEmpty(norm))
        {
            return null;
        }

        var paddocks = await context.Paddocks.Where(p => p.FarmId == farmId).ToListAsync(ct);

        // Tier 1: exact normalized match
        var exact = paddocks.FirstOrDefault(p => Normalize(p.Name) == norm);
        if (exact != null)
        {
            return exact;
        }

        // Tier 2: normalized containment
        var contains = paddocks.FirstOrDefault(p => Normalize(p.Name).Contains(norm));
        if (contains != null)
        {
            return contains;
        }

        // Tier 3: Levenshtein (acceptable here since paddock count per farm is small)
        var threshold = Math.Max(2, norm.Length / 4);
        return paddocks
            .Select(p => new { p, Dist = Levenshtein(norm, Normalize(p.Name)) })
            .Where(x => x.Dist <= threshold)
            .MinBy(x => x.Dist)
            ?.p;
    }

    private async Task<IReadOnlyList<Owner>?> ResolveOwnersAsync(
        int farmId,
        string[] mentions,
        CancellationToken ct
    )
    {
        var owners = await context
            .Owners.Where(o => o.FarmId == farmId && o.IsActive)
            .ToListAsync(ct);

        var resolved = new List<Owner>(mentions.Length);
        foreach (var mention in mentions)
        {
            var norm = Normalize(mention);
            if (string.IsNullOrEmpty(norm))
            {
                continue;
            }

            // Tier 1: exact normalized match
            var match = owners.FirstOrDefault(o => Normalize(o.Name) == norm);

            // Tier 2: normalized containment
            match ??= owners.FirstOrDefault(o =>
            {
                var ownerNorm = Normalize(o.Name);
                return ownerNorm.Contains(norm) || norm.Contains(ownerNorm);
            });

            if (match != null && resolved.All(r => r.Id != match.Id))
            {
                resolved.Add(match);
            }
        }

        return resolved.Count > 0 ? resolved : null;
    }

    // Normalize: lowercase → strip accents → strip articles → collapse whitespace
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var accents = input
            .ToLowerInvariant()
            .Replace('á', 'a')
            .Replace('é', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ú', 'u')
            .Replace('ü', 'u')
            .Replace('ñ', 'n');

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
