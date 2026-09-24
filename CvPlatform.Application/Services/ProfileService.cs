using CvPlatform.Application.Abstractions;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Exceptions;
using FluentValidation;

namespace CvPlatform.Application.Services;

public interface IProfileService
{
    Task<UserProfile> EnsureProfileAsync(string identityUserId, string firstName, string lastName, CancellationToken cancellationToken);

    /// <summary>
    /// Auto-save of a profile page (Me + Info sections, also used by in-place CV editing).
    /// Applies all changes atomically if <see cref="ProfileAutosaveRequest.Version"/> matches and returns the new version.
    /// </summary>
    Task<VersionResponse> AutosaveAsync(Actor actor, Guid profileId, ProfileAutosaveRequest request, CancellationToken cancellationToken);

    Task<ProjectDto> AddProjectAsync(Actor actor, Guid profileId, SaveProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectDto> UpdateProjectAsync(Actor actor, Guid profileId, Guid projectId, SaveProjectRequest request, CancellationToken cancellationToken);
    Task DeleteProjectsAsync(Actor actor, Guid profileId, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);
}

public sealed class ProfileService(
    IUserProfileRepository profiles,
    IAttributeDefinitionRepository definitions,
    IProjectRepository projects,
    IUnitOfWork unitOfWork,
    IValidator<ProfileAutosaveRequest> autosaveValidator,
    IValidator<SaveProjectRequest> projectValidator) : IProfileService
{
    public async Task<UserProfile> EnsureProfileAsync(string identityUserId, string firstName, string lastName, CancellationToken cancellationToken)
    {
        var existing = await profiles.GetByIdentityUserIdAsync(identityUserId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var profile = new UserProfile(identityUserId, firstName, lastName, null);
        await profiles.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<VersionResponse> AutosaveAsync(Actor actor, Guid profileId, ProfileAutosaveRequest request, CancellationToken cancellationToken)
    {
        actor.RequireCanEditProfile(profileId);
        await autosaveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var profile = await profiles.GetByIdAsync(profileId, cancellationToken)
            ?? throw new NotFoundException($"Profile '{profileId}' does not exist.");
        unitOfWork.ExpectVersion(profile, request.Version);

        var changes = request.Changes ?? [];
        var removed = request.RemovedAttributeIds ?? [];
        var ids = changes.Select(x => x.AttributeDefinitionId).Concat(removed).Distinct().ToArray();
        var found = (await definitions.GetByIdsAsync(ids, cancellationToken)).ToDictionary(x => x.Id);

        foreach (var id in removed)
        {
            if (found.TryGetValue(id, out var definition) && definition.IsBuiltIn)
            {
                throw new InvalidOperationException($"Built-in attribute '{definition.Name}' cannot be removed from the profile.");
            }

            profile.RemoveAttributeValue(id);
        }

        foreach (var change in changes)
        {
            if (!found.TryGetValue(change.AttributeDefinitionId, out var definition))
            {
                throw new InvalidOperationException("Attribute no longer exists in the library. Reload the page.");
            }

            AttributeValues.Write(profile, definition, change.Value);
        }

        // The profile row is the aggregate version: always bump it so that every save is version-checked.
        profile.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VersionResponse(profile.Id, profile.Version);
    }

    public async Task<ProjectDto> AddProjectAsync(Actor actor, Guid profileId, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        actor.RequireCanEditProfile(profileId);
        await projectValidator.ValidateAndThrowAsync(request, cancellationToken);
        var project = new Project(profileId, request.Name, request.PeriodStart, request.PeriodEnd, request.Description, request.Tags ?? []);
        await projects.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project.ToDto();
    }

    public async Task<ProjectDto> UpdateProjectAsync(Actor actor, Guid profileId, Guid projectId, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        actor.RequireCanEditProfile(profileId);
        await projectValidator.ValidateAndThrowAsync(request, cancellationToken);
        var project = await projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.ProfileId != profileId)
        {
            throw new NotFoundException($"Project '{projectId}' does not exist.");
        }

        project.Update(request.Name, request.PeriodStart, request.PeriodEnd, request.Description, request.Tags ?? []);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project.ToDto();
    }

    public async Task DeleteProjectsAsync(Actor actor, Guid profileId, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken)
    {
        actor.RequireCanEditProfile(profileId);
        foreach (var project in (await projects.GetByIdsAsync(projectIds, cancellationToken)).Where(x => x.ProfileId == profileId))
        {
            projects.Remove(project);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
