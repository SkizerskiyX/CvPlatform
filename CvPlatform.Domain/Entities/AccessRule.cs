using Ardalis.GuardClauses;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Domain.Entities;

public sealed class AccessRule : BaseEntity
{
    private AccessRule()
    {
    }

    internal AccessRule(Guid positionId, Guid attributeDefinitionId, ComparisonOperator comparisonOperator, string comparisonValue)
    {
        PositionId = Guard.Against.Default(positionId);
        AttributeDefinitionId = Guard.Against.Default(attributeDefinitionId);
        Operator = comparisonOperator;
        ComparisonValue = Guard.Against.NullOrWhiteSpace(comparisonValue).Trim();
    }

    public Guid PositionId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }
    public ComparisonOperator Operator { get; private set; }
    public string ComparisonValue { get; private set; } = null!;
    public Position? Position { get; private set; }
    public AttributeDefinition? AttributeDefinition { get; private set; }
}
