namespace PraxisNote.Application.Features.Jira;

public sealed class JiraSettings
{
    public const string SectionName = "Jira";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
