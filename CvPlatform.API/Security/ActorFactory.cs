using System.Security.Claims;
using CvPlatform.Application.Security;

namespace CvPlatform.API.Security;

internal static class ActorFactory
{
    public static Actor ToActor(this ClaimsPrincipal user)
    {
        var profileClaim = user.FindFirst("profileId")?.Value;
        var profileId = Guid.TryParse(profileClaim, out var parsed) ? parsed : (Guid?)null;
        return new Actor(profileId, user.IsInRole(RoleNames.Admin), user.IsInRole(RoleNames.Recruiter));
    }
}
