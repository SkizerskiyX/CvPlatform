using CvPlatform.Application.Abstractions;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Exceptions;

namespace CvPlatform.Application.Services;

public interface ICvService
{
    Task<VersionResponse> GenerateAsync(Actor actor, Guid positionId, CancellationToken cancellationToken);
    Task PublishAsync(Actor actor, Guid cvId, CancellationToken cancellationToken);
    Task UnpublishAsync(Actor actor, Guid cvId, CancellationToken cancellationToken);
    Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> cvIds, CancellationToken cancellationToken);
}

public sealed class CvService(
    ICvRepository cvs,
    IPositionRepository positions,
    IUserProfileRepository profiles,
    IAttributeDefinitionRepository definitions,
    IUnitOfWork unitOfWork) : ICvService
{
    public async Task<VersionResponse> GenerateAsync(Actor actor, Guid positionId, CancellationToken cancellationToken)
    {
        var profileId = actor.RequireProfileId();
        var position = await positions.GetByIdWithDetailsAsync(positionId, cancellationToken)
            ?? throw new NotFoundException($"Position '{positionId}' does not exist.");
        var profile = await profiles.GetByIdAsync(profileId, cancellationToken)
            ?? throw new NotFoundException("Profile does not exist.");

        if (!actor.IsAdmin && !HasAccess(profile, position))
        {
            throw new ForbiddenException("You do not meet the access rules of this position.");
        }

        if (await cvs.ExistsAsync(profileId, positionId, cancellationToken))
        {
            throw new InvalidOperationException("You already have a CV for this position.");
        }

        var cv = new Cv(profileId, positionId);
        await cvs.AddAsync(cv, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VersionResponse(cv.Id, cv.Version);
    }

    public async Task PublishAsync(Actor actor, Guid cvId, CancellationToken cancellationToken)
    {
        var cv = await GetEditableAsync(actor, cvId, cancellationToken);
        var position = await positions.GetByIdWithDetailsAsync(cv.PositionId, cancellationToken)
            ?? throw new NotFoundException("Position does not exist.");
        var profile = await profiles.GetByIdAsync(cv.ProfileId, cancellationToken)
            ?? throw new NotFoundException("Profile does not exist.");
        var builtIn = await definitions.GetBuiltInAsync(cancellationToken);

        var composed = CvComposer.Compose(profile, position, builtIn, []);
        cv.Publish(composed.MissingCount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnpublishAsync(Actor actor, Guid cvId, CancellationToken cancellationToken)
    {
        var cv = await GetEditableAsync(actor, cvId, cancellationToken);
        cv.Unpublish();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> cvIds, CancellationToken cancellationToken)
    {
        actor.RequireProfileId();
        foreach (var cv in await cvs.GetByIdsAsync(cvIds, cancellationToken))
        {
            actor.RequireCanEditProfile(cv.ProfileId);
            cvs.Remove(cv);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public static bool HasAccess(UserProfile profile, Position position)
    {
        var values = profile.AttributeValues.ToDictionary(x => x.AttributeDefinitionId);
        var ruleDefinitions = position.AccessRules
            .Where(x => x.AttributeDefinition is not null)
            .Select(x => x.AttributeDefinition!)
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id);
        return AccessRuleEvaluator.HasAccess(position.IsPublic, position.AccessRules, ruleDefinitions, definition => AttributeValues.Read(profile, definition, values));
    }

    private async Task<Cv> GetEditableAsync(Actor actor, Guid cvId, CancellationToken cancellationToken)
    {
        var cv = await cvs.GetByIdAsync(cvId, cancellationToken)
            ?? throw new NotFoundException($"CV '{cvId}' does not exist.");
        actor.RequireCanEditProfile(cv.ProfileId);
        return cv;
    }
}
