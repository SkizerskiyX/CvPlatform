using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Infrastructure.Integrations.Odoo;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=cvplatform_checks;Username=unused;Password=unused").Options))
{
    Assert(!db.Database.HasPendingModelChanges(), "migration snapshot matches the runtime model");
    Assert(db.Database.GetMigrations().Contains("20261002232000_AddPositionExportTokens"), "token migration is discoverable");
}

var categoryId = Guid.NewGuid();
var definitions = new[]
{
    new AttributeDefinition("Score", null, AttributeDataType.Numeric, categoryId),
    new AttributeDefinition("Skill", null, AttributeDataType.String, categoryId),
    new AttributeDefinition("Available", null, AttributeDataType.Boolean, categoryId),
    new AttributeDefinition("Language", null, AttributeDataType.Dropdown, categoryId),
    new AttributeDefinition("Started", null, AttributeDataType.Date, categoryId),
    new AttributeDefinition("Experience", null, AttributeDataType.Period, categoryId),
    new AttributeDefinition("First name", null, AttributeDataType.String, categoryId, BuiltInAttributes.FirstName),
    new AttributeDefinition("Portrait", null, AttributeDataType.Image, categoryId),
};
definitions[3].SetOptions(["English", "Russian"]);
var position = new Position("Integration checks", null, true);
position.SetAttributes(definitions.Select(x => x.Id).ToArray());
foreach (var attribute in position.PositionAttributes)
    typeof(PositionAttribute).GetProperty(nameof(PositionAttribute.AttributeDefinition))!.SetValue(attribute, definitions.Single(x => x.Id == attribute.AttributeDefinitionId));
var profiles = Enumerable.Range(0, 3).Select(x => new UserProfile($"test-{x}", $"Private-{x}", "Candidate", null)).ToArray();
profiles[0].GetOrAddAttributeValue(definitions[0].Id).SetNumeric(10);
profiles[1].GetOrAddAttributeValue(definitions[0].Id).SetNumeric(30);
profiles[0].GetOrAddAttributeValue(definitions[1].Id).SetString("React");
profiles[1].GetOrAddAttributeValue(definitions[1].Id).SetString("react");
profiles[2].GetOrAddAttributeValue(definitions[1].Id).SetString("Vue");
profiles[0].GetOrAddAttributeValue(definitions[2].Id).SetBoolean(false);
profiles[1].GetOrAddAttributeValue(definitions[2].Id).SetBoolean(true);
profiles[2].GetOrAddAttributeValue(definitions[2].Id).SetBoolean(false);
profiles[0].GetOrAddAttributeValue(definitions[3].Id).SetSelectedOption(definitions[3].Options.First().Id);
profiles[0].GetOrAddAttributeValue(definitions[4].Id).SetDate(new DateTime(2025, 1, 1));
profiles[1].GetOrAddAttributeValue(definitions[4].Id).SetDate(new DateTime(2026, 1, 1));
profiles[0].GetOrAddAttributeValue(definitions[5].Id).SetPeriod(new DateTime(2025, 1, 1), new DateTime(2025, 1, 11));
profiles[1].GetOrAddAttributeValue(definitions[5].Id).SetPeriod(new DateTime(2026, 1, 1), null);
profiles[0].GetOrAddAttributeValue(definitions[7].Id).SetImage("https://example.com/private-photo.png");
var result = PositionExportService.Aggregate(position, profiles);
Assert(result.Attributes[0].Numeric == new ExportNumeric(10, 30, 20) && result.Attributes[0].MissingCount == 1, "numeric aggregates and missing values");
Assert(result.Attributes[1].PopularValues[0].Count == 2 && result.Attributes[1].PopularValues[0].Value == "React", "case-insensitive text grouping");
Assert(result.Attributes[2].PopularValues[0] == new ExportPopularValue("false", 2), "false boolean values are included");
Assert(result.Attributes[3].PopularValues[0].Value == "English", "dropdown option titles");
Assert(result.Attributes[4].Dates == new ExportDates("2025-01-01", "2026-01-01"), "date range");
Assert(result.Attributes[5].Period?.AverageDays == 10 && result.Attributes[5].Period?.OpenEndedCount == 1, "completed and open-ended periods");
Assert(result.Attributes[6].ValuesWithheld && result.Attributes[6].PopularValues.Count == 0 && result.Attributes[7].ValuesWithheld, "built-in values and image URLs are withheld");
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
Assert(!json.Contains("Private-") && !json.Contains("private-photo.png"), "no personal values in exported JSON");
Assert(PositionExportService.Aggregate(position, []).Attributes.All(x => x.ValueCount == 0 && x.Numeric is null), "empty position exports null numeric results");

if (args.FirstOrDefault() == "--live") await LiveChecks(args.ElementAtOrDefault(1) ?? "http://localhost:5242");

static void Assert(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}

static async Task LiveChecks(string baseUrl)
{
    using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    using var loginResponse = await client.PostAsJsonAsync("/api/account/login", new { email = "admin@cv.local", password = "Admin123!" });
    loginResponse.EnsureSuccessStatusCode();
    using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
    var adminToken = login.RootElement.GetProperty("token").GetString()!;
    var ids = new List<Guid>();
    try
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        foreach (var suffix in new[] { "A", "B" })
        {
            using var response = await client.PostAsJsonAsync("/api/positions", new
            {
                title = "Odoo integration check " + suffix, isPublic = true, maxProjects = 3,
                projectTags = Array.Empty<string>(), attributeIds = Array.Empty<Guid>(), accessRules = Array.Empty<object>()
            });
            response.EnsureSuccessStatusCode();
            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            ids.Add(created.RootElement.GetProperty("id").GetGuid());
        }
        async Task<string> Generate(Guid id)
        {
            using var response = await client.PostAsync($"/api/integrations/odoo/positions/{id}/token", null);
            response.EnsureSuccessStatusCode();
            using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return data.RootElement.GetProperty("token").GetString()!;
        }
        var tokenA = await Generate(ids[0]);
        var tokenB = await Generate(ids[1]);
        Assert(tokenA.Length == 47 && tokenA != tokenB, "separate random position tokens");
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokenA);
        using var exported = await client.GetAsync("/api/integrations/odoo/position?positionId=" + ids[1]);
        exported.EnsureSuccessStatusCode();
        using var data = JsonDocument.Parse(await exported.Content.ReadAsStringAsync());
        Assert(data.RootElement.GetProperty("position").GetProperty("id").GetGuid() == ids[0], "token cannot access another position through query parameters");
        Assert(exported.Headers.CacheControl?.NoStore == true, "exports are not cached");
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        using var jwtAttempt = await client.GetAsync("/api/integrations/odoo/position");
        Assert(jwtAttempt.StatusCode == HttpStatusCode.Unauthorized, "user JWT is not an export token");
        var replacement = await Generate(ids[0]);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokenA);
        using var oldTokenAttempt = await client.GetAsync("/api/integrations/odoo/position");
        Assert(oldTokenAttempt.StatusCode == HttpStatusCode.Unauthorized, "rotation invalidates old token");
        client.DefaultRequestHeaders.Authorization = new("Bearer", replacement);
        using var newTokenAttempt = await client.GetAsync("/api/integrations/odoo/position");
        Assert(newTokenAttempt.IsSuccessStatusCode, "new token works after rotation");
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        using var revoke = await client.DeleteAsync($"/api/integrations/odoo/positions/{ids[0]}/token");
        revoke.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", replacement);
        using var revokedAttempt = await client.GetAsync("/api/integrations/odoo/position");
        Assert(revokedAttempt.StatusCode == HttpStatusCode.Unauthorized, "revoked token is rejected");
        client.DefaultRequestHeaders.Authorization = null;
        using var anonymousAttempt = await client.PostAsync($"/api/integrations/odoo/positions/{ids[1]}/token", null);
        Assert(anonymousAttempt.StatusCode == HttpStatusCode.Unauthorized, "anonymous users cannot generate tokens");
        using var candidateResponse = await client.PostAsJsonAsync("/api/account/login", new { email = "candidate@cv.local", password = "Candidate123!" });
        candidateResponse.EnsureSuccessStatusCode();
        using var candidate = JsonDocument.Parse(await candidateResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new("Bearer", candidate.RootElement.GetProperty("token").GetString());
        using var forbiddenAttempt = await client.PostAsync($"/api/integrations/odoo/positions/{ids[1]}/token", null);
        Assert(forbiddenAttempt.StatusCode == HttpStatusCode.Forbidden, "candidates cannot generate tokens");
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        using var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/positions") { Content = JsonContent.Create(new { ids = new[] { ids[1] } }) };
        using var deleteResponse = await client.SendAsync(delete);
        deleteResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokenB);
        using var deletedAttempt = await client.GetAsync("/api/integrations/odoo/position");
        Assert(deletedAttempt.StatusCode == HttpStatusCode.Unauthorized, "deleting a position removes export access");
    }
    finally
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
        using var cleanup = new HttpRequestMessage(HttpMethod.Delete, "/api/positions") { Content = JsonContent.Create(new { ids }) };
        using var response = await client.SendAsync(cleanup);
        response.EnsureSuccessStatusCode();
    }
}
