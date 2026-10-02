using System.Net.Http.Headers;
using CvPlatform.Application.Security;
using CvPlatform.Infrastructure.Integrations.Odoo;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.API.Controllers;

[ApiController]
[Route("api/integrations/odoo")]
public sealed class OdooIntegrationController(AppDbContext db, PositionExportService exports) : ControllerBase
{
    [Authorize(Roles = RoleNames.StaffRoles)]
    [HttpGet("positions/{id:guid}/token")]
    public async Task<IActionResult> TokenStatus(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!await db.Positions.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound(new { message = "Position does not exist." });
        var generatedAt = await db.PositionExportTokens.Where(x => x.PositionId == id).Select(x => (DateTime?)x.GeneratedAt).SingleOrDefaultAsync(cancellationToken);
        return Ok(new { active = generatedAt.HasValue, generatedAt });
    }

    [Authorize(Roles = RoleNames.StaffRoles)]
    [HttpPost("positions/{id:guid}/token")]
    public async Task<IActionResult> GenerateToken(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!await db.Positions.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound(new { message = "Position does not exist." });
        return Ok(new { token = await exports.GenerateTokenAsync(id, cancellationToken), endpoint = "/api/integrations/odoo/position" });
    }

    [Authorize(Roles = RoleNames.StaffRoles)]
    [HttpDelete("positions/{id:guid}/token")]
    public async Task<IActionResult> RevokeToken(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Positions.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound(new { message = "Position does not exist." });
        await db.PositionExportTokens.Where(x => x.PositionId == id).ExecuteDeleteAsync(cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("position")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authorization)
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter))
            return Unauthorized(new { message = "Provide a position API token in the Authorization: Bearer header." });
        var result = await exports.ExportAsync(authorization.Parameter, cancellationToken);
        return result is null ? Unauthorized(new { message = "The position API token is invalid or has been revoked." }) : Ok(result);
    }
}
