using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CvPlatform.Infrastructure.Persistence;

public static class SeedData
{
    public const string PersonalCategory = "Personal Information";

    private static readonly string[] Categories =
    [
        PersonalCategory, "Certification", "Domain Knowledge", "Soft Skills", "Languages", "Technical Skills", "Education", "Experience"
    ];

    public static async Task EnsureSeededAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        await MigrateAsync(db, logger, cancellationToken);

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Predefined list of attribute categories.
        var existingCategories = await db.AttributeCategories.Select(x => x.Name).ToListAsync(cancellationToken);
        db.AttributeCategories.AddRange(Categories.Except(existingCategories).Select(name => new AttributeCategory(name)));
        await db.SaveChangesAsync(cancellationToken);
        var categories = await db.AttributeCategories.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

        await EnsureBuiltInAttributesAsync(db, categories[PersonalCategory], cancellationToken);
        await EnsureSampleAttributesAsync(db, categories, cancellationToken);
        await EnsureSamplePositionsAsync(db, cancellationToken);
        await EnsureUsersAsync(provider, db, cancellationToken);
    }

    private static async Task MigrateAsync(AppDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested && exception is not InvalidOperationException)
            {
                logger.LogWarning(exception, "Database is not ready yet (attempt {Attempt}/{Max}).", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private static async Task EnsureBuiltInAttributesAsync(AppDbContext db, Guid personalCategoryId, CancellationToken cancellationToken)
    {
        var builtIn = new (string Key, string Name, AttributeDataType Type, string Description)[]
        {
            (BuiltInAttributes.FirstName, "First Name", AttributeDataType.String, "Candidate first name (built-in)."),
            (BuiltInAttributes.LastName, "Last Name", AttributeDataType.String, "Candidate last name (built-in)."),
            (BuiltInAttributes.Location, "Location", AttributeDataType.String, "City / country (built-in)."),
            (BuiltInAttributes.Photo, "Personal Photo", AttributeDataType.Image, "Profile photo stored in external cloud storage (built-in).")
        };

        var existingKeys = await db.AttributeDefinitions.Where(x => x.SystemKey != null).Select(x => x.SystemKey!).ToListAsync(cancellationToken);
        var existingNames = await db.AttributeDefinitions.Select(x => x.Name.ToLower()).ToListAsync(cancellationToken);
        foreach (var item in builtIn.Where(x => !existingKeys.Contains(x.Key)))
        {
            var name = existingNames.Contains(item.Name.ToLower()) ? $"{item.Name} (built-in)" : item.Name;
            db.AttributeDefinitions.Add(new AttributeDefinition(name, item.Description, item.Type, personalCategoryId, item.Key));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSampleAttributesAsync(AppDbContext db, IReadOnlyDictionary<string, Guid> categories, CancellationToken cancellationToken)
    {
        if (await db.AttributeDefinitions.AnyAsync(x => x.SystemKey == null, cancellationToken))
        {
            return;
        }

        var english = new AttributeDefinition("English Level", "CEFR level of English.", AttributeDataType.Dropdown, categories["Languages"]);
        english.SetOptions(["A1", "A2", "B1", "B2", "C1", "C2"]);
        var presentation = new AttributeDefinition("Presentation Skills", "Self-assessed presentation skills.", AttributeDataType.Dropdown, categories["Soft Skills"]);
        presentation.SetOptions(["Basic", "Intermediate", "Advanced"]);

        db.AttributeDefinitions.AddRange(
            english,
            presentation,
            new AttributeDefinition("GPA", "Grade point average (0–4).", AttributeDataType.Numeric, categories["Education"]),
            new AttributeDefinition("IELTS Score", "Overall IELTS band.", AttributeDataType.Numeric, categories["Certification"]),
            new AttributeDefinition("Remote Work Availability", "Ready to work remotely.", AttributeDataType.Boolean, categories["Personal Information"]),
            new AttributeDefinition("About Me", "Short professional summary (Markdown).", AttributeDataType.Text, categories["Personal Information"]),
            new AttributeDefinition("Years of Experience", "Total years of professional experience.", AttributeDataType.Numeric, categories["Experience"]),
            new AttributeDefinition("Primary Stack", "Main technology stack.", AttributeDataType.String, categories["Technical Skills"]),
            new AttributeDefinition("Available From", "Earliest start date.", AttributeDataType.Date, categories["Experience"]),
            new AttributeDefinition("Last Employment", "Period of the last employment.", AttributeDataType.Period, categories["Experience"]));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSamplePositionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Positions.AnyAsync(cancellationToken))
        {
            return;
        }

        var definitions = await db.AttributeDefinitions.ToDictionaryAsync(x => x.SystemKey ?? x.Name, cancellationToken);
        Guid Id(string key) => definitions[key].Id;
        var builtIn = BuiltInAttributes.All.Where(definitions.ContainsKey).Select(Id).ToList();

        var backend = new Position("Backend Developer", "ASP.NET Core, EF Core, PostgreSQL.", true);
        backend.UpdateBasics(backend.Title, backend.ShortDescription, "Contoso", PositionLevel.Middle, true, 3, ["c#", ".net", "postgresql"]);
        backend.SetAttributes([.. builtIn, Id("Primary Stack"), Id("Years of Experience"), Id("English Level")]);

        var analyst = new Position("Business Analyst", "Requirements, BPMN, stakeholder communication.", false);
        analyst.UpdateBasics(analyst.Title, analyst.ShortDescription, "Fabrikam", PositionLevel.Senior, false, 2, ["bpmn", "sql"]);
        analyst.SetAttributes([.. builtIn, Id("English Level"), Id("GPA"), Id("Presentation Skills")]);
        analyst.SetAccessRules([new PositionRuleSpec(definitions["Remote Work Availability"], ComparisonOperator.EqualTo, "true")]);

        var qa = new Position("QA Engineer", "Manual and automated testing.", true);
        qa.UpdateBasics(qa.Title, qa.ShortDescription, "Contoso", PositionLevel.Junior, true, 3, ["testing", "selenium"]);
        qa.SetAttributes([.. builtIn, Id("About Me"), Id("Available From")]);

        db.Positions.AddRange(backend, analyst, qa);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureUsersAsync(IServiceProvider provider, AppDbContext db, CancellationToken cancellationToken)
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var demo = new (string Email, string Password, string Role, string First, string Last)[]
        {
            ("admin@cv.local", "Admin123!", RoleNames.Admin, "Admin", "User"),
            ("recruiter@cv.local", "Recruiter123!", RoleNames.Recruiter, "Rita", "Recruiter"),
            ("candidate@cv.local", "Candidate123!", RoleNames.Candidate, "Carl", "Candidate")
        };

        foreach (var item in demo)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = item.Email, Email = item.Email, EmailConfirmed = true };
                await userManager.CreateAsync(user, item.Password);
                await userManager.AddToRoleAsync(user, item.Role);
            }

            if (!await db.UserProfiles.AnyAsync(x => x.IdentityUserId == user.Id, cancellationToken))
            {
                db.UserProfiles.Add(new UserProfile(user.Id, item.First, item.Last, null));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Users created before roles existed become candidates.
        var candidateRoleId = await db.Roles.Where(r => r.Name == RoleNames.Candidate).Select(r => r.Id).FirstAsync(cancellationToken);
        var withoutRoles = await db.Users.Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id)).Select(u => u.Id).ToListAsync(cancellationToken);
        db.UserRoles.AddRange(withoutRoles.Select(id => new IdentityUserRole<string> { UserId = id, RoleId = candidateRoleId }));
        await db.SaveChangesAsync(cancellationToken);
    }
}
