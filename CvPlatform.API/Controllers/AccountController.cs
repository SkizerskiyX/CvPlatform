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

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IProfileService profiles, ITokenService tokens, IConfiguration configuration) : ControllerBase
{
    public sealed record Credentials(string Email, string Password);
    public sealed record AuthResponse(string Token, string Email, Guid ProfileId);

    [HttpPost("register"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(Credentials request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        await users.AddToRoleAsync(user, RoleNames.Candidate);
        var profile = await profiles.EnsureProfileAsync(user.Id, request.Email, string.Empty, cancellationToken);
        return Ok(new AuthResponse(tokens.CreateToken(user.Id, request.Email, profile.Id, [RoleNames.Candidate]), request.Email, profile.Id));
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

    [HttpGet("external-login"), AllowAnonymous]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl)
    {
        if (!new[] { "Google", "Facebook" }.Contains(provider, StringComparer.Ordinal)) return BadRequest(new { message = "Unsupported provider." });
        var redirect = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
        return Challenge(signIn.ConfigureExternalAuthenticationProperties(provider, redirect), provider);
    }

    [HttpGet("external-login-callback"), AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info is null) return Redirect(Frontend("/login?error=external"));
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? $"{info.LoginProvider}-{info.ProviderKey}@local";
        var user = await users.FindByEmailAsync(email);
        if (user is null) { user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true }; await users.CreateAsync(user); await users.AddToRoleAsync(user, RoleNames.Candidate); await users.AddLoginAsync(user, info); }
        var profile = await profiles.EnsureProfileAsync(user.Id, email, string.Empty, cancellationToken);
        var roles = await users.GetRolesAsync(user);
        return Redirect(Frontend($"/oauth-callback#token={Uri.EscapeDataString(tokens.CreateToken(user.Id, email, profile.Id, roles))}"));
    }

    [HttpPost("logout"), Authorize]
    public IActionResult Logout() => NoContent();
    private string Frontend(string path) => (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/') + path;
}
