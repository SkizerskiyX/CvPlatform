using CvPlatform.Application.Abstractions;
using CvPlatform.Application.Services;
using CvPlatform.Application.Validators;
using CvPlatform.Infrastructure.Identity;
using CvPlatform.Infrastructure.Persistence;
using CvPlatform.Infrastructure.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CvPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CvPlatform")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IAttributeCategoryRepository, AttributeCategoryRepository>();
        services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<ICvRepository, CvRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAttributeDefinitionService, AttributeDefinitionService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICvService, CvService>();
        services.AddScoped<IDiscussionService, DiscussionService>();
        services.AddScoped<ILikeService, LikeService>();

        services.AddScoped<PositionQueries>();
        services.AddScoped<IPositionQueries>(sp => sp.GetRequiredService<PositionQueries>());
        services.AddScoped<ICvQueries, CvQueries>();
        services.AddScoped<IProfileQueries, ProfileQueries>();
        services.AddScoped<ISearchQueries, SearchQueries>();
        services.AddScoped<IStatsQueries, StatsQueries>();

        services.AddScoped<UserAdministration>();
        services.AddScoped<IUserDirectory>(sp => sp.GetRequiredService<UserAdministration>());
        services.AddScoped<IUserAdministration>(sp => sp.GetRequiredService<UserAdministration>());

        services.AddValidatorsFromAssemblyContaining<SavePositionRequestValidator>();
        return services;
    }
}
