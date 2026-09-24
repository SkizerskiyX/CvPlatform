using Microsoft.AspNetCore.Identity;

namespace CvPlatform.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>UI language chosen by the user (e.g. "en", "ru").</summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>UI theme chosen by the user ("light" / "dark").</summary>
    public string? PreferredTheme { get; set; }
}
