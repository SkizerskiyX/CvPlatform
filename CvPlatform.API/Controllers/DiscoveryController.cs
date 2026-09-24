using CvPlatform.API.Security;
using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api")]
public sealed class DiscoveryController(ISearchQueries search, IStatsQueries stats) : ControllerBase
{
    [HttpGet("search"), AllowAnonymous]
    public Task<SearchResultDto> Search([FromQuery] string? q, [FromQuery] string? tag, CancellationToken cancellationToken) => search.SearchAsync(User.ToActor(), q, tag, cancellationToken);
    [HttpGet("stats"), AllowAnonymous]
    public Task<StatsDto> Stats(CancellationToken cancellationToken) => stats.StatsAsync(cancellationToken);
    [HttpGet("tags"), AllowAnonymous]
    public Task<IReadOnlyList<TagCountDto>> Tags([FromQuery] int take = 30, CancellationToken cancellationToken = default) => stats.TagCloudAsync(take, cancellationToken);
    [HttpGet("tags/suggestions"), Authorize]
    public Task<IReadOnlyList<string>> Suggestions([FromQuery] string? prefix, [FromQuery] int take = 20, CancellationToken cancellationToken = default) => stats.TagSuggestionsAsync(prefix, take, cancellationToken);
}
