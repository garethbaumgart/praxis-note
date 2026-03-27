namespace PraxisNote.Application.Features.UserAiKeys;

public record UserAiKeyDto(
    string Provider,
    bool HasKey,
    string? KeyHint,
    string? PreferredModel,
    DateTimeOffset? CreatedAt);
