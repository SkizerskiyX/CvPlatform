using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class DiscussionPost : BaseEntity
{
    private DiscussionPost()
    {
    }

    public DiscussionPost(Guid positionId, Guid authorProfileId, string content)
    {
        PositionId = Guard.Against.Default(positionId);
        AuthorProfileId = Guard.Against.Default(authorProfileId);
        Content = Guard.Against.NullOrWhiteSpace(content).Trim();
    }

    public Guid PositionId { get; private set; }
    public Guid AuthorProfileId { get; private set; }
    public string Content { get; private set; } = null!;
    public Position? Position { get; private set; }
    public UserProfile? Author { get; private set; }
}
