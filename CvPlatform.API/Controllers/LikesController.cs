using CvPlatform.API.Security;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class LikesController(ILikeService service) : ControllerBase
{
    [HttpPost("{cvId:guid}/toggle"), Authorize(Roles = RoleNames.StaffRoles)]
    public Task<LikeDto> Toggle(Guid cvId, CancellationToken cancellationToken) => service.ToggleAsync(User.ToActor(), cvId, cancellationToken);
}
