using CvPlatform.Domain.Entities;

namespace CvPlatform.Application.Abstractions;

public interface IUnitOfWork
{
    void ExpectVersion(BaseEntity entity, uint version);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IAttributeCategoryRepository
{
    Task<AttributeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeCategory>> ListAsync(CancellationToken cancellationToken);
}

public interface IAttributeDefinitionRepository
{
    Task<AttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeDefinition>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeDefinition>> GetBuiltInAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeDefinition>> SearchAsync(string? namePrefix, Guid? categoryId, int take, CancellationToken cancellationToken);
    Task AddAsync(AttributeDefinition definition, CancellationToken cancellationToken);
    void Remove(AttributeDefinition definition);
}

public interface IPositionRepository
{
    Task<Position?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Position>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task AddAsync(Position position, CancellationToken cancellationToken);
    void Remove(Position position);
}

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserProfile?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken);
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken);
}

public interface ICvRepository
{
    Task<Cv?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Cv>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid profileId, Guid positionId, CancellationToken cancellationToken);
    Task AddAsync(Cv cv, CancellationToken cancellationToken);
    void Remove(Cv cv);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> ListByProfileAsync(Guid profileId, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    void Remove(Project project);
}

public interface ILikeRepository
{
    Task<Like?> GetAsync(Guid cvId, Guid recruiterProfileId, CancellationToken cancellationToken);
    Task<int> CountByCvAsync(Guid cvId, CancellationToken cancellationToken);
    Task AddAsync(Like like, CancellationToken cancellationToken);
    void Remove(Like like);
}

public interface IDiscussionRepository
{
    Task<IReadOnlyList<DiscussionPost>> GetByPositionAsync(Guid positionId, DateTime? after, CancellationToken cancellationToken);
    Task AddAsync(DiscussionPost post, CancellationToken cancellationToken);
}
