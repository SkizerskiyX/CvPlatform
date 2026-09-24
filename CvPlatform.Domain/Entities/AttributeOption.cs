using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class AttributeOption : BaseEntity
{
    private AttributeOption()
    {
    }

    internal AttributeOption(Guid attributeDefinitionId, string value, int displayOrder)
    {
        AttributeDefinitionId = attributeDefinitionId;
        Value = Guard.Against.NullOrWhiteSpace(value);
        DisplayOrder = Guard.Against.Negative(displayOrder);
    }

    public Guid AttributeDefinitionId { get; private set; }
    public string Value { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public AttributeDefinition? AttributeDefinition { get; private set; }

    internal void ChangeOrder(int displayOrder) => DisplayOrder = Guard.Against.Negative(displayOrder);
}
