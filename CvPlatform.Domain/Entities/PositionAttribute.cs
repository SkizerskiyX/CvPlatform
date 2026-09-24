using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class PositionAttribute : BaseEntity
{
    private PositionAttribute()
    {
    }

    internal PositionAttribute(Guid positionId, Guid attributeDefinitionId, int displayOrder)
    {
        PositionId = Guard.Against.Default(positionId);
        AttributeDefinitionId = Guard.Against.Default(attributeDefinitionId);
        DisplayOrder = Guard.Against.Negative(displayOrder);
    }

    public Guid PositionId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }
    public int DisplayOrder { get; private set; }
    public Position? Position { get; private set; }
    public AttributeDefinition? AttributeDefinition { get; private set; }

    internal void ChangeOrder(int displayOrder)
    {
        if (DisplayOrder != displayOrder)
        {
            DisplayOrder = Guard.Against.Negative(displayOrder);
        }
    }
}
