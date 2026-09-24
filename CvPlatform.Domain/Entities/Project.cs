using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public sealed class Project : BaseEntity
{
    private List<string> _tags = [];

    private Project()
    {
    }

    /// <summary>
    /// Projects are managed as separate entities (not through the profile aggregate) so that
    /// editing projects does not bump the profile version used by the auto-save.
    /// </summary>
    public Project(Guid profileId, string name, DateTime periodStart, DateTime? periodEnd, string? description, IEnumerable<string> tags)
    {
        ProfileId = Guard.Against.Default(profileId);
        Apply(name, periodStart, periodEnd, description);
        SetTags(tags);
    }

    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime PeriodStart { get; private set; }
    public DateTime? PeriodEnd { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<string> Tags => _tags;
    public UserProfile? Profile { get; private set; }

    public void Update(string name, DateTime periodStart, DateTime? periodEnd, string? description, IEnumerable<string> tags)
    {
        Apply(name, periodStart, periodEnd, description);
        SetTags(tags);
        RefreshUpdatedAt();
    }

    public void SetTags(IEnumerable<string> tags) => _tags = NormalizeTags(tags);

    public static List<string> NormalizeTags(IEnumerable<string> tags) => tags
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim().ToLowerInvariant())
        .Where(x => x.Length <= 50)
        .Distinct()
        .Take(30)
        .ToList();

    private void Apply(string name, DateTime periodStart, DateTime? periodEnd, string? description)
    {
        if (periodEnd.HasValue && periodEnd.Value < periodStart)
        {
            throw new InvalidOperationException("Project end date must not be before its start date.");
        }

        Name = Guard.Against.NullOrWhiteSpace(name).Trim();
        PeriodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
        PeriodEnd = periodEnd.HasValue ? DateTime.SpecifyKind(periodEnd.Value.Date, DateTimeKind.Utc) : null;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
    }
}
