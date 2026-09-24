using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Domain.Exceptions;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Queries;

public sealed class PositionQueries(AppDbContext db) : IPositionQueries
{
    private const int MaxRows = 500;

    public Task<IReadOnlyList<PositionListItemDto>> ListAsync(Actor actor, PositionFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Positions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            var tag = filter.Tag.Trim().ToLowerInvariant();
            query = query.Where(x => x.ProjectTags.Contains(tag));
        }

        if (filter.Level.HasValue)
        {
            query = query.Where(x => x.Level == filter.Level);
        }

        return VisibleAsync(actor, query.OrderByDescending(x => x.UpdatedAt), MaxRows, cancellationToken);
    }

    public Task<IReadOnlyList<PositionListItemDto>> LatestAsync(Actor actor, int count, CancellationToken cancellationToken) =>
        VisibleAsync(actor, db.Positions.AsNoTracking().OrderByDescending(x => x.UpdatedAt), Math.Clamp(count, 1, 50), cancellationToken);

    public Task<IReadOnlyList<PositionListItemDto>> PopularAsync(Actor actor, int count, CancellationToken cancellationToken) =>
        VisibleAsync(
            actor,
            db.Positions.AsNoTracking()
                .OrderByDescending(x => db.Cvs.Count(cv => cv.PositionId == x.Id && cv.Status == CvStatus.Published))
                .ThenByDescending(x => x.UpdatedAt),
            Math.Clamp(count, 1, 50),
            cancellationToken);

    public async Task<PositionDetailsDto> DetailsAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var position = await db.Positions.AsNoTracking()
            .Include(x => x.PositionAttributes).ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.AccessRules).ThenInclude(x => x.AttributeDefinition)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Position '{id}' does not exist.");

        if (!actor.IsAuthenticated && !position.IsPublic)
        {
            throw new ForbiddenException("Sign in to view this position.");
        }

        var canApply = false;
        Guid? myCvId = null;
        if (actor.ProfileId is { } profileId)
        {
            var profile = await db.UserProfiles.AsNoTracking().Include(x => x.AttributeValues).FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            canApply = profile is not null && (actor.IsAdmin || CvService.HasAccess(profile, position));
            myCvId = await db.Cvs.Where(x => x.ProfileId == profileId && x.PositionId == id).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        }

        return new PositionDetailsDto(
            position.Id,
            position.Title,
            position.ShortDescription,
            position.Company,
            position.Level,
            position.IsPublic,
            position.MaxProjects,
            position.ProjectTags.ToArray(),
            position.PositionAttributes
                .Where(x => x.AttributeDefinition is not null)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new PositionAttributeDto(x.AttributeDefinitionId, x.AttributeDefinition!.Name, x.AttributeDefinition.DataType, x.AttributeDefinition.Category?.Name, x.DisplayOrder))
                .ToArray(),
            position.AccessRules
                .Where(x => x.AttributeDefinition is not null)
                .Select(x => new AccessRuleDto(
                    x.Id,
                    x.AttributeDefinitionId,
                    x.AttributeDefinition!.Name,
                    x.AttributeDefinition.DataType,
                    x.Operator,
                    x.ComparisonValue,
                    AccessRuleEvaluator.DisplayValue(x.AttributeDefinition, x.ComparisonValue)))
                .ToArray(),
            position.Version,
            position.CreatedAt,
            position.UpdatedAt,
            canApply,
            myCvId);
    }

    public async Task<IReadOnlyList<CvListItemDto>> CvsAsync(Actor actor, Guid positionId, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        var query = db.Cvs.AsNoTracking().Where(x => x.PositionId == positionId);
        if (!actor.IsAdmin)
        {
            query = query.Where(x => x.Status == CvStatus.Published);
        }

        var rows = await query.OrderByDescending(x => x.UpdatedAt).Project(db).Take(MaxRows).ToListAsync(cancellationToken);
        return await CvRows.ToVisibleDtosAsync(db, actor, rows, cancellationToken);
    }

    /// <summary>Projects positions into list rows and applies role-based visibility.</summary>
    internal async Task<IReadOnlyList<PositionListItemDto>> VisibleAsync(Actor actor, IQueryable<Position> ordered, int take, CancellationToken cancellationToken)
    {
        if (!actor.IsAuthenticated)
        {
            ordered = ordered.Where(x => x.IsPublic);
        }

        var candidateOnly = actor.IsAuthenticated && !actor.IsStaff;
        var rows = await ordered
            .Take(candidateOnly ? MaxRows : take)
            .Select(x => new PositionListItemDto(
                x.Id,
                x.Title,
                x.Company,
                x.Level,
                x.IsPublic,
                x.PositionAttributes.Count,
                db.Cvs.Count(cv => cv.PositionId == x.Id && cv.Status == CvStatus.Published),
                x.ProjectTags.ToList(),
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        if (!candidateOnly)
        {
            return rows;
        }

        var profileId = actor.ProfileId!.Value;
        var accessible = await AccessBatch.AccessiblePairsAsync(db, rows.Select(x => (profileId, x.Id)).ToArray(), cancellationToken);
        return rows.Where(x => accessible.Contains((profileId, x.Id))).Take(take).ToArray();
    }
}
