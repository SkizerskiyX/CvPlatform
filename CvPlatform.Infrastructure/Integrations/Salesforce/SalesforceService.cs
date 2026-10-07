using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CvPlatform.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CvPlatform.Infrastructure.Integrations.Salesforce;

public sealed class SalesforceService(HttpClient httpClient, IOptions<SalesforceOptions> options)
{
    private readonly SalesforceOptions _options = options.Value;

    private async Task<SalesforceTokenResponse> AuthenticateAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_options.Domain, UriKind.Absolute, out var domain) || domain.Scheme != "https"
            || string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("Salesforce integration is not configured. Contact an administrator.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.Domain.TrimEnd('/')}/services/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials", ["client_id"] = _options.ClientId, ["client_secret"] = _options.ClientSecret
            })
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Salesforce authorization failed. Check the integration user and OAuth settings.");
        var token = await response.Content.ReadFromJsonAsync<SalesforceTokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(token?.AccessToken) || !Uri.TryCreate(token.InstanceUrl, UriKind.Absolute, out var instance) || instance.Scheme != "https")
            throw new InvalidOperationException("Salesforce returned an invalid authorization response.");
        return token;
    }

    private async Task<string> ApiRootAsync(SalesforceTokenResponse token, CancellationToken cancellationToken)
    {
        using var versions = await SendAsync(token, HttpMethod.Get, "/services/data/", null, cancellationToken);
        return versions.RootElement.EnumerateArray().Last().GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Salesforce did not return an API version.");
    }

    private async Task<JsonDocument> SendAsync(SalesforceTokenResponse token, HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, token.InstanceUrl.TrimEnd('/') + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body, options: new JsonSerializerOptions { PropertyNamingPolicy = null });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Salesforce request failed (HTTP {(int)response.StatusCode}). Check Account and Contact permissions.");
        return JsonDocument.Parse(content);
    }

    public async Task<string> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var token = await AuthenticateAsync(cancellationToken);
        var root = await ApiRootAsync(token, cancellationToken);
        using var result = await SendAsync(token, HttpMethod.Get, root + "/query/?q=" + Uri.EscapeDataString("SELECT Id, Name FROM Account LIMIT 10"), null, cancellationToken);
        return result.RootElement.GetRawText();
    }

    public async Task<(string AccountId, string ContactId)> CreateProfileAsync(UserProfile profile, string email, string organization, string? phone, string? notes, bool newsletterConsent, CancellationToken cancellationToken)
    {
        var token = await AuthenticateAsync(cancellationToken);
        var root = await ApiRootAsync(token, cancellationToken);
        var marker = $"CvPlatform profile {profile.Id:D}";
        string? contactId = null;
        string? accountId = null;
        var escapedEmail = email.Replace("\\", "\\\\").Replace("'", "\\'");
        var contactFilter = $"Email = '{escapedEmail}'";
        if (profile.SalesforceContactId is { } savedContactId)
            contactFilter += $" OR Id = '{savedContactId.Replace("\\", "\\\\").Replace("'", "\\'")}'";
        using var existing = await SendAsync(token, HttpMethod.Get, root + "/query/?q=" + Uri.EscapeDataString($"SELECT Id, AccountId, Description FROM Contact WHERE {contactFilter} LIMIT 2000"), null, cancellationToken);
        foreach (var record in existing.RootElement.GetProperty("records").EnumerateArray())
            if (record.GetProperty("Description").GetString()?.StartsWith(marker + "\n", StringComparison.Ordinal) == true)
            {
                contactId = record.GetProperty("Id").GetString();
                accountId = record.GetProperty("AccountId").ValueKind == JsonValueKind.String ? record.GetProperty("AccountId").GetString() : null;
                break;
            }
        accountId ??= profile.SalesforceAccountId;
        var accountQuery = accountId is null
            ? $"SELECT Id, Description FROM Account WHERE Name = '{organization.Replace("\\", "\\\\").Replace("'", "\\'")}' LIMIT 2000"
            : $"SELECT Id FROM Account WHERE Id = '{accountId.Replace("'", "\\'")}' LIMIT 1";
        using var accounts = await SendAsync(token, HttpMethod.Get, root + "/query/?q=" + Uri.EscapeDataString(accountQuery), null, cancellationToken);
        accountId = accounts.RootElement.GetProperty("records").EnumerateArray()
            .Where(x => accountId is not null || x.GetProperty("Description").GetString() == marker)
            .Select(x => x.GetProperty("Id").GetString()).FirstOrDefault();

        var description = $"{marker}\nLocation: {profile.Location}\nPhoto: {profile.PhotoUrl}\nNewsletter consent: {newsletterConsent}\n{notes}";
        var contactBody = new Dictionary<string, object?>
        {
            ["AccountId"] = accountId ?? "@{account.id}", ["FirstName"] = profile.FirstName, ["LastName"] = profile.LastName,
            ["Email"] = email, ["Phone"] = phone, ["Description"] = description
        };
        using var metadata = await SendAsync(token, HttpMethod.Get, root + "/sobjects/Contact/describe", null, cancellationToken);
        if (metadata.RootElement.GetProperty("fields").EnumerateArray().Any(x => x.GetProperty("name").GetString() == "HasOptedOutOfEmail" && x.TryGetProperty(contactId is null ? "createable" : "updateable", out var writable) && writable.GetBoolean()))
            contactBody["HasOptedOutOfEmail"] = !newsletterConsent;
        var body = new
        {
            allOrNone = true,
            compositeRequest = new object[]
            {
                new { method = accountId is null ? "POST" : "PATCH", url = root + "/sobjects/Account" + (accountId is null ? "" : "/" + accountId), referenceId = "account", body = new { Name = organization, Description = marker } },
                new { method = contactId is null ? "POST" : "PATCH", url = root + "/sobjects/Contact" + (contactId is null ? "" : "/" + contactId), referenceId = "contact", body = contactBody }
            }
        };
        using var result = await SendAsync(token, HttpMethod.Post, root + "/composite", body, cancellationToken);
        var responses = result.RootElement.GetProperty("compositeResponse").EnumerateArray().ToArray();
        var failures = responses.Where(x => x.GetProperty("httpStatusCode").GetInt32() >= 400).ToArray();
        var failed = failures.FirstOrDefault(x => !x.GetProperty("body").ToString().Contains("PROCESSING_HALTED"));
        if (failed.ValueKind == JsonValueKind.Undefined) failed = failures.FirstOrDefault();
        if (failed.ValueKind != JsonValueKind.Undefined)
        {
            var error = failed.GetProperty("body");
            var code = error.ValueKind == JsonValueKind.Array && error.GetArrayLength() > 0 && error[0].TryGetProperty("errorCode", out var errorCode) ? errorCode.GetString() : "UNKNOWN_ERROR";
            throw new InvalidOperationException($"Salesforce could not create the records ({code}). Check required fields and Account/Contact permissions.");
        }
        return (accountId ?? responses.Single(x => x.GetProperty("referenceId").GetString() == "account").GetProperty("body").GetProperty("id").GetString()!,
            contactId ?? responses.Single(x => x.GetProperty("referenceId").GetString() == "contact").GetProperty("body").GetProperty("id").GetString()!);
    }
}
