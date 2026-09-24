using CvPlatform.Domain.Entities;
using CvPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace CvPlatform.Infrastructure.Persistence.Configurations;

internal static class SearchVectorExtensions
{
    /// <summary>
    /// Adds a stored generated tsvector column (full-text search, 'simple' config works for EN and RU)
    /// with a GIN index. Columns are concatenated with coalesce so NULLs do not wipe the vector.
    /// </summary>
    public static void HasSearchVector<T>(this EntityTypeBuilder<T> builder, params string[] columns) where T : class
    {
        var expression = string.Join(" || ' ' || ", columns.Select(c => $"coalesce(\"{c}\", '')"));
        builder.Property<NpgsqlTsVector>(AppDbContext.SearchVector)
            .HasComputedColumnSql($"to_tsvector('{AppDbContext.TextSearchConfig}'::regconfig, {expression})", stored: true);
        builder.HasIndex(AppDbContext.SearchVector).HasMethod("GIN");
    }
}

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.PreferredLanguage).HasMaxLength(10);
        builder.Property(x => x.PreferredTheme).HasMaxLength(10);
    }
}

public sealed class AttributeCategoryConfiguration : IEntityTypeConfiguration<AttributeCategory>
{
    public void Configure(EntityTypeBuilder<AttributeCategory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DataType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.SystemKey).HasMaxLength(50);
        builder.HasIndex(x => x.SystemKey).IsUnique().HasFilter("\"SystemKey\" IS NOT NULL");
        builder.Ignore(x => x.IsBuiltIn);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Category).AutoInclude();
        builder.HasMany(x => x.Options)
            .WithOne(x => x.AttributeDefinition)
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public sealed class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasMaxLength(200).IsRequired();
    }
}

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(2000);
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
        builder.PrimitiveCollection(x => x.ProjectTags).HasField("_projectTags").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.UpdatedAt);
        builder.HasSearchVector(nameof(Position.Title), nameof(Position.ShortDescription), nameof(Position.Company));
        builder.HasMany(x => x.PositionAttributes)
            .WithOne(x => x.Position)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.PositionAttributes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.AccessRules)
            .WithOne(x => x.Position)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.AccessRules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PositionAttributeConfiguration : IEntityTypeConfiguration<PositionAttribute>
{
    public void Configure(EntityTypeBuilder<PositionAttribute> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PositionId, x.AttributeDefinitionId }).IsUnique();
        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AccessRuleConfiguration : IEntityTypeConfiguration<AccessRule>
{
    public void Configure(EntityTypeBuilder<AccessRule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Operator).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ComparisonValue).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentityUserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => x.IdentityUserId).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.PhotoUrl).HasMaxLength(1000);
        builder.Ignore(x => x.DisplayName);
        builder.HasSearchVector(nameof(UserProfile.FirstName), nameof(UserProfile.LastName), nameof(UserProfile.Location));
        builder.HasMany(x => x.AttributeValues)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.AttributeValues).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Projects)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Projects).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProfileAttributeValueConfiguration : IEntityTypeConfiguration<ProfileAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProfileAttributeValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProfileId, x.AttributeDefinitionId }).IsUnique();
        builder.Property(x => x.StringValue).HasMaxLength(8000);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.HasSearchVector(nameof(ProfileAttributeValue.StringValue));
        builder.HasOne(x => x.AttributeDefinition)
            .WithMany()
            .HasForeignKey(x => x.AttributeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(8000);
        builder.PrimitiveCollection(x => x.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.ProfileId);
        builder.HasIndex(x => x.Tags).HasMethod("GIN");
        builder.HasSearchVector(nameof(Project.Name), nameof(Project.Description));
    }
}

public sealed class CvConfiguration : IEntityTypeConfiguration<Cv>
{
    public void Configure(EntityTypeBuilder<Cv> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => new { x.ProfileId, x.PositionId }).IsUnique();
        builder.HasIndex(x => new { x.PositionId, x.Status });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DiscussionPostConfiguration : IEntityTypeConfiguration<DiscussionPost>
{
    public void Configure(EntityTypeBuilder<DiscussionPost> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => new { x.PositionId, x.CreatedAt });
        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CvId, x.RecruiterProfileId }).IsUnique();
        builder.HasOne(x => x.Cv)
            .WithMany()
            .HasForeignKey(x => x.CvId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.RecruiterProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
