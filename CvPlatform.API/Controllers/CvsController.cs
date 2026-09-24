using CvPlatform.API.Security;
using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class CvsController(ICvQueries queries, ICvService service, IProfileService profiles) : ControllerBase
{
    [HttpGet("me")]
    public Task<IReadOnlyList<CvListItemDto>> Mine(CancellationToken cancellationToken) { var actor = User.ToActor(); return queries.ListByProfileAsync(actor, actor.RequireProfileId(), cancellationToken); }
    [HttpGet("{id:guid}")]
    public Task<CvDocumentDto> Get(Guid id, CancellationToken cancellationToken) => queries.DocumentAsync(User.ToActor(), id, cancellationToken);
    [HttpPost("generate/{positionId:guid}")]
    public Task<VersionResponse> Generate(Guid positionId, CancellationToken cancellationToken) => service.GenerateAsync(User.ToActor(), positionId, cancellationToken);
    [HttpPut("{id:guid}/attributes")]
    public async Task<VersionResponse> SetAttribute(Guid id, ProfileAutosaveRequest request, CancellationToken cancellationToken) { var actor = User.ToActor(); var cv = await queries.DocumentAsync(actor, id, cancellationToken); return await profiles.AutosaveAsync(actor, cv.ProfileId, request, cancellationToken); }
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) { await service.PublishAsync(User.ToActor(), id, cancellationToken); return NoContent(); }
    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken) { await service.UnpublishAsync(User.ToActor(), id, cancellationToken); return NoContent(); }
    [HttpDelete]
    public async Task<IActionResult> Delete(IdsRequest request, CancellationToken cancellationToken) { await service.DeleteAsync(User.ToActor(), request.Ids, cancellationToken); return NoContent(); }
}
