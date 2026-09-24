using Ardalis.GuardClauses;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Domain.Entities;

public sealed record PositionRuleSpec(AttributeDefinition Definition, ComparisonOperator Operator, string ComparisonValue);

public sealed class Position : BaseEntity
{
    public const int MaxProjectsLimit = 20;

    private readonly List<PositionAttribute> _positionAttributes = [];
    private readonly List<AccessRule> _accessRules = [];
    private List<string> _projectTags = [];

    private Position()
    {
    }

    public Position(string title, string? shortDescription, bool isPublic)
    {
        Title = Guard.Against.NullOrWhiteSpace(title).Trim();
        ShortDescription = shortDescription;
        IsPublic = isPublic;
        MaxProjects = 3;
    }

    public string Title { get; private set; } = null!;
    public string? ShortDescription { get; private set; }
    public string? Company { get; private set; }
    public PositionLevel? Level { get; private set; }

    /// <summary>Public positions are accessible to all authenticated users; otherwise access rules apply.</summary>
    public bool IsPublic { get; private set; }

    public int MaxProjects { get; private set; }
    public IReadOnlyList<string> ProjectTags => _projectTags;
    public IReadOnlyCollection<PositionAttribute> PositionAttributes => _positionAttributes.AsReadOnly();
    public IReadOnlyCollection<AccessRule> AccessRules => _accessRules.AsReadOnly();

    public void UpdateBasics(string title, string? shortDescription, string? company, PositionLevel? level, bool isPublic, int maxProjects, IEnumerable<string> projectTags)
    {
        Title = Guard.Against.NullOrWhiteSpace(title).Trim();
        ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
        Level = level;
        IsPublic = isPublic;
        MaxProjects = Guard.Against.OutOfRange(maxProjects, nameof(maxProjects), 0, MaxProjectsLimit);
        _projectTags = Project.NormalizeTags(projectTags);
        RefreshUpdatedAt();
    }

    /// <summary>Replaces the template attributes; list order is the display order.</summary>
    public void SetAttributes(IReadOnlyList<Guid> attributeDefinitionIds)
    {
        if (attributeDefinitionIds.Distinct().Count() != attributeDefinitionIds.Count)
        {
            throw new InvalidOperationException("An attribute can be added to a position only once.");
        }

        _positionAttributes.RemoveAll(x => !attributeDefinitionIds.Contains(x.AttributeDefinitionId));
        for (var index = 0; index < attributeDefinitionIds.Count; index++)
        {
            var id = attributeDefinitionIds[index];
            var current = _positionAttributes.FirstOrDefault(x => x.AttributeDefinitionId == id);
            if (current is null)
            {
                _positionAttributes.Add(new PositionAttribute(Id, id, index));
            }
            else
            {
                current.ChangeOrder(index);
            }
        }

        RefreshUpdatedAt();
    }

    public void SetAccessRules(IEnumerable<PositionRuleSpec> rules)
    {
        _accessRules.Clear();
        foreach (var rule in rules)
        {
            if (!IsOperatorCompatible(rule.Definition.DataType, rule.Operator))
            {
                throw new InvalidOperationException($"Operator {rule.Operator} is not compatible with {rule.Definition.DataType} attribute '{rule.Definition.Name}'.");
            }

            _accessRules.Add(new AccessRule(Id, rule.Definition.Id, rule.Operator, rule.ComparisonValue));
        }

        RefreshUpdatedAt();
    }

    public Position Duplicate(string copySuffix = " (copy)")
    {
        var copy = new Position(Title + copySuffix, ShortDescription, IsPublic)
        {
            Company = Company,
            Level = Level,
            MaxProjects = MaxProjects,
            _projectTags = [.. _projectTags]
        };

        foreach (var attribute in _positionAttributes.OrderBy(x => x.DisplayOrder))
        {
            copy._positionAttributes.Add(new PositionAttribute(copy.Id, attribute.AttributeDefinitionId, attribute.DisplayOrder));
        }

        foreach (var rule in _accessRules)
        {
            copy._accessRules.Add(new AccessRule(copy.Id, rule.AttributeDefinitionId, rule.Operator, rule.ComparisonValue));
        }

        return copy;
    }

    public static bool IsOperatorCompatible(AttributeDataType dataType, ComparisonOperator comparisonOperator) => dataType switch
    {
        AttributeDataType.Boolean => comparisonOperator == ComparisonOperator.EqualTo,
        AttributeDataType.Numeric or AttributeDataType.Date or AttributeDataType.Period => comparisonOperator != ComparisonOperator.In,
        AttributeDataType.Dropdown => comparisonOperator is ComparisonOperator.EqualTo or ComparisonOperator.NotEquals or ComparisonOperator.In,
        AttributeDataType.Image => false,
        _ => comparisonOperator is ComparisonOperator.EqualTo or ComparisonOperator.NotEquals or ComparisonOperator.In
    };
}
