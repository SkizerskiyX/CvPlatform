using Ardalis.GuardClauses;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Domain.Entities;

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
