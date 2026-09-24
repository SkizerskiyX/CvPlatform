using CvPlatform.Application.Abstractions;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Domain.Exceptions;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Queries;

public sealed class CvQueries(AppDbContext db, IAttributeDefinitionRepository definitions) : ICvQueries
{
    public async Task<CvDocumentDto> DocumentAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var cv = await db.Cvs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"CV '{id}' does not exist.");
        var isOwner = actor.IsOwner(cv.ProfileId);
        if (!actor.IsAdmin && !isOwner && !(actor.IsStaff && cv.Status == CvStatus.Published))
        {
            throw new ForbiddenException("This CV is not available.");
        }

        var position = await db.Positions.AsNoTracking()
            .Include(x => x.PositionAttributes).ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.AccessRules).ThenInclude(x => x.AttributeDefinition)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == cv.PositionId, cancellationToken);
        var profile = await db.UserProfiles.AsNoTracking().Include(x => x.AttributeValues).FirstAsync(x => x.Id == cv.ProfileId, cancellationToken);

        if (!actor.IsAdmin && !CvService.HasAccess(profile, position))
        {
            // The candidate lost access to the position: the CV is kept but hidden.
            throw new ForbiddenException("The position is no longer available for this candidate, so the CV is hidden.");
        }

        var builtIn = await definitions.GetBuiltInAsync(cancellationToken);
        var projects = await db.Projects.AsNoTracking().Where(x => x.ProfileId == cv.ProfileId).ToListAsync(cancellationToken);
        var composed = CvComposer.Compose(profile, position, builtIn, projects);

        var likeCount = await db.Likes.CountAsync(x => x.CvId == id, cancellationToken);
        var likedByMe = actor.ProfileId is { } me && await db.Likes.AnyAsync(x => x.CvId == id && x.RecruiterProfileId == me, cancellationToken);

        return new CvDocumentDto(
            cv.Id,
            cv.Status,
            cv.PublishedAt,
            cv.UpdatedAt,
            position.Id,
            position.Title,
            position.Company,
            position.Level,
            profile.Id,
            profile.DisplayName,
            profile.Version,
            actor.CanEditProfile(cv.ProfileId),
            actor.IsStaff && cv.Status == CvStatus.Published,
            likedByMe,
            likeCount,
            composed.MissingCount,
            composed.Header,
            composed.Sections,
            composed.Projects);
    }

    public async Task<IReadOnlyList<CvListItemDto>> ListByProfileAsync(Actor actor, Guid profileId, CancellationToken cancellationToken)
    {
        if (!actor.CanEditProfile(profileId) && !actor.IsStaff)
        {
            throw new ForbiddenException();
        }

        var query = db.Cvs.AsNoTracking().Where(x => x.ProfileId == profileId);
        if (!actor.CanEditProfile(profileId))
        {
            query = query.Where(x => x.Status == CvStatus.Published);
        }

        var rows = await query.OrderByDescending(x => x.UpdatedAt).Project(db).Take(200).ToListAsync(cancellationToken);
        return await CvRows.ToVisibleDtosAsync(db, actor, rows, cancellationToken);
    }
}

public sealed class ProfileQueries(AppDbContext db, IAttributeDefinitionRepository definitions, ICvQueries cvQueries) : IProfileQueries
{
    public async Task<ProfileEditorDto> EditorAsync(Actor actor, Guid profileId, CancellationToken cancellationToken)
    {
        actor.RequireCanEditProfile(profileId);
        var profile = await db.UserProfiles.AsNoTracking().Include(x => x.AttributeValues).FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new NotFoundException($"Profile '{profileId}' does not exist.");

        var builtIn = await definitions.GetBuiltInAsync(cancellationToken);
        var infoIds = profile.AttributeValues.Select(x => x.AttributeDefinitionId).ToArray();
        var info = await db.AttributeDefinitions.AsNoTracking()
            .Where(x => infoIds.Contains(x.Id) && x.SystemKey == null)
            .OrderBy(x => x.Category!.Name).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var values = profile.AttributeValues.ToDictionary(x => x.AttributeDefinitionId);
        var views = builtIn.Concat(info)
            .Select(definition => AttributeValues.View(definition, AttributeValues.Read(profile, definition, values)))
            .ToArray();

        var projects = await db.Projects.AsNoTracking().Where(x => x.ProfileId == profileId).OrderByDescending(x => x.PeriodStart).ToListAsync(cancellationToken);
        var cvs = await cvQueries.ListByProfileAsync(actor, profileId, cancellationToken);

        return new ProfileEditorDto(
            profile.Id,
            profile.DisplayName,
            profile.Version,
            actor.IsOwner(profileId),
            builtIn.Select(x => x.ToDto()).ToArray(),
            info.Select(x => x.ToDto()).ToArray(),
            views,
            projects.Select(x => x.ToDto()).ToArray(),
            cvs);
    }

    public async Task<PublicProfileDto> PublicAsync(Actor actor, Guid profileId, CancellationToken cancellationToken)
    {
        if (!actor.IsStaff && !actor.CanEditProfile(profileId))
        {
            throw new ForbiddenException();
        }

        var profile = await db.UserProfiles.AsNoTracking()
            .Where(x => x.Id == profileId)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Location, x.PhotoUrl })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Profile '{profileId}' does not exist.");

        var cvs = (await cvQueries.ListByProfileAsync(actor, profileId, cancellationToken))
            .Where(x => x.Status == CvStatus.Published)
            .ToArray();
        return new PublicProfileDto(profile.Id, $"{profile.FirstName} {profile.LastName}".Trim(), profile.Location, profile.PhotoUrl, cvs);
    }
}
