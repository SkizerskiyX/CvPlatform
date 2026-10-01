using Microsoft.AspNetCore.Identity;

namespace CvPlatform.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public string? PreferredLanguage { get; set; }

    public string? PreferredTheme { get; set; }
}
