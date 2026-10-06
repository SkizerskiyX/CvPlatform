using System.Security.Cryptography;
using System.Text;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CvPlatform.Infrastructure.Integrations.Support;

public sealed class DropboxConnectionStore(AppDbContext db, IOptions<SupportOptions> options)
{
    public bool HasKey => options.Value.EncryptionKey.Length >= 32;

    public string Protect(string value, string purpose)
    {
        if (!HasKey) throw new InvalidOperationException("Set Support__EncryptionKey to a random secret of at least 32 characters.");
        var nonce = RandomNumberGenerator.GetBytes(12);
        var input = Encoding.UTF8.GetBytes(value);
        var encrypted = new byte[input.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.EncryptionKey)), 16);
        aes.Encrypt(nonce, input, encrypted, tag, Encoding.UTF8.GetBytes(purpose));
        return Convert.ToBase64String(nonce.Concat(tag).Concat(encrypted).ToArray());
    }

    public string Unprotect(string value, string purpose)
    {
        if (!HasKey) throw new InvalidOperationException("Support encryption key is not configured.");
        var bytes = Convert.FromBase64String(value);
        if (bytes.Length < 28) throw new CryptographicException("Invalid encrypted value.");
        var output = new byte[bytes.Length - 28];
        using var aes = new AesGcm(SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.EncryptionKey)), 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), output, Encoding.UTF8.GetBytes(purpose));
        return Encoding.UTF8.GetString(output);
    }

    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!HasKey) return null;
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT \"EncryptedToken\" AS \"Value\" FROM \"SupportDropboxConnection\" WHERE \"Id\" = 1 AND \"AppKey\" = {0}",
            options.Value.AppKey).ToListAsync(cancellationToken);
        return rows.Count == 0 ? null : Unprotect(rows[0], "dropbox-refresh");
    }

    public Task SaveAsync(string token, CancellationToken cancellationToken)
    {
        var encrypted = Protect(token, "dropbox-refresh");
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SupportDropboxConnection" ("Id", "AppKey", "EncryptedToken", "ConnectedAt")
            VALUES (1, {options.Value.AppKey}, {encrypted}, {DateTime.UtcNow})
            ON CONFLICT ("Id") DO UPDATE SET "AppKey" = EXCLUDED."AppKey",
            "EncryptedToken" = EXCLUDED."EncryptedToken", "ConnectedAt" = EXCLUDED."ConnectedAt"
            """, cancellationToken);
    }
}
