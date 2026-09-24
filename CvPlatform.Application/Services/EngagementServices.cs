using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Domain.Exceptions;
using FluentValidation;

namespace CvPlatform.Application.Services;

public interface IDiscussionService
{
    Task<IReadOnlyList<DiscussionPostDto>> ListAsync(Guid positionId, DateTime? after, CancellationToken cancellationToken);
    Task<DiscussionPostDto> AddAsync(Actor actor, Guid positionId, CreateDiscussionPostRequest request, CancellationToken cancellationToken);
}

/// <summary>Posts are append-only and always ordered by creation time.</summary>
public sealed class DiscussionService(
    IDiscussionRepository discussions,
    IPositionRepository positions,
    IUserProfileRepository profiles,
    IUnitOfWork unitOfWork,
    IValidator<CreateDiscussionPostRequest> validator) : IDiscussionService
{
    public async Task<IReadOnlyList<DiscussionPostDto>> ListAsync(Guid positionId, DateTime? after, CancellationToken cancellationToken) =>
        (await discussions.GetByPositionAsync(positionId, after, cancellationToken))
            .Select(post => new DiscussionPostDto(post.Id, post.PositionId, post.AuthorProfileId, post.Author?.DisplayName ?? string.Empty, post.Content, post.CreatedAt))
            .ToArray();

    public async Task<DiscussionPostDto> AddAsync(Actor actor, Guid positionId, CreateDiscussionPostRequest request, CancellationToken cancellationToken)
    {
        var profileId = actor.RequireProfileId();
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        _ = await positions.GetByIdWithDetailsAsync(positionId, cancellationToken)
            ?? throw new NotFoundException($"Position '{positionId}' does not exist.");
        var author = await profiles.GetByIdAsync(profileId, cancellationToken)
            ?? throw new NotFoundException("Profile does not exist.");

        var post = new DiscussionPost(positionId, profileId, request.Content);
        await discussions.AddAsync(post, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DiscussionPostDto(post.Id, post.PositionId, profileId, author.DisplayName, post.Content, post.CreatedAt);
    }
}

public interface ILikeService
{
    Task<LikeDto> ToggleAsync(Actor actor, Guid cvId, CancellationToken cancellationToken);
}

/// <summary>Only recruiters (and admins, who can do everything recruiters can) like published CVs; one like per recruiter per CV.</summary>
public sealed class LikeService(ILikeRepository likes, ICvRepository cvs, IUnitOfWork unitOfWork) : ILikeService
{
    public async Task<LikeDto> ToggleAsync(Actor actor, Guid cvId, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        var profileId = actor.RequireProfileId();
        var cv = await cvs.GetByIdAsync(cvId, cancellationToken)
            ?? throw new NotFoundException($"CV '{cvId}' does not exist.");
        if (cv.Status != CvStatus.Published)
        {
            throw new InvalidOperationException("Only published CVs can be liked.");
        }

        var existing = await likes.GetAsync(cvId, profileId, cancellationToken);
        if (existing is null)
        {
            await likes.AddAsync(new Like(cvId, profileId), cancellationToken);
        }
        else
        {
            likes.Remove(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new LikeDto(cvId, await likes.CountByCvAsync(cvId, cancellationToken), existing is null);
    }
}
