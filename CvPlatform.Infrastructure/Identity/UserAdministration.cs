using CvPlatform.Application.Security;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Identity;

public sealed record UserSnapshot(string UserId, string Email, Guid? ProfileId, bool IsBlocked, IReadOnlyList<string> Roles, string? Language, string? Theme);

public sealed record UserAdminDto(
    string Id,
    Guid? ProfileId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsBlocked,
    DateTime RegisteredAt);

public interface IUserDirectory
{
    /// <summary>Current state of the user (roles, lock-out) loaded with a single query; used on every authenticated request.</summary>
    Task<UserSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken);
    Task SetPreferencesAsync(string userId, string? language, string? theme, CancellationToken cancellationToken);
}

public interface IUserAdministration
{
    Task<IReadOnlyList<UserAdminDto>> ListAsync(string? search, CancellationToken cancellationToken);
    Task BlockAsync(string actorUserId, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);
    Task UnblockAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);
    Task DeleteAsync(string actorUserId, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);
    Task AddRoleAsync(IReadOnlyCollection<string> userIds, string role, CancellationToken cancellationToken);
    Task RemoveRoleAsync(IReadOnlyCollection<string> userIds, string role, CancellationToken cancellationToken);
}

public sealed class UserAdministration(AppDbContext db) : IUserDirectory, IUserAdministration
{
    public async Task<UserSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSnapshot(
                u.Id,
                u.Email ?? string.Empty,
                db.UserProfiles.Where(p => p.IdentityUserId == u.Id).Select(p => (Guid?)p.Id).FirstOrDefault(),
                u.LockoutEnd != null && u.LockoutEnd > now,
                db.UserRoles.Where(ur => ur.UserId == u.Id).Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).ToList(),
                u.PreferredLanguage,
                u.PreferredTheme))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SetPreferencesAsync(string userId, string? language, string? theme, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(u => u.PreferredLanguage, u => language ?? u.PreferredLanguage)
                .SetProperty(u => u.PreferredTheme, u => theme ?? u.PreferredTheme),
            cancellationToken);

    public async Task<IReadOnlyList<UserAdminDto>> ListAsync(string? search, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + AttributeDefinitionRepository.EscapeLike(search.Trim()) + "%";
            users = users.Where(u => EF.Functions.ILike(u.Email!, pattern, "\\")
                || db.UserProfiles.Any(p => p.IdentityUserId == u.Id && (EF.Functions.ILike(p.FirstName, pattern, "\\") || EF.Functions.ILike(p.LastName, pattern, "\\"))));
        }

        var rows = await users
            .OrderBy(u => u.Email)
            .Take(500)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.RegisteredAt,
                Blocked = u.LockoutEnd != null && u.LockoutEnd > now,
                Profile = db.UserProfiles.Where(p => p.IdentityUserId == u.Id).Select(p => new { p.Id, p.FirstName, p.LastName }).FirstOrDefault(),
                Roles = db.UserRoles.Where(ur => ur.UserId == u.Id).Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).ToList()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new UserAdminDto(
            x.Id,
            x.Profile?.Id,
            x.Email ?? string.Empty,
            x.Profile is null ? string.Empty : $"{x.Profile.FirstName} {x.Profile.LastName}".Trim(),
            x.Roles.OrderBy(r => r).ToArray(),
            x.Blocked,
            x.RegisteredAt)).ToArray();
    }

    public async Task BlockAsync(string actorUserId, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        EnsureNotSelf(actorUserId, userIds, "block");
        await db.Users.Where(u => userIds.Contains(u.Id)).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(u => u.LockoutEnabled, true)
                .SetProperty(u => u.LockoutEnd, DateTimeOffset.MaxValue)
                .SetProperty(u => u.SecurityStamp, Guid.NewGuid().ToString("N")),
            cancellationToken);
    }

    public Task UnblockAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken) =>
        db.Users.Where(u => userIds.Contains(u.Id)).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(u => u.LockoutEnd, (DateTimeOffset?)null)
                .SetProperty(u => u.AccessFailedCount, 0),
            cancellationToken);

    public async Task DeleteAsync(string actorUserId, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        EnsureNotSelf(actorUserId, userIds, "delete");

        // Profiles (and their values, projects, CVs, likes, posts) are removed by ON DELETE CASCADE.
        await db.Users.Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddRoleAsync(IReadOnlyCollection<string> userIds, string role, CancellationToken cancellationToken)
    {
        var roleId = await GetRoleIdAsync(role, cancellationToken);
        var existingUsers = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken);
        var alreadyInRole = await db.UserRoles.Where(ur => ur.RoleId == roleId && userIds.Contains(ur.UserId)).Select(ur => ur.UserId).ToListAsync(cancellationToken);
        db.UserRoles.AddRange(existingUsers.Except(alreadyInRole).Select(userId => new IdentityUserRole<string> { UserId = userId, RoleId = roleId }));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Administrators may also remove their own Administrator role.</summary>
    public async Task RemoveRoleAsync(IReadOnlyCollection<string> userIds, string role, CancellationToken cancellationToken)
    {
        var roleId = await GetRoleIdAsync(role, cancellationToken);
        await db.UserRoles.Where(ur => ur.RoleId == roleId && userIds.Contains(ur.UserId)).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<string> GetRoleIdAsync(string role, CancellationToken cancellationToken)
    {
        if (!RoleNames.All.Contains(role))
        {
            throw new InvalidOperationException($"Unknown role '{role}'.");
        }

        return await db.Roles.Where(r => r.Name == role).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Role '{role}' is not configured.");
    }

    private static void EnsureNotSelf(string actorUserId, IReadOnlyCollection<string> userIds, string action)
    {
        if (userIds.Contains(actorUserId))
        {
            throw new InvalidOperationException($"You cannot {action} your own account.");
        }
    }
}
