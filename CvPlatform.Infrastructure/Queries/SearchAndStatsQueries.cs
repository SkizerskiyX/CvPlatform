using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CvPlatform.Infrastructure.Queries;

/// <summary>Full-text search over PostgreSQL tsvector columns (GIN-indexed).</summary>
public sealed class SearchQueries(AppDbContext db, PositionQueries positionQueries) : ISearchQueries
{
    private const int MaxResults = 50;

    public async Task<SearchResultDto> SearchAsync(Actor actor, string? query, string? tag, CancellationToken cancellationToken)
    {
        var tsQuery = FullText.ToPrefixQuery(query);
        var normalizedTag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToLowerInvariant();
        if (tsQuery is null && normalizedTag is null)
        {
            return new SearchResultDto(query ?? string.Empty, [], []);
        }

        var positions = db.Positions.AsNoTracking();
        if (tsQuery is not null)
        {
            positions = positions.Where(x => EF.Property<NpgsqlTsVector>(x, AppDbContext.SearchVector)
                .Matches(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery)));
        }

        if (normalizedTag is not null)
        {
            positions = positions.Where(x => x.ProjectTags.Contains(normalizedTag));
        }

        var positionResults = await positionQueries.VisibleAsync(
            actor,
            tsQuery is null
                ? positions.OrderByDescending(x => x.UpdatedAt)
                : positions.OrderByDescending(x => EF.Property<NpgsqlTsVector>(x, AppDbContext.SearchVector)
                    .Rank(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery))),
            MaxResults,
            cancellationToken);

        IReadOnlyList<CvListItemDto> cvResults = [];
        if (actor.IsStaff)
        {
            var cvs = db.Cvs.AsNoTracking();
            if (!actor.IsAdmin)
            {
                cvs = cvs.Where(x => x.Status == CvStatus.Published);
            }

            if (tsQuery is not null)
            {
                cvs = cvs.Where(cv =>
                    EF.Property<NpgsqlTsVector>(cv.Profile!, AppDbContext.SearchVector).Matches(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery))
                    || EF.Property<NpgsqlTsVector>(cv.Position!, AppDbContext.SearchVector).Matches(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery))
                    || db.Projects.Any(p => p.ProfileId == cv.ProfileId
                        && EF.Property<NpgsqlTsVector>(p, AppDbContext.SearchVector).Matches(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery)))
                    || db.ProfileAttributeValues.Any(v => v.ProfileId == cv.ProfileId
                        && db.PositionAttributes.Any(pa => pa.PositionId == cv.PositionId && pa.AttributeDefinitionId == v.AttributeDefinitionId)
                        && EF.Property<NpgsqlTsVector>(v, AppDbContext.SearchVector).Matches(EF.Functions.ToTsQuery(AppDbContext.TextSearchConfig, tsQuery))));
            }

            if (normalizedTag is not null)
            {
                cvs = cvs.Where(cv => db.Projects.Any(p => p.ProfileId == cv.ProfileId && p.Tags.Contains(normalizedTag)));
            }

            var rows = await cvs.OrderByDescending(x => x.UpdatedAt).Project(db).Take(MaxResults).ToListAsync(cancellationToken);
            cvResults = await CvRows.ToVisibleDtosAsync(db, actor, rows, cancellationToken);
        }

        return new SearchResultDto(query ?? normalizedTag ?? string.Empty, positionResults, cvResults);
    }
}

public sealed class StatsQueries(AppDbContext db) : IStatsQueries
{
    public async Task<StatsDto> StatsAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var roleCounts = await db.UserRoles.AsNoTracking()
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .Where(name => name == RoleNames.Candidate || name == RoleNames.Recruiter)
            .GroupBy(name => name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new StatsDto(
            await db.Cvs.CountAsync(x => x.CreatedAt >= since, cancellationToken),
            await db.Positions.CountAsync(cancellationToken),
            roleCounts.FirstOrDefault(x => x.Role == RoleNames.Candidate)?.Count ?? 0,
            roleCounts.FirstOrDefault(x => x.Role == RoleNames.Recruiter)?.Count ?? 0,
            await db.Cvs.CountAsync(x => x.Status == CvStatus.Published, cancellationToken));
    }

    public async Task<IReadOnlyList<TagCountDto>> TagCloudAsync(int take, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 100);
        var rows = await db.Database.SqlQuery<TagRow>($"""
            SELECT t.tag AS "Tag", COUNT(*)::int AS "Count"
            FROM (
                SELECT unnest(p."Tags") AS tag FROM "Projects" p
                UNION ALL
                SELECT unnest(pos."ProjectTags") AS tag FROM "Positions" pos
            ) t
            GROUP BY t.tag
            ORDER BY 2 DESC, 1
            LIMIT {limit}
            """).ToListAsync(cancellationToken);
        return rows.Select(x => new TagCountDto(x.Tag, x.Count)).ToArray();
    }

    public async Task<IReadOnlyList<string>> TagSuggestionsAsync(string? prefix, int take, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 50);
        var pattern = AttributeDefinitionRepository.EscapeLike((prefix ?? string.Empty).Trim().ToLowerInvariant()) + "%";
        return await db.Database.SqlQuery<string>($"""
            SELECT DISTINCT t.tag AS "Value"
            FROM (
                SELECT unnest(p."Tags") AS tag FROM "Projects" p
                UNION
                SELECT unnest(pos."ProjectTags") AS tag FROM "Positions" pos
            ) t
            WHERE t.tag LIKE {pattern}
            ORDER BY 1
            LIMIT {limit}
            """).ToListAsync(cancellationToken);
    }

    private sealed class TagRow
    {
        public string Tag { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
