using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public record UserAiKeyDto(
    AiProvider Provider,
    bool HasKey,
    string? KeyHint,
    string? PreferredModel,
    DateTimeOffset? CreatedAt);
