using System.Net;
using System.Text.Json;
using CvPlatform.Infrastructure.Integrations.Support;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Infrastructure.Persistence;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

var settings = new SupportOptions
{
    PublicOrigin = "https://example.com", AppKey = "key", AppSecret = "secret", RefreshToken = "refresh"
};
var id = Guid.NewGuid();
var handler = new CheckHandler();
var uploader = new DropboxTicketUploader(new HttpClient(handler), Options.Create(settings));
if (!uploader.IsConfigured) throw new Exception("Valid configuration rejected");
await uploader.UploadAsync(id, "user-1", "{\"Summary\":\"Тест\"}", default);
if (handler.Calls != 2 || !handler.Path.EndsWith($"{id:N}.json") || !handler.Body.Contains("Тест"))
    throw new Exception("JSON upload or filename is incorrect");
settings.PublicOrigin = "http://example.com";
if (uploader.IsConfigured) throw new Exception("Non-HTTPS origin accepted");
settings.PublicOrigin = "https://example.com/profile";
if (uploader.IsConfigured) throw new Exception("Origin with path accepted");
settings.PublicOrigin = "https://example.com";
settings.Folder = "/../bad";
try { await uploader.UploadAsync(id, "user-1", "{}", default); throw new Exception("Invalid folder accepted"); }
catch (InvalidOperationException) { }
settings.Folder = "/support-tickets";
handler.FailToken = true;
try { await uploader.UploadAsync(id, "user-1", "{}", default); throw new Exception("Failed authorization accepted"); }
catch (InvalidOperationException error)
{
    if (error.Message.Contains("secret-provider-response")) throw new Exception("Provider details leaked");
}
handler.FailToken = false;
handler.FailUpload = true;
try { await uploader.UploadAsync(id, "user-1", "{}", default); throw new Exception("Failed upload accepted"); }
catch (InvalidOperationException) { }
settings.EncryptionKey = "test-only-encryption-key-with-32-characters";
using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);
var store = new DropboxConnectionStore(db, Options.Create(settings));
var protectedValue = store.Protect("test-refresh-token", "dropbox-refresh");
if (protectedValue.Contains("test-refresh-token") || store.Unprotect(protectedValue, "dropbox-refresh") != "test-refresh-token")
    throw new Exception("Encryption round trip failed");
try { store.Unprotect(protectedValue, "dropbox-state"); throw new Exception("Wrong purpose accepted"); }
catch (CryptographicException) { }
var tampered = Convert.FromBase64String(protectedValue);
tampered[^1] ^= 1;
try { store.Unprotect(Convert.ToBase64String(tampered), "dropbox-refresh"); throw new Exception("Tampered token accepted"); }
catch (CryptographicException) { }
settings.EncryptionKey = "another-test-only-key-with-32-characters";
try { store.Unprotect(protectedValue, "dropbox-refresh"); throw new Exception("Wrong key accepted"); }
catch (CryptographicException) { }
if (!db.GetService<IMigrationsAssembly>().Migrations.ContainsKey("20261006220000_AddSupportDropboxConnection"))
    throw new Exception("Dropbox connection migration was not discovered");
if (db.Database.HasPendingModelChanges()) throw new Exception("EF snapshot does not match the runtime model");
Console.WriteLine("12 support upload, encryption and migration checks passed");

sealed class CheckHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public string Path { get; private set; } = "";
    public string Body { get; private set; } = "";
    public bool FailToken { get; set; }
    public bool FailUpload { get; set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (request.RequestUri!.Host == "api.dropboxapi.com")
            return new HttpResponseMessage(FailToken ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
            { Content = new StringContent(FailToken ? "secret-provider-response" : "{\"access_token\":\"test-token\"}") };
        if (request.RequestUri.Host != "content.dropboxapi.com" || request.Headers.Authorization?.Parameter != "test-token")
            throw new Exception("Wrong upload endpoint or authorization");
        using var args = JsonDocument.Parse(request.Headers.GetValues("Dropbox-API-Arg").Single());
        Path = args.RootElement.GetProperty("path").GetString()!;
        if (args.RootElement.GetProperty("mode").GetString() != "overwrite") throw new Exception("Retry path not stable");
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(FailUpload ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = new StringContent("{}") };
    }
}
