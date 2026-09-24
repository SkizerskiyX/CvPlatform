using Ardalis.GuardClauses;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Domain.Entities;

/// <summary>
/// A CV is a (candidate, position) pair. Attribute values are not copied into the CV:
/// the single master value of every attribute lives in the candidate profile, and the CV
/// is rendered from the profile + position template on the fly.
/// </summary>
public sealed class Cv : BaseEntity
{
    private Cv()
    {
    }

    public Cv(Guid profileId, Guid positionId)
    {
        ProfileId = Guard.Against.Default(profileId);
        PositionId = Guard.Against.Default(positionId);
        Status = CvStatus.Draft;
    }

    public Guid ProfileId { get; private set; }
    public Guid PositionId { get; private set; }
    public CvStatus Status { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public UserProfile? Profile { get; private set; }
    public Position? Position { get; private set; }

    public void Publish(int missingAttributeCount)
    {
        if (missingAttributeCount > 0)
        {
            throw new InvalidOperationException($"CV cannot be published: {missingAttributeCount} attribute(s) are not filled out.");
        }

        Status = CvStatus.Published;
        PublishedAt = DateTime.UtcNow;
        RefreshUpdatedAt();
    }

    public void Unpublish()
    {
        Status = CvStatus.Draft;
        PublishedAt = null;
        RefreshUpdatedAt();
    }
}
