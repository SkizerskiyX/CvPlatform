using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Application.Abstractions;

// Read models. Implemented in Infrastructure with projections and batched queries
// (no entity-per-row loading, no queries inside loops).

public sealed record PositionFilter(string? Tag, PositionLevel? Level);

public interface IPositionQueries
{
    /// <summary>Positions visible to the actor: staff see all, candidates see accessible ones, guests see public ones.</summary>
    Task<IReadOnlyList<PositionListItemDto>> ListAsync(Actor actor, PositionFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<PositionListItemDto>> LatestAsync(Actor actor, int count, CancellationToken cancellationToken);
    Task<IReadOnlyList<PositionListItemDto>> PopularAsync(Actor actor, int count, CancellationToken cancellationToken);
    Task<PositionDetailsDto> DetailsAsync(Actor actor, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CvListItemDto>> CvsAsync(Actor actor, Guid positionId, CancellationToken cancellationToken);
}

public interface ICvQueries
{
    Task<CvDocumentDto> DocumentAsync(Actor actor, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CvListItemDto>> ListByProfileAsync(Actor actor, Guid profileId, CancellationToken cancellationToken);
}

public interface IProfileQueries
{
    Task<ProfileEditorDto> EditorAsync(Actor actor, Guid profileId, CancellationToken cancellationToken);
    Task<PublicProfileDto> PublicAsync(Actor actor, Guid profileId, CancellationToken cancellationToken);
}

public interface ISearchQueries
{
    Task<SearchResultDto> SearchAsync(Actor actor, string? query, string? tag, CancellationToken cancellationToken);
}

public interface IStatsQueries
{
    Task<StatsDto> StatsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TagCountDto>> TagCloudAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> TagSuggestionsAsync(string? prefix, int take, CancellationToken cancellationToken);
}
