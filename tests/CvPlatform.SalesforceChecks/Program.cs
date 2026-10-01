using System.Net;
using System.Text.Json;
using CvPlatform.Domain.Entities;
using CvPlatform.Infrastructure.Integrations.Salesforce;
using Microsoft.Extensions.Options;

var profile = new UserProfile("test-user", "Test", "Candidate", "Warsaw");
var options = Options.Create(new SalesforceOptions { Domain = "https://login.test", ClientId = "test", ClientSecret = "test" });

foreach (var mode in new[] { "create", "recover", "failure", "restricted-fields" })
{
    var handler = new SalesforceHandler(profile.Id, mode);
    var service = new SalesforceService(new HttpClient(handler), options);
    try
    {
        var ids = await service.CreateProfileAsync(profile, "test@example.com", "Test organization", null, "Test notes", false, default);
        if (mode == "failure" || ids != ("account-id", "contact-id")) throw new Exception("Unexpected integration result.");
        if (handler.CompositeCalls != (mode == "recover" ? 0 : 1)) throw new Exception("Unexpected duplicate creation.");
    }
    catch (InvalidOperationException exception) when (mode == "failure" && exception.Message.Contains("REQUIRED_FIELD_MISSING")) { }
    Console.WriteLine($"PASS: {mode}");
}

var accountsHandler = new SalesforceHandler(profile.Id, "accounts");
var accounts = await new SalesforceService(new HttpClient(accountsHandler), options).GetAccountsAsync();
if (!accounts.Contains("totalSize")) throw new Exception("Accounts were not returned.");
Console.WriteLine("PASS: version discovery and instance URL");

sealed class SalesforceHandler(Guid profileId, string mode) : HttpMessageHandler
{
    public int CompositeCalls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/oauth2/token"))
            return Json("{\"access_token\":\"test-token\",\"instance_url\":\"https://instance.test\"}");
        if (request.RequestUri.Host != "instance.test" || request.Headers.Authorization?.Parameter != "test-token")
            throw new Exception("API requests must use the authenticated instance and token.");
        if (path == "/services/data/") return Json("[{\"url\":\"/services/data/v65.0\"},{\"url\":\"/services/data/v66.0\"}]");
        if (!path.StartsWith("/services/data/v66.0/")) throw new Exception("Incorrect API version.");
        if (path.EndsWith("/describe")) return mode == "restricted-fields" ? Json("{\"fields\":[]}") : Json("{\"fields\":[{\"name\":\"HasOptedOutOfEmail\",\"createable\":true}]}");
        if (path.EndsWith("/query/"))
        {
            if (mode == "recover") return Json(JsonSerializer.Serialize(new { records = new[] { new { Id = "contact-id", AccountId = "account-id", Description = $"CvPlatform profile {profileId:D}\nLocation: Warsaw" } } }));
            return Json("{\"totalSize\":0,\"records\":[]}");
        }
        if (path.EndsWith("/composite"))
        {
            CompositeCalls++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (!body.RootElement.GetProperty("allOrNone").GetBoolean()) throw new Exception("Creation must be atomic.");
            var contact = body.RootElement.GetProperty("compositeRequest")[1].GetProperty("body");
            if (contact.GetProperty("AccountId").GetString() != "@{account.id}" || (mode != "restricted-fields" && !contact.GetProperty("HasOptedOutOfEmail").GetBoolean()))
                throw new Exception("Contact linkage or consent is incorrect.");
            if (mode == "restricted-fields" && (contact.TryGetProperty("HasOptedOutOfEmail", out _) || !contact.GetProperty("Description").GetString()!.Contains("Newsletter consent: False")))
                throw new Exception("Unavailable fields must be omitted, while consent must be preserved.");
            return mode == "failure"
                ? Json("{\"compositeResponse\":[{\"referenceId\":\"account\",\"httpStatusCode\":400,\"body\":[{\"errorCode\":\"PROCESSING_HALTED\"}]},{\"referenceId\":\"contact\",\"httpStatusCode\":400,\"body\":[{\"errorCode\":\"REQUIRED_FIELD_MISSING\"}]}]}")
                : Json("{\"compositeResponse\":[{\"referenceId\":\"account\",\"httpStatusCode\":201,\"body\":{\"id\":\"account-id\"}},{\"referenceId\":\"contact\",\"httpStatusCode\":201,\"body\":{\"id\":\"contact-id\"}}]}");
        }
        throw new Exception("Unexpected Salesforce request.");
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
}
