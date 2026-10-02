using CvPlatform.Domain.Entities;
using CvPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Infrastructure.Integrations.Odoo;

namespace CvPlatform.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public const string SearchVector = "SearchVector";
    public const string TextSearchConfig = "simple";

    public DbSet<AttributeCategory> AttributeCategories => Set<AttributeCategory>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<AccessRule> AccessRules => Set<AccessRule>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Cv> Cvs => Set<Cv>();
    public DbSet<ProfileAttributeValue> ProfileAttributeValues => Set<ProfileAttributeValue>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<PositionExportToken> PositionExportTokens => Set<PositionExportToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<PositionExportToken>(entity =>
        {
            entity.HasKey(x => x.PositionId);
            entity.Property(x => x.PositionId).ValueGeneratedNever();
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne<Position>().WithOne().HasForeignKey<PositionExportToken>(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
            entity.Property(nameof(BaseEntity.CreatedAt)).IsRequired();
            entity.Property(nameof(BaseEntity.UpdatedAt)).IsRequired();

            entity.Property(nameof(BaseEntity.Version)).IsRowVersion();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified && entry.Property(x => x.Version).OriginalValue == 0)
            {
                entry.State = EntityState.Added;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
