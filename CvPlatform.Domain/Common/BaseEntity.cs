namespace CvPlatform.Domain.Entities;

public abstract class BaseEntity : IEquatable<BaseEntity>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Mapped to the PostgreSQL system column <c>xmin</c>,
    /// so it changes automatically on every UPDATE of the row.
    /// </summary>
    public uint Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected void RefreshUpdatedAt() => UpdatedAt = DateTime.UtcNow;

    public bool Equals(BaseEntity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id != Guid.Empty && Id == other.Id;
    }

    public override bool Equals(object? other) => Equals(other as BaseEntity);

    public override int GetHashCode() => Id == Guid.Empty ? base.GetHashCode() : Id.GetHashCode();
}
