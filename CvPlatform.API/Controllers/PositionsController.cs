using CvPlatform.API.Security;
using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using CvPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class PositionsController(IPositionQueries queries, IPositionService service) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public Task<IReadOnlyList<PositionListItemDto>> List([FromQuery] string? tag, [FromQuery] PositionLevel? level, CancellationToken cancellationToken) => queries.ListAsync(User.ToActor(), new PositionFilter(tag, level), cancellationToken);
    [HttpGet("latest"), AllowAnonymous]
    public Task<IReadOnlyList<PositionListItemDto>> Latest([FromQuery] int count = 10, CancellationToken cancellationToken = default) => queries.LatestAsync(User.ToActor(), count, cancellationToken);
    [HttpGet("popular"), AllowAnonymous]
    public Task<IReadOnlyList<PositionListItemDto>> Popular([FromQuery] int count = 5, CancellationToken cancellationToken = default) => queries.PopularAsync(User.ToActor(), count, cancellationToken);
    [HttpGet("{id:guid}"), AllowAnonymous]
    public Task<PositionDetailsDto> Get(Guid id, CancellationToken cancellationToken) => queries.DetailsAsync(User.ToActor(), id, cancellationToken);
    [HttpGet("{id:guid}/cvs"), Authorize(Roles = RoleNames.StaffRoles)]
    public Task<IReadOnlyList<CvListItemDto>> Cvs(Guid id, CancellationToken cancellationToken) => queries.CvsAsync(User.ToActor(), id, cancellationToken);
    [HttpPost, Authorize(Roles = RoleNames.StaffRoles)]
    public Task<VersionResponse> Create(SavePositionRequest request, CancellationToken cancellationToken) => service.CreateAsync(User.ToActor(), request, cancellationToken);
    [HttpPut("{id:guid}"), Authorize(Roles = RoleNames.StaffRoles)]
    public Task<VersionResponse> Save(Guid id, SavePositionRequest request, CancellationToken cancellationToken) => service.SaveAsync(User.ToActor(), id, request, cancellationToken);
    [HttpPost("{id:guid}/duplicate"), Authorize(Roles = RoleNames.StaffRoles)]
    public Task<VersionResponse> Duplicate(Guid id, CancellationToken cancellationToken) => service.DuplicateAsync(User.ToActor(), id, cancellationToken);
    [HttpDelete, Authorize(Roles = RoleNames.StaffRoles)]
    public async Task<IActionResult> Delete(IdsRequest request, CancellationToken cancellationToken) { await service.DeleteAsync(User.ToActor(), request.Ids, cancellationToken); return NoContent(); }
}
