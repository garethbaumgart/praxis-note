namespace PraxisNote.Application.Features.Jira;

public record JiraIssueDto(
    string Key,
    string Summary,
    string Status,
    string StatusCategory,
    string IssueType,
    string Url);
