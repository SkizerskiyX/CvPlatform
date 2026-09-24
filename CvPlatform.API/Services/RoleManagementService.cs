using Microsoft.AspNetCore.Identity;

namespace CvPlatform.API.Services;

public interface IRoleManagementService
{
    Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken);
    Task EnsureRoleExistsAsync(string roleName);
    Task AssignRoleAsync(string userId, string roleName);
    Task RemoveRoleAsync(string userId, string roleName);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId);
}

public sealed class RoleManagementService(UserManager<Infrastructure.Identity.ApplicationUser> users, RoleManager<IdentityRole> roles) : IRoleManagementService
{
    public async Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        var result = new List<string>();
        result.AddRange(roles.Roles.Select(x => x.Name!));
        await Task.CompletedTask;
        return result;
    }

    public async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await roles.RoleExistsAsync(roleName))
        {
            await roles.CreateAsync(new IdentityRole(roleName));
        }
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' does not exist.");
        await EnsureRoleExistsAsync(roleName);
        var result = await users.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task RemoveRoleAsync(string userId, string roleName)
    {
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' does not exist.");
        var result = await users.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' does not exist.");
        var result = await users.GetRolesAsync(user);
        return result.ToArray();
    }
}
