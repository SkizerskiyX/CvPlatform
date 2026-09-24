using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class AttributeCategory : BaseEntity
{
    private AttributeCategory()
    {
    }

    public AttributeCategory(string name)
    {
        Name = Guard.Against.NullOrWhiteSpace(name);
    }

    public string Name { get; private set; } = null!;
}
