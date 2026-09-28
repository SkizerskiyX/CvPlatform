using System.Security.Claims;
using CvPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CvPlatform.API.Security;

public static class CurrentUserValidation
{
    public static async Task Validate(TokenValidatedContext context)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identity is null || id is null) { context.Fail("Invalid identity."); return; }
        var directory = context.HttpContext.RequestServices.GetRequiredService<IUserDirectory>();
        var user = await directory.GetSnapshotAsync(id, context.HttpContext.RequestAborted);
        if (user is null || user.IsBlocked) { context.Fail("Account unavailable."); return; }
        foreach (var claim in identity.FindAll(identity.RoleClaimType).Concat(identity.FindAll("profileId")).ToArray())
            identity.RemoveClaim(claim);
        foreach (var role in user.Roles) identity.AddClaim(new Claim(identity.RoleClaimType, role));
        if (user.ProfileId is { } profileId) identity.AddClaim(new Claim("profileId", profileId.ToString()));
    }
}
