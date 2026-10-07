using CvPlatform.Infrastructure.Integrations.Salesforce;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using CvPlatform.API.Security;
using CvPlatform.Domain.Exceptions;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesforceController : ControllerBase
{
    private readonly SalesforceService _salesforceService;

    public SalesforceController(SalesforceService salesforceService)
    {
        _salesforceService = salesforceService;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(
        CancellationToken cancellationToken)
    {
        var result = await _salesforceService.GetAccountsAsync(
            cancellationToken
        );

        return Content(result, "application/json");
    }

    [Authorize]
    [HttpGet("profiles/{profileId:guid}")]
    public async Task<IActionResult> Profile(Guid profileId, [FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        if (!User.ToActor().CanEditProfile(profileId)) return Forbid();
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new NotFoundException("Profile does not exist.");
        var email = await db.Users.Where(x => x.Id == profile.IdentityUserId).Select(x => x.Email).SingleAsync(cancellationToken);
        return Ok(new { profile.FirstName, profile.LastName, email, profile.Location, profile.PhotoUrl,
            connected = profile.SalesforceContactId != null, profile.SalesforceConnectedAt });
    }

    [Authorize]
    [HttpPost("profiles/{profileId:guid}")]
    public async Task<IActionResult> Connect(Guid profileId, SalesforceProfileRequest request, [FromServices] AppDbContext db, CancellationToken cancellationToken)
    {
        if (!User.ToActor().CanEditProfile(profileId)) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var profile = await db.UserProfiles.FromSqlInterpolated($"SELECT *, xmin FROM \"UserProfiles\" WHERE \"Id\" = {profileId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Profile does not exist.");
        if (string.IsNullOrWhiteSpace(profile.LastName) || profile.FirstName.Length > 40 || profile.LastName.Length > 80)
            return BadRequest(new { message = "Save your last name in the profile first. Salesforce allows up to 40 characters for first name and 80 for last name." });
        var email = await db.Users.Where(x => x.Id == profile.IdentityUserId).Select(x => x.Email).SingleAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { message = "An account email is required." });
        (string AccountId, string ContactId) ids;
        try
        {
            ids = await _salesforceService.CreateProfileAsync(profile, email, request.Organization.Trim(), request.Phone?.Trim(), request.Notes?.Trim(), request.NewsletterConsent, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = exception.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Salesforce is unavailable. Try again later." });
        }
        profile.ConnectSalesforce(ids.AccountId, ids.ContactId);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { connected = true, profile.SalesforceConnectedAt });
    }
}

public sealed record SalesforceProfileRequest(
    [Required, StringLength(255, MinimumLength = 1)] string Organization,
    [StringLength(40)] string? Phone,
    [StringLength(2000)] string? Notes,
    bool NewsletterConsent);
