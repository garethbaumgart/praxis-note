using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Jira;
using PraxisNote.Application.Features.Jira.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class JiraService(
    IHttpClientFactory httpClientFactory,
    IOptions<JiraSettings> settings,
    ILogger<JiraService> logger) : IJiraService
{
    public async Task<JiraIssueDto> GetIssueAsync(
        string cloudId,
        string issueKey,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = $"https://api.atlassian.com/ex/jira/{cloudId}/rest/api/3/issue/{issueKey}?fields=summary,status,issuetype";

        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Jira API error {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to fetch Jira issue {issueKey}. Status: {response.StatusCode}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = doc.RootElement;

        var key = root.GetProperty("key").GetString()!;
        var fields = root.GetProperty("fields");
        var summary = fields.GetProperty("summary").GetString() ?? string.Empty;

        var status = fields.GetProperty("status");
        var statusName = status.GetProperty("name").GetString() ?? "Unknown";
        var statusCategory = status.GetProperty("statusCategory").GetProperty("key").GetString() ?? "undefined";

        var issueType = fields.GetProperty("issuetype");
        var issueTypeName = issueType.GetProperty("name").GetString() ?? "Task";

        // Build the issue URL using the cloud ID — we don't have the site URL at this layer,
        // so return a self link or use the key
        var self = root.TryGetProperty("self", out var selfProp) ? selfProp.GetString() : null;
        var issueUrl = self ?? $"https://api.atlassian.com/ex/jira/{cloudId}/browse/{key}";

        return new JiraIssueDto(key, summary, statusName, statusCategory, issueTypeName, issueUrl);
    }

    public async Task<JiraTokenRefreshResult> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient();

        var jiraSettings = settings.Value;
        var requestBody = new
        {
            grant_type = "refresh_token",
            client_id = jiraSettings.ClientId,
            client_secret = jiraSettings.ClientSecret,
            refresh_token = refreshToken
        };

        var response = await client.PostAsJsonAsync("https://auth.atlassian.com/oauth/token", requestBody, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Jira token refresh failed: {Response}", responseBody);
            throw new InvalidOperationException("Failed to refresh Jira access token.");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()!;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var newRefreshToken = root.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString()
            : null;

        logger.LogInformation("Successfully refreshed Jira access token");

        return new JiraTokenRefreshResult(
            accessToken,
            DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            newRefreshToken);
    }

    public async Task<(string CloudId, string SiteUrl)?> GetAccessibleResourceAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://api.atlassian.com/oauth/token/accessible-resources", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch accessible Jira resources: {Body}", body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        var resources = doc.RootElement;

        if (resources.ValueKind != JsonValueKind.Array || resources.GetArrayLength() == 0)
        {
            logger.LogWarning("No accessible Jira resources found for the user");
            return null;
        }

        // Use the first accessible resource
        var resource = resources[0];
        var cloudId = resource.GetProperty("id").GetString()!;
        var siteUrl = resource.GetProperty("url").GetString()!;

        return (cloudId, siteUrl);
    }
}
