using CvPlatform.API.Security;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class DiscussionsController(IDiscussionService service) : ControllerBase
{
    [HttpGet("position/{positionId:guid}"), Authorize]
    public Task<IReadOnlyList<DiscussionPostDto>> List(Guid positionId, [FromQuery] DateTime? after, CancellationToken cancellationToken) => service.ListAsync(positionId, after, cancellationToken);
    [HttpPost("position/{positionId:guid}"), Authorize]
    public Task<DiscussionPostDto> Add(Guid positionId, CreateDiscussionPostRequest request, CancellationToken cancellationToken) => service.AddAsync(User.ToActor(), positionId, request, cancellationToken);
}
