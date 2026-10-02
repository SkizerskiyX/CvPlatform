using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CvPlatform.Application.Common;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Infrastructure.Integrations.Odoo;

public sealed record ExportPopularValue(string Value, int Count);
public sealed record ExportNumeric(decimal Minimum, decimal Maximum, decimal Average);
public sealed record ExportDates(string Earliest, string Latest);
public sealed record ExportPeriod(string EarliestStart, string? LatestEnd, decimal? AverageDays, int OpenEndedCount);
public sealed record ExportAttribute(Guid Id, string Title, string Type, int ValueCount, int MissingCount,
    bool ValuesWithheld, ExportNumeric? Numeric, IReadOnlyList<ExportPopularValue> PopularValues, ExportDates? Dates, ExportPeriod? Period);
public sealed record ExportPosition(Guid Id, string Title, string? Company);
public sealed record PositionExport(int SchemaVersion, DateTime GeneratedAt, ExportPosition Position, int PublishedCvCount, IReadOnlyList<ExportAttribute> Attributes);

public sealed class PositionExportService(AppDbContext db)
{
    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<string> GenerateTokenAsync(Guid positionId, CancellationToken cancellationToken)
    {
        var token = "cvp_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = HashToken(token);
        var generatedAt = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PositionExportTokens" ("PositionId", "TokenHash", "GeneratedAt")
            VALUES ({positionId}, {hash}, {generatedAt})
            ON CONFLICT ("PositionId") DO UPDATE SET "TokenHash" = EXCLUDED."TokenHash", "GeneratedAt" = EXCLUDED."GeneratedAt"
            """, cancellationToken);
        return token;
    }

    public async Task<PositionExport?> ExportAsync(string token, CancellationToken cancellationToken)
    {
        if (token.Length != 47 || !token.StartsWith("cvp_", StringComparison.Ordinal)
            || token.AsSpan(4).ContainsAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_")) return null;
        var hash = HashToken(token);
        var positionId = await db.PositionExportTokens.AsNoTracking().Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.PositionId).SingleOrDefaultAsync(cancellationToken);
        if (positionId is null) return null;

        var position = await db.Positions.AsNoTracking().Include(x => x.PositionAttributes)
            .ThenInclude(x => x.AttributeDefinition).ThenInclude(x => x!.Options)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == positionId, cancellationToken);
        if (position is null) return null;
        var profileIds = db.Cvs.Where(x => x.PositionId == positionId && x.Status == CvStatus.Published).Select(x => x.ProfileId);
        var profiles = await db.UserProfiles.AsNoTracking().Where(x => profileIds.Contains(x.Id))
            .Include(x => x.AttributeValues).AsSplitQuery().ToListAsync(cancellationToken);
        return Aggregate(position, profiles);
    }

    public static PositionExport Aggregate(Position position, IReadOnlyList<UserProfile> profiles)
    {
        var attributes = position.PositionAttributes.OrderBy(x => x.DisplayOrder).Select(attribute =>
        {
            var definition = attribute.AttributeDefinition!;
            var values = profiles.Select(profile => AttributeValues.Read(profile, definition,
                profile.AttributeValues.ToDictionary(x => x.AttributeDefinitionId)))
                .Where(value => !AttributeValues.IsEmpty(definition.DataType, value)).ToArray();
            var withheld = definition.IsBuiltIn || definition.DataType == AttributeDataType.Image;
            ExportNumeric? numeric = null;
            ExportDates? dates = null;
            ExportPeriod? period = null;
            IReadOnlyList<ExportPopularValue> popular = [];
            if (!withheld && values.Length > 0)
            {
                switch (definition.DataType)
                {
                    case AttributeDataType.Numeric:
                        var numbers = values.Select(x => x.NumericValue!.Value).ToArray();
                        numeric = new(numbers.Min(), numbers.Max(), numbers.Average());
                        break;
                    case AttributeDataType.Date:
                        var dateValues = values.Select(x => x.DateValue!.Value).ToArray();
                        dates = new(Date(dateValues.Min()), Date(dateValues.Max()));
                        break;
                    case AttributeDataType.Period:
                        var completed = values.Where(x => x.PeriodEnd.HasValue).ToArray();
                        period = new(Date(values.Min(x => x.PeriodStart!.Value)),
                            completed.Length == 0 ? null : Date(completed.Max(x => x.PeriodEnd!.Value)),
                            completed.Length == 0 ? null : completed.Average(x => (decimal)(x.PeriodEnd!.Value - x.PeriodStart!.Value).TotalDays),
                            values.Count(x => x.PeriodEnd is null));
                        break;
                    default:
                        popular = values.Select(x => definition.DataType switch
                        {
                            AttributeDataType.Boolean => x.BoolValue == true ? "true" : "false",
                            AttributeDataType.Dropdown => definition.Options.FirstOrDefault(o => o.Id == x.SelectedOptionId)?.Value,
                            _ => x.StringValue?.Trim()
                        }).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x!, StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Take(5)
                            .Select(x => new ExportPopularValue(x.Key, x.Count())).ToArray();
                        break;
                }
            }
            return new ExportAttribute(definition.Id, definition.Name, definition.DataType.ToString(), values.Length,
                profiles.Count - values.Length, withheld, numeric, popular, dates, period);
        }).ToArray();
        return new PositionExport(1, DateTime.UtcNow, new(position.Id, position.Title, position.Company), profiles.Count, attributes);
    }

    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
