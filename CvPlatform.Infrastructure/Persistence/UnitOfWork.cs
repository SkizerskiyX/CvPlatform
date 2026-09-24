using CvPlatform.Application.Abstractions;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvPlatform.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private const string ConflictMessage = "The data was changed by someone else. Reload to get the latest version.";

    public void ExpectVersion(BaseEntity entity, uint version)
    {
        // Fast path: reject stale versions before doing any work...
        if (version == 0 || entity.Version != version)
        {
            throw new ConcurrencyConflictException(ConflictMessage);
        }

        // ...and let the database enforce it atomically (UPDATE ... WHERE xmin = @version).
        db.Entry(entity).Property(x => x.Version).OriginalValue = version;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(ConflictMessage);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new InvalidOperationException("Such a record already exists.");
        }
    }
}
