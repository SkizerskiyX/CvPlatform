using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CvPlatform.Infrastructure.Identity;
using CvPlatform.Infrastructure.Integrations.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/support/dropbox")]
public sealed class DropboxConnectionController(
    DropboxConnectionStore store, IOptions<SupportOptions> options,
    IHttpClientFactory clients, UserManager<ApplicationUser> users) : ControllerBase
{
    private const string CookieName = "__Host-cv-dropbox-state";
    private string Callback => options.Value.PublicOrigin.TrimEnd('/') + "/api/support/dropbox/callback";
    private bool Ready => store.HasKey && !string.IsNullOrWhiteSpace(options.Value.AppKey)
        && !string.IsNullOrWhiteSpace(options.Value.AppSecret)
        && Uri.TryCreate(options.Value.PublicOrigin, UriKind.Absolute, out var origin)
        && origin.Scheme == "https" && origin.AbsolutePath == "/" && origin.UserInfo.Length == 0
        && origin.Query.Length == 0 && origin.Fragment.Length == 0;

    [HttpGet("status"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(new { ready = Ready, connected = Ready && await store.ReadAsync(cancellationToken) is not null, callbackUrl = Ready ? Callback : null }); }
        catch (CryptographicException) { return Ok(new { ready = Ready, connected = false, callbackUrl = Ready ? Callback : null, message = "Encryption key changed. Reconnect Dropbox." }); }
    }

    [HttpPost("connect"), Authorize(Roles = "Admin")]
    public IActionResult Connect()
    {
        if (!Ready) return StatusCode(503, new { message = "Set Support__PublicOrigin, Support__AppKey, Support__AppSecret and Support__EncryptionKey in Render." });
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var payload = JsonSerializer.Serialize(new AuthorizationState(
            users.GetUserId(User)!, state, verifier, DateTimeOffset.UtcNow.AddMinutes(10)));
        Response.Cookies.Append(CookieName, store.Protect(payload, "dropbox-state"), new CookieOptions
        { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromMinutes(10), IsEssential = true });
        Response.Headers.CacheControl = "no-store";
        var url = QueryHelpers.AddQueryString("https://www.dropbox.com/oauth2/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = options.Value.AppKey, ["response_type"] = "code", ["redirect_uri"] = Callback,
            ["token_access_type"] = "offline", ["state"] = state, ["scope"] = "files.content.write",
            ["code_challenge_method"] = "S256", ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
        });
        return Ok(new { url });
    }

    [HttpGet("callback"), AllowAnonymous]
    public async Task<IActionResult> Complete(string? code, string? state, string? error, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!Ready) return BadRequest(new { message = "Dropbox connection is not configured." });
        var encrypted = Request.Cookies[CookieName];
        Response.Cookies.Delete(CookieName, new CookieOptions { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/" });
        if (string.IsNullOrWhiteSpace(encrypted) || string.IsNullOrWhiteSpace(state)) return Back("invalid-state");
        AuthorizationState? pending;
        try { pending = JsonSerializer.Deserialize<AuthorizationState>(store.Unprotect(encrypted, "dropbox-state")); }
        catch (Exception e) when (e is CryptographicException or FormatException or JsonException) { return Back("invalid-state"); }
        if (pending is null || pending.ExpiresAt < DateTimeOffset.UtcNow
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(pending.State))) return Back("invalid-state");
        var administrator = await users.FindByIdAsync(pending.UserId);
        if (administrator is null || !await users.IsInRoleAsync(administrator, "Admin") || await users.IsLockedOutAsync(administrator)) return Back("forbidden");
        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code)) return Back("cancelled");
        try
        {
            var client = clients.CreateClient("dropbox-oauth");
            using var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback,
                ["client_id"] = options.Value.AppKey, ["client_secret"] = options.Value.AppSecret,
                ["code_verifier"] = pending.Verifier
            });
            using var response = await client.PostAsync("https://api.dropboxapi.com/oauth2/token", body, cancellationToken);
            if (!response.IsSuccessStatusCode) return Back("exchange-failed");
            using var tokens = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!tokens.RootElement.TryGetProperty("refresh_token", out var refresh) || string.IsNullOrWhiteSpace(refresh.GetString())) return Back("missing-refresh");
            var accessToken = tokens.RootElement.GetProperty("access_token").GetString();
            var folder = options.Value.Folder.TrimEnd('/');
            if (!folder.StartsWith('/') || folder.Contains("..") || folder.Contains('\\')) return Back("folder-failed");
            using var folderRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/create_folder_v2");
            folderRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            folderRequest.Content = new StringContent(JsonSerializer.Serialize(new { path = folder, autorename = false }), Encoding.UTF8, "application/json");
            using var created = await client.SendAsync(folderRequest, cancellationToken);
            if (!created.IsSuccessStatusCode)
            {
                if (created.StatusCode != System.Net.HttpStatusCode.Conflict) return Back("folder-failed");
                using var detail = JsonDocument.Parse(await created.Content.ReadAsStringAsync(cancellationToken));
                if (!detail.RootElement.GetProperty("error_summary").GetString()!.StartsWith("path/conflict/folder", StringComparison.Ordinal)) return Back("folder-failed");
            }
            await store.SaveAsync(refresh.GetString()!, cancellationToken);
            return Back("connected");
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException
            || e is TaskCanceledException && !cancellationToken.IsCancellationRequested) { return Back("unavailable"); }
    }

    private IActionResult Back(string status) => Redirect(options.Value.PublicOrigin.TrimEnd('/') + "/users?dropbox=" + status);
    private sealed record AuthorizationState(string UserId, string State, string Verifier, DateTimeOffset ExpiresAt);
}
