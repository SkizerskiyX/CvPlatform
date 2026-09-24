namespace CvPlatform.Domain.Entities;

public sealed class ProfileAttributeValue : AttributeValueBase
{
    private ProfileAttributeValue()
    {
    }

    internal ProfileAttributeValue(Guid profileId, Guid attributeDefinitionId) : base(attributeDefinitionId)
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; private set; }
    public UserProfile? Profile { get; private set; }
}
