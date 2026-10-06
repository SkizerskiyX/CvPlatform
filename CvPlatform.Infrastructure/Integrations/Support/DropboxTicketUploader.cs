using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CvPlatform.Infrastructure.Integrations.Support;

public sealed class SupportOptions
{
    public string PublicOrigin { get; set; } = "";
    public string AppKey { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string EncryptionKey { get; set; } = "";
    public string Folder { get; set; } = "/support-tickets";
}

public sealed class DropboxTicketUploader(HttpClient client, IOptions<SupportOptions> options, DropboxConnectionStore? store = null)
{
    public bool IsConfigured => (!string.IsNullOrWhiteSpace(options.Value.AccessToken)
        || !string.IsNullOrWhiteSpace(options.Value.AppKey)
        && !string.IsNullOrWhiteSpace(options.Value.AppSecret)
        && !string.IsNullOrWhiteSpace(options.Value.RefreshToken))
        && Uri.TryCreate(options.Value.PublicOrigin, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.AbsolutePath == "/" && uri.UserInfo.Length == 0
        && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    public async Task UploadAsync(Guid id, string userId, string json, CancellationToken cancellationToken)
    {
        if (!await IsConfiguredAsync(cancellationToken)) throw new InvalidOperationException("Support upload is not configured. Contact an administrator.");
        var settings = options.Value;
        var folder = settings.Folder.TrimEnd('/');
        if (!folder.StartsWith('/') || folder.Contains("..") || folder.Contains('\\'))
            throw new InvalidOperationException("Support folder configuration is invalid.");
        var savedRefresh = store is null ? null : await store.ReadAsync(cancellationToken);
        var refresh = savedRefresh ?? settings.RefreshToken;
        var token = string.IsNullOrWhiteSpace(refresh) ? settings.AccessToken.Trim() : "";
        if (string.IsNullOrWhiteSpace(token))
        {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token", ["refresh_token"] = refresh,
                ["client_id"] = settings.AppKey, ["client_secret"] = settings.AppSecret
            })
        };
        using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Dropbox authorization failed. Ask an administrator to check support credentials.");
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        token = tokenJson.RootElement.GetProperty("access_token").GetString()!;
        }
        var owner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(userId)))[..16];
        using var upload = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
        upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        upload.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(new
        {
            path = $"{folder}/{owner}-{id:N}.json", mode = "overwrite", autorename = false, mute = true
        }));
        upload.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        upload.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var response = await client.SendAsync(upload, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Dropbox token expired or was revoked. Generate a new access token or configure a refresh token.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Dropbox did not accept the support ticket. Check the upload folder and permissions, then retry.");
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
    {
        if (IsConfigured) return true;
        if (store is null || string.IsNullOrWhiteSpace(options.Value.AppKey) || string.IsNullOrWhiteSpace(options.Value.AppSecret)) return false;
        if (!Uri.TryCreate(options.Value.PublicOrigin, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || uri.AbsolutePath != "/" || uri.UserInfo.Length != 0
            || uri.Query.Length != 0 || uri.Fragment.Length != 0) return false;
        return await store.ReadAsync(cancellationToken) is not null;
    }
}
