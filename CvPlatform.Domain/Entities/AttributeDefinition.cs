using Ardalis.GuardClauses;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Domain.Entities;

public sealed class AttributeDefinition : BaseEntity
{
    private readonly List<AttributeOption> _options = [];

    private AttributeDefinition()
    {
    }

    public AttributeDefinition(string name, string? description, AttributeDataType dataType, Guid categoryId, string? systemKey = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name).Trim();
        Description = description;
        DataType = dataType;
        CategoryId = Guard.Against.Default(categoryId);
        SystemKey = systemKey;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AttributeDataType DataType { get; private set; }
    public Guid CategoryId { get; private set; }
    public AttributeCategory? Category { get; private set; }

    /// <summary>Non-null for built-in ("Me") attributes that cannot be removed.</summary>
    public string? SystemKey { get; private set; }

    public bool IsBuiltIn => SystemKey is not null;
    public IReadOnlyCollection<AttributeOption> Options => _options.AsReadOnly();

    public void Update(string name, string? description, AttributeDataType dataType, Guid categoryId)
    {
        if (IsBuiltIn && dataType != DataType)
        {
            throw new InvalidOperationException("The data type of a built-in attribute cannot be changed.");
        }

        Name = Guard.Against.NullOrWhiteSpace(name).Trim();
        Description = description;
        DataType = dataType;
        CategoryId = Guard.Against.Default(categoryId);
        if (DataType != AttributeDataType.Dropdown)
        {
            _options.Clear();
        }

        RefreshUpdatedAt();
    }

    public void EnsureCanBeDeleted()
    {
        if (IsBuiltIn)
        {
            throw new InvalidOperationException($"Built-in attribute '{Name}' cannot be deleted.");
        }
    }

    /// <summary>
    /// Synchronizes dropdown options by value: keeps existing ones (so stored selections stay valid),
    /// adds new ones and removes missing ones.
    /// </summary>
    public void SetOptions(IReadOnlyList<string> values)
    {
        if (DataType != AttributeDataType.Dropdown)
        {
            _options.Clear();
            return;
        }

        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalized.Count == 0)
        {
            throw new InvalidOperationException("Dropdown attribute requires at least one option.");
        }

        _options.RemoveAll(option => !normalized.Contains(option.Value, StringComparer.OrdinalIgnoreCase));
        for (var index = 0; index < normalized.Count; index++)
        {
            var existing = _options.FirstOrDefault(x => string.Equals(x.Value, normalized[index], StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                _options.Add(new AttributeOption(Id, normalized[index], index));
            }
            else
            {
                existing.ChangeOrder(index);
            }
        }

        RefreshUpdatedAt();
    }
}
