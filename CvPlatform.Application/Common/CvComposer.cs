using CvPlatform.Application.DTOs;
using CvPlatform.Domain.Entities;

namespace CvPlatform.Application.Common;

public sealed record ComposedCv(
    IReadOnlyList<CvFieldDto> Header,
    IReadOnlyList<CvSectionDto> Sections,
    IReadOnlyList<CvProjectDto> Projects,
    int MissingCount);

/// <summary>
/// Assembles a CV from the candidate profile (built-in + library attribute values),
/// the position template (attribute list) and the candidate projects filtered by the position tags.
/// </summary>
public static class CvComposer
{
    public static ComposedCv Compose(
        UserProfile profile,
        Position position,
        IReadOnlyList<AttributeDefinition> builtInDefinitions,
        IReadOnlyCollection<Project> projects)
    {
        var values = profile.AttributeValues.ToDictionary(x => x.AttributeDefinitionId);
        CvFieldDto Field(AttributeDefinition definition) =>
            new(definition.ToDto(), AttributeValues.View(definition, AttributeValues.Read(profile, definition, values)));

        var header = builtInDefinitions.Select(Field).ToArray();
        var headerIds = builtInDefinitions.Select(x => x.Id).ToHashSet();

        var sections = position.PositionAttributes
            .Where(x => x.AttributeDefinition is not null && !headerIds.Contains(x.AttributeDefinitionId))
            .OrderBy(x => x.DisplayOrder)
            .GroupBy(x => x.AttributeDefinition!.Category?.Name ?? "Other")
            .Select(group => new CvSectionDto(group.Key, group.Select(x => Field(x.AttributeDefinition!)).ToArray()))
            .ToArray();

        var missing = header.Count(x => x.Value.Display is null)
            + sections.Sum(section => section.Fields.Count(x => x.Value.Display is null));

        return new ComposedCv(header, sections, SelectProjects(position, projects), missing);
    }

    public static IReadOnlyList<CvProjectDto> SelectProjects(Position position, IReadOnlyCollection<Project> projects)
    {
        var tags = position.ProjectTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return projects
            .Select(project => (project, score: tags.Count == 0 ? 0 : project.Tags.Count(tags.Contains)))
            .Where(x => tags.Count == 0 || x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.project.PeriodStart)
            .Take(position.MaxProjects)
            .Select(x => new CvProjectDto(x.project.Id, x.project.Name, x.project.PeriodStart, x.project.PeriodEnd, x.project.Description, x.project.Tags.ToArray()))
            .ToArray();
    }
}
