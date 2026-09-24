using CvPlatform.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RolesController(IRoleManagementService service) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Admin")]
    public Task<IReadOnlyList<string>> GetAll(CancellationToken cancellationToken) =>
        service.GetAllRolesAsync(cancellationToken);

    [HttpGet("user/{userId}"), Authorize(Roles = "Admin")]
    public Task<IReadOnlyList<string>> GetUserRoles(string userId) =>
        service.GetUserRolesAsync(userId);

    [HttpPost("{roleName}/user/{userId}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(string roleName, string userId)
    {
        try
        {
            await service.AssignRoleAsync(userId, roleName);
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("{roleName}/user/{userId}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Remove(string roleName, string userId)
    {
        try
        {
            await service.RemoveRoleAsync(userId, roleName);
            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
