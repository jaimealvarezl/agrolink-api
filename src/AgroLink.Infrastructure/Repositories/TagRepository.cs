using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgroLink.Infrastructure.Repositories;

public class TagRepository(AgroLinkDbContext context) : Repository<Tag>(context), ITagRepository
{
    public async Task<Tag> UpsertAsync(
        int farmId,
        string displayName,
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Tag display name is required.");
        }

        var canonicalName = displayName.ToLowerInvariant();

        var existing = await _dbSet.FirstOrDefaultAsync(
            t => t.FarmId == farmId && t.CanonicalName == canonicalName,
            cancellationToken
        );
        if (existing != null)
        {
            return existing;
        }

        var tag = new Tag
        {
            FarmId = farmId,
            CanonicalName = canonicalName,
            DisplayName = displayName,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        await _dbSet.AddAsync(tag, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return tag;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            _context.Entry(tag).State = EntityState.Detached;

            return await _dbSet.FirstAsync(
                t => t.FarmId == farmId && t.CanonicalName == canonicalName,
                cancellationToken
            );
        }
    }

    public async Task<List<Tag>> GetByFarmAsync(
        int farmId,
        string? search = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbSet.Include(t => t.AnimalTags).Where(t => t.FarmId == farmId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.DisplayName.ToLower().Contains(searchTerm)
                || t.CanonicalName.ToLower().Contains(searchTerm)
            );
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Tag>> GetByCanonicalNamesAsync(
        int farmId,
        IEnumerable<string> canonicalNames,
        CancellationToken cancellationToken = default
    )
    {
        var canonicalList = canonicalNames.ToList();
        if (canonicalList.Count == 0)
        {
            return [];
        }

        return await _dbSet
            .Where(t => t.FarmId == farmId && canonicalList.Contains(t.CanonicalName))
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> RenameAsync(
        int id,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        var tag = await _dbSet
            .Include(t => t.AnimalTags)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag == null)
        {
            return null;
        }

        tag.DisplayName = displayName;
        _dbSet.Update(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<Tag?> UpdateColorAsync(
        int id,
        string? colorToken,
        CancellationToken cancellationToken = default
    )
    {
        var tag = await _dbSet
            .Include(t => t.AnimalTags)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag == null)
        {
            return null;
        }

        tag.ColorToken = string.IsNullOrWhiteSpace(colorToken) ? null : colorToken.Trim();
        _dbSet.Update(tag);
        await _context.SaveChangesAsync(cancellationToken);

        return tag;
    }

    public async Task<(Tag? Tag, int AffectedAnimals)> DeleteWithCascadeAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var tag = await _dbSet.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag == null)
        {
            return (null, 0);
        }

        var linkedAnimalIds = await _context
            .AnimalTags.Where(at => at.TagId == id)
            .Select(at => at.AnimalId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var links = await _context
            .AnimalTags.Where(at => at.TagId == id)
            .ToListAsync(cancellationToken);
        _context.AnimalTags.RemoveRange(links);

        _dbSet.Remove(tag);

        await _context.SaveChangesAsync(cancellationToken);

        return (tag, linkedAnimalIds.Count);
    }
}
