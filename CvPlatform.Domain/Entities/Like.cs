using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class Like : BaseEntity
{
    private Like()
    {
    }

    public Like(Guid cvId, Guid recruiterProfileId)
    {
        CvId = Guard.Against.Default(cvId);
        RecruiterProfileId = Guard.Against.Default(recruiterProfileId);
    }

    public Guid CvId { get; private set; }
    public Guid RecruiterProfileId { get; private set; }
    public Cv? Cv { get; private set; }
}
