using System.Text.RegularExpressions;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Queries;

internal static partial class FullText
{
    /// <summary>
    /// Converts free user input into a safe prefix tsquery: "senior dot" -> "senior:* &amp; dot:*".
    /// Only letters and digits survive, so no tsquery syntax can be injected.
    /// </summary>
    public static string? ToPrefixQuery(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var tokens = TokenRegex().Matches(input.ToLowerInvariant()).Select(x => x.Value).Distinct().Take(8).ToArray();
        return tokens.Length == 0 ? null : string.Join(" & ", tokens.Select(x => x + ":*"));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TokenRegex();
}

internal sealed record CvRow(
    Guid Id,
    Guid PositionId,
    string PositionTitle,
    Guid ProfileId,
    string FirstName,
    string LastName,
    CvStatus Status,
    int LikeCount,
    DateTime UpdatedAt);

internal static class CvRows
{
    public static IQueryable<CvRow> Project(this IQueryable<Cv> query, AppDbContext db) => query.Select(cv => new CvRow(
        cv.Id,
        cv.PositionId,
        cv.Position!.Title,
        cv.ProfileId,
        cv.Profile!.FirstName,
        cv.Profile!.LastName,
        cv.Status,
        db.Likes.Count(like => like.CvId == cv.Id),
        cv.UpdatedAt));

    public static CvListItemDto ToDto(this CvRow row, bool accessible) => new(
        row.Id,
        row.PositionId,
        row.PositionTitle,
        row.ProfileId,
        $"{row.FirstName} {row.LastName}".Trim(),
        row.Status,
        row.LikeCount,
        row.UpdatedAt,
        accessible);

    /// <summary>
    /// Applies CV visibility for the actor. Hidden when the candidate lost access to the position:
    /// admins still see such CVs (flagged), everyone else does not. Recruiters see published CVs only.
    /// </summary>
    public static async Task<IReadOnlyList<CvListItemDto>> ToVisibleDtosAsync(AppDbContext db, Actor actor, IReadOnlyList<CvRow> rows, CancellationToken cancellationToken)
    {
        var accessible = await AccessBatch.AccessiblePairsAsync(db, rows.Select(x => (x.ProfileId, x.PositionId)).ToArray(), cancellationToken);
        return rows
            .Select(row => (row, ok: accessible.Contains((row.ProfileId, row.PositionId))))
            .Where(x => actor.IsAdmin || (x.ok && (actor.IsOwner(x.row.ProfileId) || x.row.Status == CvStatus.Published)))
            .Select(x => x.row.ToDto(x.ok))
            .ToArray();
    }
}

internal static class AccessBatch
{
    /// <summary>
    /// Evaluates access rules for many (profile, position) pairs with a constant number of queries.
    /// </summary>
    public static async Task<HashSet<(Guid ProfileId, Guid PositionId)>> AccessiblePairsAsync(
        AppDbContext db,
        IReadOnlyCollection<(Guid ProfileId, Guid PositionId)> pairs,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<(Guid, Guid)>();
        if (pairs.Count == 0)
        {
            return result;
        }

        var positionIds = pairs.Select(x => x.PositionId).Distinct().ToArray();
        var publicIds = (await db.Positions.AsNoTracking()
            .Where(x => positionIds.Contains(x.Id) && x.IsPublic)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var restrictedPairs = pairs.Where(x => !publicIds.Contains(x.PositionId)).Distinct().ToArray();
        foreach (var pair in pairs.Where(x => publicIds.Contains(x.PositionId)))
        {
            result.Add(pair);
        }

        if (restrictedPairs.Length == 0)
        {
            return result;
        }

        var restrictedIds = restrictedPairs.Select(x => x.PositionId).Distinct().ToArray();
        var rules = await db.AccessRules.AsNoTracking()
            .Include(x => x.AttributeDefinition)
            .Where(x => restrictedIds.Contains(x.PositionId))
            .ToListAsync(cancellationToken);
        var profileIds = restrictedPairs.Select(x => x.ProfileId).Distinct().ToArray();
        var ruleDefinitionIds = rules.Select(x => x.AttributeDefinitionId).Distinct().ToArray();

        var profiles = await db.UserProfiles.AsNoTracking()
            .Where(x => profileIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var values = (await db.ProfileAttributeValues.AsNoTracking()
                .Where(x => profileIds.Contains(x.ProfileId) && ruleDefinitionIds.Contains(x.AttributeDefinitionId))
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.ProfileId)
            .ToDictionary(x => x.Key, x => (IReadOnlyDictionary<Guid, ProfileAttributeValue>)x.ToDictionary(v => v.AttributeDefinitionId));

        var definitions = rules.Where(x => x.AttributeDefinition is not null).Select(x => x.AttributeDefinition!).DistinctBy(x => x.Id).ToDictionary(x => x.Id);
        var rulesByPosition = rules.ToLookup(x => x.PositionId);
        var noValues = new Dictionary<Guid, ProfileAttributeValue>();

        foreach (var pair in restrictedPairs)
        {
            if (!profiles.TryGetValue(pair.ProfileId, out var profile))
            {
                continue;
            }

            var profileValues = values.GetValueOrDefault(pair.ProfileId) ?? noValues;
            if (AccessRuleEvaluator.HasAccess(false, rulesByPosition[pair.PositionId], definitions, definition => AttributeValues.Read(profile, definition, profileValues)))
            {
                result.Add(pair);
            }
        }

        return result;
    }
}
