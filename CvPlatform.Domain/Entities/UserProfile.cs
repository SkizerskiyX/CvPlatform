using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class UserProfile : BaseEntity
{
    private readonly List<ProfileAttributeValue> _attributeValues = [];
    private readonly List<Project> _projects = [];

    private UserProfile()
    {
    }

    public UserProfile(string identityUserId, string firstName, string lastName, string? location)
    {
        IdentityUserId = Guard.Against.NullOrWhiteSpace(identityUserId);
        FirstName = firstName?.Trim() ?? string.Empty;
        LastName = lastName?.Trim() ?? string.Empty;
        Location = location;
    }

    public string IdentityUserId { get; private set; } = null!;

    // Storage of the built-in "Me" attributes (see BuiltInAttributes). They are read and
    // written through the same attribute engine as library attributes.
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public string? PhotoUrl { get; private set; }

    public IReadOnlyCollection<ProfileAttributeValue> AttributeValues => _attributeValues.AsReadOnly();
    public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    /// <summary>Marks the aggregate root as modified so its version (xmin) is checked and bumped.</summary>
    public void Touch() => RefreshUpdatedAt();

    public string? GetBuiltIn(string key) => key switch
    {
        BuiltInAttributes.FirstName => NullIfEmpty(FirstName),
        BuiltInAttributes.LastName => NullIfEmpty(LastName),
        BuiltInAttributes.Location => Location,
        BuiltInAttributes.Photo => PhotoUrl,
        _ => throw new InvalidOperationException($"Unknown built-in attribute '{key}'.")
    };

    public void SetBuiltIn(string key, string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        switch (key)
        {
            case BuiltInAttributes.FirstName:
                FirstName = Guard.Against.StringTooLong(normalized ?? string.Empty, 200);
                break;
            case BuiltInAttributes.LastName:
                LastName = Guard.Against.StringTooLong(normalized ?? string.Empty, 200);
                break;
            case BuiltInAttributes.Location:
                Location = normalized;
                break;
            case BuiltInAttributes.Photo:
                PhotoUrl = normalized;
                break;
            default:
                throw new InvalidOperationException($"Unknown built-in attribute '{key}'.");
        }

        RefreshUpdatedAt();
    }

    public ProfileAttributeValue GetOrAddAttributeValue(Guid attributeDefinitionId)
    {
        var existing = _attributeValues.FirstOrDefault(x => x.AttributeDefinitionId == attributeDefinitionId);
        if (existing is not null)
        {
            return existing;
        }

        var created = new ProfileAttributeValue(Id, attributeDefinitionId);
        _attributeValues.Add(created);
        RefreshUpdatedAt();
        return created;
    }

    public void RemoveAttributeValue(Guid attributeDefinitionId)
    {
        if (_attributeValues.RemoveAll(x => x.AttributeDefinitionId == attributeDefinitionId) > 0)
        {
            RefreshUpdatedAt();
        }
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
