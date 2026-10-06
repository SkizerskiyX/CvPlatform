using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using CvPlatform.API.Security;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Identity;
using CvPlatform.Infrastructure.Integrations.Support;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CvPlatform.API.Controllers;

[ApiController, Authorize, Route("api/support")]
public sealed class SupportController(
    AppDbContext db, UserManager<ApplicationUser> users, DropboxTicketUploader uploader,
    IOptions<SupportOptions> options) : ControllerBase
{
    [HttpPost("tickets"), EnableRateLimiting("support")]
    public async Task<IActionResult> Create(SupportTicketRequest request, CancellationToken cancellationToken)
    {
        if (request.TicketId == Guid.Empty || string.IsNullOrWhiteSpace(request.Summary)
            || request.Priority is not ("High" or "Average" or "Low"))
            return BadRequest(new { message = "Provide a summary, ticket ID and valid priority." });
        if (!request.PagePath.StartsWith('/') || request.PagePath.StartsWith("//")
            || request.PagePath.Contains('\\') || request.PagePath.Contains('?')
            || request.PagePath.Contains('#') || request.PagePath.Any(char.IsControl))
            return BadRequest(new { message = "Provide a local page path without query parameters." });
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (!await uploader.IsConfiguredAsync(cancellationToken))
            return StatusCode(503, new { message = "Support upload is not configured. Contact an administrator." });
        var admins = await users.GetUsersInRoleAsync("Admin");
        var emails = admins.Where(x => !string.IsNullOrWhiteSpace(x.Email) && !(x.LockoutEnd > DateTimeOffset.UtcNow))
            .Select(x => x.Email!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (emails.Length == 0)
            return StatusCode(503, new { message = "No administrator email is configured for support." });
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == user.Id, cancellationToken);
        var segments = request.PagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Guid? positionId = null;
        if (segments.Length >= 2 && Guid.TryParse(segments[1], out var resourceId))
        {
            if (segments[0] == "positions") positionId = resourceId;
            if (segments[0] == "cvs")
                positionId = await db.Cvs.Where(x => x.Id == resourceId && (x.Status == CvStatus.Published
                    || x.ProfileId == User.ToActor().ProfileId || User.IsInRole("Admin") || User.IsInRole("Recruiter")))
                    .Select(x => (Guid?)x.PositionId).SingleOrDefaultAsync(cancellationToken);
        }
        var title = positionId is { } id
            ? await db.Positions.Where(x => x.Id == id).Select(x => x.Title).SingleOrDefaultAsync(cancellationToken) : null;
        var roles = await users.GetRolesAsync(user);
        var name = profile is null ? user.Email : $"{profile.FirstName} {profile.LastName}".Trim();
        var json = JsonSerializer.Serialize(new SupportTicketDocument(
            request.TicketId, DateTimeOffset.UtcNow, request.Summary.Trim(),
            $"{name} <{user.Email}> ({string.Join(", ", roles)})", title,
            options.Value.PublicOrigin.TrimEnd('/') + request.PagePath, request.Priority, emails));
        try { await uploader.UploadAsync(request.TicketId, user.Id, json, cancellationToken); }
        catch (InvalidOperationException e) { return StatusCode(502, new { message = e.Message }); }
        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        { return StatusCode(502, new { message = "Support storage is unavailable. Retry with the same ticket." }); }
        return Ok(new { ticketId = request.TicketId, uploaded = true });
    }
}

public sealed record SupportTicketRequest(
    Guid TicketId,
    [Required, StringLength(2000, MinimumLength = 1)] string Summary,
    [Required, StringLength(7)] string Priority,
    [Required, StringLength(2048)] string PagePath);

public sealed record SupportTicketDocument(
    [property: JsonPropertyName("Ticket ID")] Guid TicketId,
    [property: JsonPropertyName("Created at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("Summary")] string Summary,
    [property: JsonPropertyName("Reported by")] string ReportedBy,
    [property: JsonPropertyName("Position")] string? Position,
    [property: JsonPropertyName("Link")] string Link,
    [property: JsonPropertyName("Priority")] string Priority,
    [property: JsonPropertyName("admins")] string[] AdminEmails);
