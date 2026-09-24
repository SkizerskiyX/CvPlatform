using CvPlatform.Application.Abstractions;
using CvPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Persistence;

public sealed class AttributeCategoryRepository(AppDbContext db) : IAttributeCategoryRepository
{
    public Task<AttributeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.AttributeCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttributeCategory>> ListAsync(CancellationToken cancellationToken) =>
        await db.AttributeCategories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
}

public sealed class AttributeDefinitionRepository(AppDbContext db) : IAttributeDefinitionRepository
{
    public Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.AttributeDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AttributeDefinition>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await db.AttributeDefinitions.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();
        return db.AttributeDefinitions.AnyAsync(x => x.Name.ToLower() == lowered && x.Id != exceptId, cancellationToken);
    }

    public async Task<IReadOnlyList<AttributeDefinition>> GetBuiltInAsync(CancellationToken cancellationToken)
    {
        var items = await db.AttributeDefinitions.AsNoTracking().Where(x => x.SystemKey != null).ToListAsync(cancellationToken);
        return items.OrderBy(x => BuiltInAttributesOrder(x.SystemKey)).ToArray();
    }

    public async Task<IReadOnlyList<AttributeDefinition>> SearchAsync(string? namePrefix, Guid? categoryId, int take, CancellationToken cancellationToken)
    {
        var query = db.AttributeDefinitions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(namePrefix))
        {
            // ILIKE 'prefix%' – escaped so user input cannot inject wildcards.
            var pattern = EscapeLike(namePrefix) + "%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        return await query.OrderBy(x => x.Name).Take(take).ToListAsync(cancellationToken);
    }

    public Task AddAsync(AttributeDefinition definition, CancellationToken cancellationToken) =>
        db.AttributeDefinitions.AddAsync(definition, cancellationToken).AsTask();

    public void Remove(AttributeDefinition definition) => db.AttributeDefinitions.Remove(definition);

    internal static int BuiltInAttributesOrder(string? key)
    {
        var index = key is null ? -1 : BuiltInAttributes.All.ToList().IndexOf(key);
        return index < 0 ? int.MaxValue : index;
    }

    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

public sealed class PositionRepository(AppDbContext db) : IPositionRepository
{
    public Task<Position?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Positions
            .Include(x => x.PositionAttributes).ThenInclude(x => x.AttributeDefinition)
            .Include(x => x.AccessRules).ThenInclude(x => x.AttributeDefinition)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Position>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await db.Positions.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

    public Task AddAsync(Position position, CancellationToken cancellationToken) =>
        db.Positions.AddAsync(position, cancellationToken).AsTask();

    public void Remove(Position position) => db.Positions.Remove(position);
}

public sealed class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.UserProfiles.Include(x => x.AttributeValues).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<UserProfile?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken) =>
        db.UserProfiles.Include(x => x.AttributeValues).FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId, cancellationToken);

    public Task AddAsync(UserProfile profile, CancellationToken cancellationToken) =>
        db.UserProfiles.AddAsync(profile, cancellationToken).AsTask();
}

public sealed class CvRepository(AppDbContext db) : ICvRepository
{
    public Task<Cv?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Cvs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Cv>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await db.Cvs.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid profileId, Guid positionId, CancellationToken cancellationToken) =>
        db.Cvs.AnyAsync(x => x.ProfileId == profileId && x.PositionId == positionId, cancellationToken);

    public Task AddAsync(Cv cv, CancellationToken cancellationToken) => db.Cvs.AddAsync(cv, cancellationToken).AsTask();

    public void Remove(Cv cv) => db.Cvs.Remove(cv);
}

public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await db.Projects.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Project>> ListByProfileAsync(Guid profileId, CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking().Where(x => x.ProfileId == profileId).OrderByDescending(x => x.PeriodStart).ToListAsync(cancellationToken);

    public Task AddAsync(Project project, CancellationToken cancellationToken) => db.Projects.AddAsync(project, cancellationToken).AsTask();

    public void Remove(Project project) => db.Projects.Remove(project);
}

public sealed class LikeRepository(AppDbContext db) : ILikeRepository
{
    public Task<Like?> GetAsync(Guid cvId, Guid recruiterProfileId, CancellationToken cancellationToken) =>
        db.Likes.FirstOrDefaultAsync(x => x.CvId == cvId && x.RecruiterProfileId == recruiterProfileId, cancellationToken);

    public Task<int> CountByCvAsync(Guid cvId, CancellationToken cancellationToken) =>
        db.Likes.CountAsync(x => x.CvId == cvId, cancellationToken);

    public Task AddAsync(Like like, CancellationToken cancellationToken) => db.Likes.AddAsync(like, cancellationToken).AsTask();

    public void Remove(Like like) => db.Likes.Remove(like);
}

public sealed class DiscussionRepository(AppDbContext db) : IDiscussionRepository
{
    public async Task<IReadOnlyList<DiscussionPost>> GetByPositionAsync(Guid positionId, DateTime? after, CancellationToken cancellationToken)
    {
        var query = db.DiscussionPosts.AsNoTracking().Include(x => x.Author).Where(x => x.PositionId == positionId);
        if (after.HasValue)
        {
            var since = DateTime.SpecifyKind(after.Value, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt > since);
        }

        return await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Take(500).ToListAsync(cancellationToken);
    }

    public Task AddAsync(DiscussionPost post, CancellationToken cancellationToken) =>
        db.DiscussionPosts.AddAsync(post, cancellationToken).AsTask();
}
