using System.Security.Claims;
using CvPlatform.API.Security;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using CvPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CvPlatform.Infrastructure.Persistence;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IProfileService profiles, ITokenService tokens, IConfiguration configuration, AppDbContext db) : ControllerBase
{
    public sealed record Credentials(string Email, string Password);
    public sealed record Registration(string Email, string Password, bool IsRecruiter = false);
    public sealed record AuthResponse(string Token, string Email, Guid ProfileId);

    [HttpPost("register"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(Registration request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        var role = request.IsRecruiter ? RoleNames.Recruiter : RoleNames.Candidate;
        var roleResult = await users.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded) return BadRequest(new { errors = roleResult.Errors.Select(x => x.Description) });
        var profile = await profiles.EnsureProfileAsync(user.Id, request.Email, string.Empty, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new AuthResponse(tokens.CreateToken(user.Id, request.Email, profile.Id, [role]), request.Email, profile.Id));
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(Credentials request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !(await signIn.CheckPasswordSignInAsync(user, request.Password, true)).Succeeded) return Unauthorized(new { message = "Invalid credentials." });
        var profile = await profiles.EnsureProfileAsync(user.Id, request.Email, string.Empty, cancellationToken);
        var roles = await users.GetRolesAsync(user);
        return Ok(new AuthResponse(tokens.CreateToken(user.Id, request.Email, profile.Id, roles), request.Email, profile.Id));
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (user is null) return Unauthorized();
        var profile = await profiles.EnsureProfileAsync(user.Id, user.Email ?? string.Empty, string.Empty, cancellationToken);
        return Ok(new MeDto(profile.Id, user.Email ?? string.Empty, profile.DisplayName, profile.PhotoUrl, (await users.GetRolesAsync(user)).ToArray(), null, null));
    }

    [HttpGet("external-providers"), AllowAnonymous]
    public async Task<ActionResult<string[]>> ExternalProviders() =>
        Ok((await signIn.GetExternalAuthenticationSchemesAsync()).Select(x => x.Name).ToArray());

    [HttpGet("external-login"), AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromQuery] string provider)
    {
        if (!new[] { "Google", "Facebook" }.Contains(provider, StringComparer.Ordinal)) return BadRequest(new { message = "Unsupported provider." });
        if (!(await signIn.GetExternalAuthenticationSchemesAsync()).Any(x => x.Name == provider))
            return Redirect(Frontend("/login?error=unavailable"));
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var redirect = Url.Action(nameof(ExternalLoginCallback));
        return Challenge(signIn.ConfigureExternalAuthenticationProperties(provider, redirect), provider);
    }

    [HttpGet("external-login-callback"), AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(CancellationToken cancellationToken)
    {
        if (!(await signIn.GetExternalAuthenticationSchemesAsync()).Any())
            return ExternalError("unavailable");
        Response.Headers.CacheControl = "no-store";
        try
        {
            var info = await signIn.GetExternalLoginInfoAsync();
            if (info is null) return ExternalError("external");
            // Provider ID is the identity. Email alone must never grant access to an existing account.
            var user = await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (user is null)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email)) return ExternalError("missing_email");
                if (await users.FindByEmailAsync(email) is not null) return ExternalError("account_exists");
                user = new ApplicationUser { UserName = email, Email = email };
                if (!(await users.CreateAsync(user)).Succeeded) return ExternalError("external");
                if (!(await users.AddToRoleAsync(user, RoleNames.Candidate)).Succeeded
                    || !(await users.AddLoginAsync(user, info)).Succeeded)
                    return ExternalError("external");
            }
            if (!await signIn.CanSignInAsync(user)
                || (users.SupportsUserLockout && await users.IsLockedOutAsync(user)))
                return ExternalError("locked");
            var profile = await profiles.EnsureProfileAsync(user.Id, user.Email ?? string.Empty, string.Empty, cancellationToken);
            var roles = await users.GetRolesAsync(user);
            await transaction.CommitAsync(cancellationToken);
            return Redirect(Frontend($"/oauth-callback#token={Uri.EscapeDataString(tokens.CreateToken(user.Id, user.Email ?? string.Empty, profile.Id, roles))}"));
        }
        finally
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    [HttpPost("logout"), Authorize]
    public IActionResult Logout() => NoContent();
    private IActionResult ExternalError(string code) => Redirect(Frontend("/login?error=" + code));
    private string Frontend(string path) => (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/') + path;
}
