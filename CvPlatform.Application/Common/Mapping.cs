using CvPlatform.Application.DTOs;
using CvPlatform.Domain.Entities;

namespace CvPlatform.Application.Common;

public static class Mapping
{
    public static AttributeDefinitionDto ToDto(this AttributeDefinition definition) => new(
        definition.Id,
        definition.Name,
        definition.Description,
        definition.DataType,
        definition.CategoryId,
        definition.Category?.Name,
        definition.IsBuiltIn,
        definition.SystemKey,
        definition.Options.OrderBy(x => x.DisplayOrder).Select(x => new AttributeOptionDto(x.Id, x.Value, x.DisplayOrder)).ToArray(),
        definition.Version);

    public static ProjectDto ToDto(this Project project) => new(
        project.Id,
        project.Name,
        project.PeriodStart,
        project.PeriodEnd,
        project.Description,
        project.Tags.ToArray());
}
