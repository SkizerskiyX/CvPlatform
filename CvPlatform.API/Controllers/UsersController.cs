using System.Security.Claims;
using CvPlatform.Application.Security;
using CvPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = RoleNames.Admin)]
public sealed class UsersController(IUserAdministration users) : ControllerBase
{
    public sealed record RoleRequest(List<string> Ids, string Role);
    public sealed record UserIdsRequest(List<string> Ids);

    [HttpGet]
    public Task<IReadOnlyList<UserAdminDto>> List([FromQuery] string? search, CancellationToken cancellationToken) => users.ListAsync(search, cancellationToken);
    [HttpPost("block")]
    public async Task<IActionResult> Block(UserIdsRequest request, CancellationToken cancellationToken) { await users.BlockAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, request.Ids, cancellationToken); return NoContent(); }
    [HttpPost("unblock")]
    public async Task<IActionResult> Unblock(UserIdsRequest request, CancellationToken cancellationToken) { await users.UnblockAsync(request.Ids, cancellationToken); return NoContent(); }
    [HttpPost("roles")]
    public async Task<IActionResult> AddRole(RoleRequest request, CancellationToken cancellationToken) { await users.AddRoleAsync(request.Ids, request.Role, cancellationToken); return NoContent(); }
    [HttpDelete("roles")]
    public async Task<IActionResult> RemoveRole(RoleRequest request, CancellationToken cancellationToken) { await users.RemoveRoleAsync(request.Ids, request.Role, cancellationToken); return NoContent(); }
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] List<string> ids, CancellationToken cancellationToken) { await users.DeleteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, ids, cancellationToken); return NoContent(); }
}
