using CvPlatform.Domain.Enums;

namespace CvPlatform.Application.DTOs;

// ---------- Common ----------
public sealed record IdsRequest(List<Guid> Ids);
public sealed record VersionResponse(Guid Id, uint Version);

/// <summary>Typed attribute value. Used both for input and output; only the field matching the data type is meaningful.</summary>
public sealed record AttributeValueInput(
    string? StringValue,
    decimal? NumericValue,
    DateTime? DateValue,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    bool? BoolValue,
    Guid? SelectedOptionId,
    string? ImageUrl)
{
    public static readonly AttributeValueInput Empty = new(null, null, null, null, null, null, null, null);
}

/// <summary>Attribute value with a human-readable representation; <see cref="Display"/> is null when the value is empty.</summary>
public sealed record AttributeValueView(Guid AttributeDefinitionId, AttributeValueInput Value, string? Display);

// ---------- Attribute library ----------
public sealed record AttributeCategoryDto(Guid Id, string Name);
public sealed record AttributeOptionDto(Guid Id, string Value, int DisplayOrder);
public sealed record AttributeDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    AttributeDataType DataType,
    Guid CategoryId,
    string? CategoryName,
    bool IsBuiltIn,
    string? SystemKey,
    IReadOnlyList<AttributeOptionDto> Options,
    uint Version);

public sealed record SaveAttributeDefinitionRequest(
    string Name,
    string? Description,
    AttributeDataType DataType,
    Guid CategoryId,
    List<string>? DropdownOptions,
    uint? Version);

// ---------- Profiles ----------
public sealed record MeDto(
    Guid ProfileId,
    string Email,
    string DisplayName,
    string? PhotoUrl,
    IReadOnlyList<string> Roles,
    string? Language,
    string? Theme);

public sealed record ProfileEditorDto(
    Guid Id,
    string DisplayName,
    uint Version,
    bool IsOwner,
    IReadOnlyList<AttributeDefinitionDto> BuiltInDefinitions,
    IReadOnlyList<AttributeDefinitionDto> InfoDefinitions,
    IReadOnlyList<AttributeValueView> Values,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<CvListItemDto> Cvs);

public sealed record ProfileValueChange(Guid AttributeDefinitionId, AttributeValueInput Value);
public sealed record ProfileAutosaveRequest(uint Version, List<ProfileValueChange>? Changes, List<Guid>? RemovedAttributeIds);

public sealed record PublicProfileDto(
    Guid Id,
    string DisplayName,
    string? Location,
    string? PhotoUrl,
    IReadOnlyList<CvListItemDto> PublishedCvs);

public sealed record ProjectDto(Guid Id, string Name, DateTime PeriodStart, DateTime? PeriodEnd, string? Description, IReadOnlyList<string> Tags);
public sealed record SaveProjectRequest(string Name, DateTime PeriodStart, DateTime? PeriodEnd, string? Description, List<string>? Tags);

// ---------- Positions ----------
public sealed record PositionListItemDto(
    Guid Id,
    string Title,
    string? Company,
    PositionLevel? Level,
    bool IsPublic,
    int AttributeCount,
    int PublishedCvCount,
    IReadOnlyList<string> ProjectTags,
    DateTime UpdatedAt);

public sealed record PositionAttributeDto(Guid AttributeDefinitionId, string Name, AttributeDataType DataType, string? CategoryName, int DisplayOrder);
public sealed record AccessRuleDto(Guid Id, Guid AttributeDefinitionId, string AttributeName, AttributeDataType DataType, ComparisonOperator Operator, string ComparisonValue, string DisplayValue);

public sealed record PositionDetailsDto(
    Guid Id,
    string Title,
    string? ShortDescription,
    string? Company,
    PositionLevel? Level,
    bool IsPublic,
    int MaxProjects,
    IReadOnlyList<string> ProjectTags,
    IReadOnlyList<PositionAttributeDto> Attributes,
    IReadOnlyList<AccessRuleDto> AccessRules,
    uint Version,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool CanApply,
    Guid? MyCvId);

public sealed record AccessRuleInput(Guid AttributeDefinitionId, ComparisonOperator Operator, string ComparisonValue);

public sealed record SavePositionRequest(
    string Title,
    string? ShortDescription,
    string? Company,
    PositionLevel? Level,
    bool IsPublic,
    int MaxProjects,
    List<string>? ProjectTags,
    List<Guid>? AttributeIds,
    List<AccessRuleInput>? AccessRules,
    uint? Version);

// ---------- CVs ----------
public sealed record CvListItemDto(
    Guid Id,
    Guid PositionId,
    string PositionTitle,
    Guid ProfileId,
    string CandidateName,
    CvStatus Status,
    int LikeCount,
    DateTime UpdatedAt,
    bool IsAccessible);

public sealed record CvFieldDto(AttributeDefinitionDto Definition, AttributeValueView Value);
public sealed record CvSectionDto(string Title, IReadOnlyList<CvFieldDto> Fields);
public sealed record CvProjectDto(Guid Id, string Name, DateTime PeriodStart, DateTime? PeriodEnd, string? Description, IReadOnlyList<string> Tags);

public sealed record CvDocumentDto(
    Guid Id,
    CvStatus Status,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    Guid PositionId,
    string PositionTitle,
    string? PositionCompany,
    PositionLevel? PositionLevel,
    Guid ProfileId,
    string CandidateName,
    uint ProfileVersion,
    bool CanEdit,
    bool CanLike,
    bool LikedByMe,
    int LikeCount,
    int MissingCount,
    IReadOnlyList<CvFieldDto> Header,
    IReadOnlyList<CvSectionDto> Sections,
    IReadOnlyList<CvProjectDto> Projects);

// ---------- Engagement ----------
public sealed record DiscussionPostDto(Guid Id, Guid PositionId, Guid AuthorProfileId, string AuthorName, string Content, DateTime CreatedAt);
public sealed record CreateDiscussionPostRequest(string Content);
public sealed record LikeDto(Guid CvId, int LikeCount, bool LikedByMe);

// ---------- Main page / search ----------
public sealed record StatsDto(int CvsLast24Hours, int TotalPositions, int TotalCandidates, int TotalRecruiters, int TotalPublishedCvs);
public sealed record TagCountDto(string Tag, int Count);
public sealed record SearchResultDto(string Query, IReadOnlyList<PositionListItemDto> Positions, IReadOnlyList<CvListItemDto> Cvs);
