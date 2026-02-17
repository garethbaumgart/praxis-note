using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class InsightTools(McpUserContext userContext)
{
    [McpServerTool, Description("Get the insights summary dashboard showing talk time, question ratio, red flags, and trends from analyzed meetings.")]
    public async Task<string> GetInsightsSummary(GetInsightsSummary getInsightsSummary)
    {
        var query = new GetInsightsSummary.Query(userContext.UserId, userContext.ProfileId);
        var summary = await getInsightsSummary.ExecuteAsync(query);
        return summary is null
            ? JsonSerializer.Serialize(new { message = "No analyzed meetings found. Analyze some meetings first." })
            : JsonSerializer.Serialize(summary);
    }

    [McpServerTool, Description("Get behavioral trends over time showing talk time, question ratio, and red flags per meeting.")]
    public async Task<string> GetBehavioralTrends(
        GetBehavioralTrends getBehavioralTrends,
        [Description("Time range: 7d, 30d, 90d, or all")] string range = "30d",
        [Description("Optional participant name to filter by")] string? participantName = null)
    {
        var query = new GetBehavioralTrends.Query(userContext.UserId, userContext.ProfileId, range, participantName);
        var trends = await getBehavioralTrends.ExecuteAsync(query);
        return JsonSerializer.Serialize(trends);
    }

    [McpServerTool, Description("Get the communication profile analysis showing strengths, areas for improvement, and style.")]
    public async Task<string> GetCommunicationProfile(
        GetCommunicationProfile getCommunicationProfile,
        [Description("Time range: 7d, 30d, 90d, or all")] string range = "30d")
    {
        var query = new GetCommunicationProfile.Query(userContext.UserId, userContext.ProfileId, range);
        var profile = await getCommunicationProfile.ExecuteAsync(query);
        return JsonSerializer.Serialize(profile);
    }
}
