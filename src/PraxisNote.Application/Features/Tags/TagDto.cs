namespace PraxisNote.Application.Features.Tags;

/// <summary>
/// Tag with usage statistics for listing.
/// </summary>
public record TagDto(
    Guid Id,
    string Name,
    int UsageCount);

/// <summary>
/// Minimal tag info for embedding in task responses.
/// </summary>
public record TaskTagDto(
    Guid Id,
    string Name);
