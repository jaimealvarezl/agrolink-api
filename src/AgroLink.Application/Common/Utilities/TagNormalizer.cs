using System.Text.RegularExpressions;
using AgroLink.Domain.Constants;

namespace AgroLink.Application.Common.Utilities;

public static partial class TagNormalizer
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^[\p{L}\p{N}\- ]+$")]
    private static partial Regex AllowedCharactersRegex();

    public static NormalizedTag Normalize(string rawTag)
    {
        ArgumentNullException.ThrowIfNull(rawTag);

        var normalized = rawTag.Trim();
        normalized = normalized.Trim('#').Trim();
        normalized = WhitespaceRegex().Replace(normalized, " ");

        if (normalized.Length < AnimalConstants.TagNameMinLength)
        {
            throw new ArgumentException(
                $"Tag name must be at least {AnimalConstants.TagNameMinLength} characters."
            );
        }

        if (normalized.Length > AnimalConstants.TagNameMaxLength)
        {
            throw new ArgumentException(
                $"Tag name cannot exceed {AnimalConstants.TagNameMaxLength} characters."
            );
        }

        if (!AllowedCharactersRegex().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Tag name can only contain letters, numbers, spaces, and hyphens."
            );
        }

        return new NormalizedTag(normalized.ToLowerInvariant(), normalized);
    }

    public static List<NormalizedTag> NormalizeDistinct(IEnumerable<string>? rawTags)
    {
        var normalizedTags = new List<NormalizedTag>();

        if (rawTags == null)
        {
            return normalizedTags;
        }

        foreach (var rawTag in rawTags)
        {
            var normalizedTag = Normalize(rawTag);
            if (normalizedTags.All(t => t.CanonicalName != normalizedTag.CanonicalName))
            {
                normalizedTags.Add(normalizedTag);
            }
        }

        if (normalizedTags.Count > AnimalConstants.MaxTagsPerAnimal)
        {
            throw new ArgumentException(
                $"Animals can have at most {AnimalConstants.MaxTagsPerAnimal} tags."
            );
        }

        return normalizedTags;
    }
}

public record NormalizedTag(string CanonicalName, string DisplayName);
